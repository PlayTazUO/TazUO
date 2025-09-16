#!/usr/bin/env python3
"""
SDL Replacement Script
Replaces SDL constants/functions across ClassicUO projects.
Usage: python3 sdl_keymod_replacer.py "old_string" "new_string"
"""

import os
import re
import glob
import sys
from typing import List, Tuple

def find_cs_files(root_dir: str) -> List[str]:
    """Find all C# files in ClassicUO projects."""
    cs_files = []
    for pattern in ['src/ClassicUO.*/**/*.cs']:
        cs_files.extend(glob.glob(os.path.join(root_dir, pattern), recursive=True))
    return cs_files

def replace_in_file(file_path: str, replacements: List[Tuple[str, str]]) -> int:
    """Replace multiple patterns in a single file. Returns number of replacements made."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        original_content = content
        total_replacements = 0

        for old_pattern, new_pattern in replacements:
            # Count occurrences before replacement
            count = len(re.findall(re.escape(old_pattern), content))
            if count > 0:
                content = content.replace(old_pattern, new_pattern)
                total_replacements += count
                print(f"  {old_pattern} -> {new_pattern}: {count} replacements")

        # Only write if changes were made
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            return total_replacements

        return 0
    except Exception as e:
        print(f"Error processing {file_path}: {e}")
        return 0

def main():
    """Main function to run the replacements."""
    # Parse command line arguments
    if len(sys.argv) != 3:
        print("Usage: python3 sdl_keymod_replacer.py \"old_string\" \"new_string\"")
        print()
        print("Examples:")
        print("  python3 sdl_keymod_replacer.py \"SDL.SDL_Keymod.KMOD_SHIFT\" \"SDL.SDL_Keymod.SDL_KMOD_SHIFT\"")
        print("  python3 sdl_keymod_replacer.py \"SDL.SDL_IsTextInputActive()\" \"SDL.SDL_TextInputActive(Client.Game.Window.Handle)\"")
        sys.exit(1)

    old_string = sys.argv[1]
    new_string = sys.argv[2]

    # Create replacements list from command line arguments
    replacements = [(old_string, new_string)]

    print(f"Replacing: '{old_string}' -> '{new_string}'")
    print()

    # Get the current directory (should be TazUO repo root)
    root_dir = os.getcwd()
    print(f"Working in directory: {root_dir}")

    # Find all C# files
    cs_files = find_cs_files(root_dir)
    print(f"Found {len(cs_files)} C# files to process")

    if not cs_files:
        print("No C# files found. Make sure you're running from the TazUO repository root.")
        return

    total_files_changed = 0
    total_replacements = 0

    # Process each file
    for file_path in cs_files:
        rel_path = os.path.relpath(file_path, root_dir)
        replacements_in_file = replace_in_file(file_path, replacements)

        if replacements_in_file > 0:
            print(f"\n{rel_path}:")
            total_files_changed += 1
            total_replacements += replacements_in_file

    print(f"\n=== Summary ===")
    print(f"Files processed: {len(cs_files)}")
    print(f"Files changed: {total_files_changed}")
    print(f"Total replacements: {total_replacements}")

    if total_replacements > 0:
        print(f"\nSuccessfully completed {total_replacements} replacements across {total_files_changed} files.")
    else:
        print("\nNo replacements were needed.")

if __name__ == "__main__":
    main()
