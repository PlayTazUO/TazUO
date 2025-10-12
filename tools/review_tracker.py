#!/usr/bin/env python3
"""
Review Tracker - A tool for tracking code review progress across C# files.

This script helps manage which files have been reviewed and which need attention next.
"""

import sqlite3
import os
import sys
import argparse
from datetime import datetime
from pathlib import Path


class ReviewTracker:
    """Manages the review tracking database and operations."""

    def __init__(self, db_path="review_tracker.db"):
        """
        Initialize the review tracker with a database connection.

        Args:
            db_path: Path to the SQLite database file
        """
        self.db_path = db_path
        self.conn = None
        self._init_database()

    def _init_database(self):
        """Create the database and table if they don't exist."""
        self.conn = sqlite3.connect(self.db_path)
        cursor = self.conn.cursor()

        # Create the reviews table with path as primary key
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS reviews (
                path TEXT PRIMARY KEY,
                last_reviewed DATETIME
            )
        """)

        self.conn.commit()
        print(f"✓ Database initialized at: {self.db_path}")

    def scan_directory(self, directory):
        """
        Recursively scan directory for *.cs files and add them to the database.

        Args:
            directory: Root directory to scan
        """
        directory = os.path.abspath(directory)

        if not os.path.isdir(directory):
            print(f"✗ Error: '{directory}' is not a valid directory")
            return

        print(f"Scanning directory: {directory}")

        # Find all .cs files recursively
        cs_files = []
        for root, dirs, files in os.walk(directory):
            # Skip common directories that don't need review
            dirs[:] = [d for d in dirs if d not in {'.git', '.vs', 'bin', 'obj', 'packages'}]

            for file in files:
                if file.endswith('.cs'):
                    full_path = os.path.join(root, file)
                    cs_files.append(full_path)

        if not cs_files:
            print("✗ No .cs files found")
            return

        # Add files to database (only if not already present)
        cursor = self.conn.cursor()
        added_count = 0
        existing_count = 0

        for file_path in cs_files:
            try:
                cursor.execute(
                    "INSERT INTO reviews (path, last_reviewed) VALUES (?, NULL)",
                    (file_path,)
                )
                added_count += 1
            except sqlite3.IntegrityError:
                # File already exists in database
                existing_count += 1

        self.conn.commit()

        print(f"✓ Scan complete:")
        print(f"  - {added_count} new files added")
        print(f"  - {existing_count} files already tracked")
        print(f"  - {added_count + existing_count} total .cs files found")

    def update_review(self, file_path):
        """
        Mark a file as reviewed with the current timestamp.

        Args:
            file_path: Path to the file to mark as reviewed
        """
        file_path = os.path.abspath(file_path)

        cursor = self.conn.cursor()
        current_time = datetime.now().isoformat()

        # Check if file exists in database
        cursor.execute("SELECT path FROM reviews WHERE path = ?", (file_path,))
        result = cursor.fetchone()

        if result is None:
            print(f"✗ Error: '{file_path}' is not in the database")
            print("  Run scan command first to add files")
            return

        # Update the last_reviewed timestamp
        cursor.execute(
            "UPDATE reviews SET last_reviewed = ? WHERE path = ?",
            (current_time, file_path)
        )
        self.conn.commit()

        print(f"✓ Marked as reviewed: {file_path}")
        print(f"  Timestamp: {current_time}")

    def get_next_file(self):
        """
        Get the next file to review based on review priority.

        Priority:
        1. Files never reviewed (last_reviewed IS NULL)
        2. Files with oldest review date

        Returns:
            Path to the next file to review, or None if no files found
        """
        cursor = self.conn.cursor()

        # First, try to get files that have never been reviewed
        cursor.execute("""
            SELECT path FROM reviews
            WHERE last_reviewed IS NULL
            LIMIT 1
        """)

        result = cursor.fetchone()

        if result:
            file_path = result[0]
            print(f"Next file to review (never reviewed):")
            print(f"  {file_path}")
            return file_path

        # If all files have been reviewed, get the oldest one
        cursor.execute("""
            SELECT path, last_reviewed FROM reviews
            ORDER BY last_reviewed ASC
            LIMIT 1
        """)

        result = cursor.fetchone()

        if result:
            file_path, last_reviewed = result
            print(f"Next file to review (oldest review):")
            print(f"  Path: {file_path}")
            print(f"  Last reviewed: {last_reviewed}")
            return file_path

        print("✗ No files in database. Run scan command first.")
        return None

    def get_stats(self):
        """Display statistics about the review database."""
        cursor = self.conn.cursor()

        # Total files
        cursor.execute("SELECT COUNT(*) FROM reviews")
        total = cursor.fetchone()[0]

        # Never reviewed
        cursor.execute("SELECT COUNT(*) FROM reviews WHERE last_reviewed IS NULL")
        never_reviewed = cursor.fetchone()[0]

        # Reviewed
        reviewed = total - never_reviewed

        print(f"Review Statistics:")
        print(f"  Total files tracked: {total}")
        print(f"  Files reviewed: {reviewed}")
        print(f"  Files never reviewed: {never_reviewed}")

        if reviewed > 0:
            percentage = (reviewed / total) * 100
            print(f"  Progress: {percentage:.1f}%")

    def close(self):
        """Close the database connection."""
        if self.conn:
            self.conn.close()


def main():
    """Main entry point for the CLI."""
    parser = argparse.ArgumentParser(
        description="Review Tracker - Track code review progress for C# files",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python review_tracker.py -scan /path/to/project
  python review_tracker.py -update /path/to/file.cs
  python review_tracker.py -next
  python review_tracker.py -stats
        """
    )

    # Add mutually exclusive group for commands
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument(
        '-scan',
        metavar='DIRECTORY',
        help='Recursively scan directory for .cs files and add to database'
    )
    group.add_argument(
        '-update',
        metavar='FILE_PATH',
        help='Mark a file as reviewed with current timestamp'
    )
    group.add_argument(
        '-next',
        action='store_true',
        help='Get the next file to review (prioritizes never-reviewed files)'
    )
    group.add_argument(
        '-stats',
        action='store_true',
        help='Display review statistics'
    )

    parser.add_argument(
        '-db',
        metavar='DB_PATH',
        default='review_tracker.db',
        help='Path to database file (default: review_tracker.db)'
    )

    args = parser.parse_args()

    # Initialize tracker
    tracker = ReviewTracker(db_path=args.db)

    try:
        # Execute the appropriate command
        if args.scan:
            tracker.scan_directory(args.scan)
        elif args.update:
            tracker.update_review(args.update)
        elif args.next:
            tracker.get_next_file()
        elif args.stats:
            tracker.get_stats()
    finally:
        tracker.close()


if __name__ == "__main__":
    main()


"""
# Review Tracker Documentation

## Overview
Review Tracker is a command-line tool for managing code review progress across C# files in a project.
It uses SQLite to track which files have been reviewed and when, helping you systematically work
through a codebase.

## Installation

### Requirements
- Python 3.6 or higher (no additional packages required - uses only standard library)

### Setup
1. Download the `review_tracker.py` script to your tools directory
2. Make it executable (Linux/Mac): `chmod +x review_tracker.py`
3. That's it! The database will be created automatically on first use.

## Usage

### Basic Commands

#### Scan Directory
Recursively finds all `.cs` files in a directory and adds them to the tracking database:
```bash
python review_tracker.py -scan /path/to/project
```

Example for TazUO:
```bash
python review_tracker.py -scan ../src
```

#### Update Review Status
Mark a specific file as reviewed (records current timestamp):
```bash
python review_tracker.py -update /path/to/file.cs
```

Example:
```bash
python review_tracker.py -update ../src/ClassicUO.Client/Game/GameController.cs
```

#### Get Next File
Get the next file that needs review (prioritizes never-reviewed files, then oldest reviews):
```bash
python review_tracker.py -next
```

#### View Statistics
Display review progress statistics:
```bash
python review_tracker.py -stats
```

### Advanced Options

#### Custom Database Location
By default, the database is stored as `review_tracker.db` in the current directory.
You can specify a different location:
```bash
python review_tracker.py -db /path/to/custom.db -scan ../src
```

## Workflow Example

1. **Initial Setup**: Scan your project
   ```bash
   cd tools
   python review_tracker.py -scan ../src
   ```

2. **Start Reviewing**: Get the next file
   ```bash
   python review_tracker.py -next
   ```

3. **After Review**: Mark the file as reviewed
   ```bash
   python review_tracker.py -update /path/shown/by/next
   ```

4. **Check Progress**: View statistics
   ```bash
   python review_tracker.py -stats
   ```

5. **Repeat**: Continue with steps 2-4

## Database Schema

The tool uses a simple SQLite database with one table:

```sql
CREATE TABLE reviews (
    path TEXT PRIMARY KEY,           -- Full path to the file
    last_reviewed DATETIME           -- ISO format timestamp or NULL
);
```

## Features

- **Automatic Filtering**: Skips common non-source directories (.git, .vs, bin, obj, packages)
- **Duplicate Prevention**: Won't add the same file twice
- **Priority System**: Files never reviewed are prioritized over old reviews
- **Simple Storage**: Uses SQLite - no external database required
- **Portable**: Pure Python with no dependencies outside standard library

## Tips

- Run the scan command periodically to pick up new files
- Use `-stats` to track your progress
- The database is portable - you can share it with team members
- Files are identified by absolute path, so moving the database requires rescanning
"""
