from pathlib import Path

def update_language_file():
    print("--- TazLang Loc Utility ---")
    val_string = input("Enter the text string: ").strip()
    key = input("Enter the key name: ").strip()

    val_string = val_string.removeprefix("\"")
    val_string = val_string.removesuffix("\"")
    
    if not val_string or not key:
        print("❌ Error: Both string and key are required!")
        return

    # Define the target file path relative to this script
    script_dir = Path(__file__).parent
    target_file = (script_dir / ".." / "src" / "ClassicUO.Client" / "Configuration" / "language.ini").resolve()

    # Append the key=string to the file
    try:
        target_file.parent.mkdir(parents=True, exist_ok=True)
        
        with open(target_file, "a", encoding="utf-8") as f:
            f.write(f"\n{key}={val_string}")
            
        print(f" Saved to: {target_file}")
        
    except Exception as e:
        print(f"❌ Failed to write to file: {e}")
        return

    # Output the result to the console
    print("\n--- Result ---")
    print(f'TazLang.Get("{key}")')
    print("--------------")

if __name__ == "__main__":
    while True:
        update_language_file()