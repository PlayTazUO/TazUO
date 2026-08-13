# TazUO Project Guide for Claude

## Project Overview

**TazUO** is a feature-rich fork of ClassicUO, an open-source implementation of the Ultima Online Classic Client. Originally forked to add quality-of-life features requested by users, TazUO has evolved into an independent project that selectively incorporates updates from the original ClassicUO while focusing on enhanced gameplay features.

- **Repository**: https://github.com/PlayTazUO/TazUO
- **Language**: C# targeting .NET 10
- **Platform**: Windows with support for Mac and Linux via Mono
- **License**: Based on ClassicUO's open-source license

## Architecture Overview

### Solution Structure
The project is organized as a Visual Studio solution with the following main components:

```
ClassicUO.sln
├── src/
│   ├── ClassicUO.Client/          # Main executable - game client logic
│   ├── ClassicUO.Assets/          # Asset loading (animations, art, sounds, etc.)
│   ├── ClassicUO.Renderer/        # Rendering engine using FNA
│   ├── ClassicUO.IO/             # I/O operations for UO file formats
│   └── ClassicUO.Utility/        # Common utilities and helpers
├── external/                     # Third-party dependencies
├── tests/                       # Unit tests
└── tools/                      # Build and development tools
```

### Key Dependencies
- **FNA**: XNA reimplementation for cross-platform graphics
- **FontStashSharp**: Advanced font rendering
- **MP3Sharp**: MP3 audio decoding
- **IronPython**: Python scripting integration
- **Discord SDK**: Discord rich presence integration


## Code Style Rules

Applies to new code. Don't refactor existing code to match unless asked.
Indent/charset/EOL come from `.editorconfig`; this section covers what it can't express.

### Structure
- DRY. Single responsibility.
- Funcs ~40 lines ideal; exceed only if split costs more than saves.
- Params max 4-6; else bundle into struct/class.
- Files ~500 lines guideline, not a cap; 700 is fine.
- One public class per file, loosely - 1-2 extra small related ones fine; unrelated or large → split.
- OOP where it genuinely fits.

### Naming
- Names informative, no single-letter (`op` ok, `o` not), easily cognizable (`selection` ok, `sel`
  not); well-known shorthand ok (`num`, `i`/`j` loop counters).

### Formatting
- Lines ~120 ideal, 150 max (logger calls exempt).
- Multi-param call over limit → one param/line:
  ```csharp
  Something(
      a,
      b
  );
  ```
- No decorative comment banners.
- Explicitly specify access modifier.
- Class composition order, each section in its own `#region` when more than a couple of items:
  ```
  Public events
  Public accessors

  Private events
  Private members
  Protected members

  Ctor

  Public methods

  Protected methods
      Self protected methods
      Interface protected methods

  Private methods
  ```

### Comments/Docs
- XMLDocs: public/internal always, with `<param>`/`<returns>`/`<exception>`. Private/protected when
  non-trivial. Inline comments welcome on non-trivial code.
- Content: the why, the pitfall, the constraint. Restating the name is noise. Must stand alone.
- **No meta-references, ever** — "per discussion", review rounds, chat deltas, "as requested".
- History back-references ("used to be", "ported from") age into noise. Avoid as a rule; keep only
  where the history explains a still-live constraint.
- Length scales with the code. Inline 1-2 lines, XMLDoc `<summary>` 1-2; a 10-liner is occasionally
  warranted, ~95% of the time not. Boilerplate → one-liner, no rationale essay. Say it once.
- A genuinely tangled mechanism (re-entrancy, ordering, multi-hop flow) earns extra elaboration —
  code block, remarks, diagram, whatever actually clarifies.
- Acronyms use standard casing in prose — `ID`, `UO`, `JSON`. Code identifiers keep theirs.
- Docs for LLM consumption (design refs, system overviews): as terse as possible, never at the cost
  of correctness or load-bearing context.

### Readability
- Brevity yes, not at cost of clarity — dense one-liners that hurt reading → normal loop/block.

### Config Structs
- 3+ related options (e.g. one feature's toggles) → nested sub-struct, not flat fields on the parent.

### Constants/Enums
- Magic values → const.
- Multiple related values (now/future) → enum
- Many unrelated-shape consts → dedicated constants file

### Interfaces/Types
- Interface only for multiple real/expected impls; else concrete class/struct. Keep small.

### Shared Logic
- Logic used by 2+ consumers → dedicated struct/class, not copy-pasted (e.g. temp file IO).

### Serialization
- All JSON serialize/deserialize needs a generated `JsonSerializerContext`.
- Regexes invoked more than once → compiled/source-generated, not built per call.

### Performance
- Rendering / per-tick / per-frame code → perf imperative. Watch allocs (GC pressure), avoid
  LINQ/boxing/closures in hot loops, hoist cacheable work out of the loop.
- Profile or reason through cost before committing to an approach.

### UI
- Keep layout responsive: `WrapPanel`, no fixed `Width`/`Height` boxes. Resizable windows already
  provide scrollers. A vertical `WrapPanel` answers an over-tall child by starting a second column,
  so a fixed vertical sequence wants a `StackPanel`.
- User-facing strings live in `Configuration/language.ini`, read via `TazLang.Get(key, fallback)`.
  Keys should have a meaningful prefix (e.g. `options_video_tab_`).

#### Options tabs
- Build from `Option.*` / `OptionsUi.*` fragments in `Options/Tabs`; don't hand-build widgets.
- Every entry gets `SearchMetadata` (label + keywords); groups get `.WithSearch(...)`.
- A toggle that gates other options → `OptionsUi.CheckBoxGroup`, nested for sub-systems.
  Never a bare checkbox governing settings elsewhere in the panel.
- Bind with the expression form where it suffices: `new Accessor<T>(() => obj.Prop)`. Write the
  get/set pair only for a real side effect (e.g. poking a manager that doesn't watch the config).
- Fractional sliders need `decimalPlaces` - the default rounds to whole numbers, leaving a 0-1
  range with only its two ends.
- Persistence belongs to the config owner, `Profile.Save` for side-file configs. Save eagerly
  only on structural edits (add/delete/rename), never per keystroke or slider tick.

### Files
- No license header on new files.

### Cross-Platform
- Decision hurts cross-platform compat → stop, ask user first.

## Core Features

### Python Scripting System
TazUO includes a powerful Python scripting system:

- **Python Integration** (`external/iplib/`)
  - Full IronPython runtime included
  - Python API classes in `src/ClassicUO.Client/LegionScripting/PyClasses/`
  - Auto-generated documentation via `src/APIToMarkdown/`
  - Commands for movement, combat, item manipulation, UI interaction, and more

### Enhanced UI Features
- **Grid Containers**: Visual inventory management with customizable layouts
- **Modern UI Elements**: Updated gumps and controls
- **Custom Fonts**: TTF font support for better readability
- **Buff Bars**: Customizable status effect displays
- **Cooldown Bars**: Visual cooldown tracking

### Quality of Life Improvements
- **Auto Loot System**: Configurable item collection
- **Grid Highlighting**: Item property-based highlighting
- **Tooltip Overrides**: Customizable item information display
- **Controller Support**: Gamepad integration
- **Enhanced Journal**: Improved chat and message organization

## Build System

### Build Configuration
- **Framework**: .NET 10
- **Platform**: x64 only (`Directory.Build.props`)
- **Configurations**: Debug and Release
- **Output**: `bin/Debug/` or `bin/Release/`

### Build Process
1. Restores NuGet packages
2. Builds all projects in dependency order
3. Copies external dependencies (native libraries)
4. Generates scripting API documentation
5. Packages for distribution

### External Dependencies Management
The build system automatically copies platform-specific native libraries:
- `external/x64/` → Windows x64 libraries
- `external/lib64/` → Linux x64 libraries  
- `external/osx/` → macOS libraries

## Development Workflow

### Common File Locations

#### Configuration & Settings
- `src/ClassicUO.Client/Configuration/` - Game settings and profiles
- `Directory.Build.props` - MSBuild configuration
- `ClassicUO.sln.DotSettings` - ReSharper/Rider settings

#### Core Game Logic
- `src/ClassicUO.Client/Game/` - Main game systems
- `src/ClassicUO.Client/Game/Managers/` - Game feature managers
- `src/ClassicUO.Client/Game/UI/Gumps/` - User interface windows

#### Asset Management
- `src/ClassicUO.Assets/` - UO file format loaders
- `src/ClassicUO.Client/Resources/` - Embedded resources

#### Network Layer  
- `src/ClassicUO.Client/Network/` - Client-server communication
- Includes packet handlers and encryption

### Testing
- **Unit Tests**: `tests/ClassicUO.UnitTests/`
- **Test Framework**: MSTest
- **Coverage**: Primarily utility and I/O functions

## Scripting System Details

### Python Integration
- **Runtime**: IronPython 3.4.2
- **API Classes**: `PyClasses/` directory contains C# wrappers
- **Documentation**: Auto-generated markdown files in `LegionScripting/docs/`

### Script Management
- **Editor**: Built-in script editor (`ScriptEditor.cs`)
- **Browser**: Script file browser (`ScriptBrowser.cs`)
- **Manager**: Script execution manager (`ScriptManagerWindow.cs`)

## Asset System

### UO File Support
TazUO reads original Ultima Online data files:
- **Art**: Static and item graphics
- **Animations**: Character and creature animations
- **Maps**: World geography data
- **Audio**: Music and sound effects
- **Fonts**: Game fonts and text rendering

### Custom Assets
- `src/ClassicUO.Assets/gumpartassets/` - Custom UI graphics
- `src/ClassicUO.Assets/fonts/` - Additional font files
- Modern UI replacements for legacy UO interface elements

## Network Protocol

### Packet Handling
- **Location**: `src/ClassicUO.Client/Network/`
- **Handlers**: `PacketHandlers.cs` - Server message processing
- **Outgoing**: `OutgoingPackets.cs` - Client message generation
- **Enhanced**: Custom packet extensions for TazUO features

### Encryption Support
- Multiple encryption methods supported
- Legacy and modern UO server compatibility

## Performance Considerations

### Rendering
- FNA-based rendering pipeline for cross-platform compatibility
- Texture atlas system for efficient sprite batching
- Customizable graphics effects (XBR scaling, lighting)

### Memory Management
- Object pooling for frequently allocated objects
- Efficient collection management for game entities
- Asset caching and lazy loading

## Debugging and Troubleshooting

### Debug Features
- Network statistics display
- Performance profiler
- Debug gumps for internal state inspection
- Comprehensive logging system

## Contributing Guidelines

### Code Style
- Follow existing C# conventions
- Use meaningful variable and method names
- Document public APIs
- Maintain cross-platform compatibility

### Feature Development
- Scripting features should expose Python APIs for automation
- Test on multiple platforms when possible

### Testing
- Add unit tests for utility functions
- Test UI changes with different resolutions
- Verify script API changes don't break existing scripts

## Useful Commands

### Building
```bash
# Build release version
dotnet build -c Release

# Build debug version  
dotnet build -c Debug
```

### Testing
```bash
dotnet test tests/ClassicUO.UnitTests/
```

### Documentation Generation
The scripting API documentation is automatically generated during build via the `APIToMarkdown` project.

## External Resources

- **Original ClassicUO**: https://github.com/andreakarasho/ClassicUO
- **FNA Documentation**: https://fna-xna.github.io/
- **Ultima Online Technical Resources**: Various community sites for UO file format documentation
- **Discord Community**: Active development and user community

- All json serialize and deserialize need to have context generated for them.
- Don't put a licsense at the top of files you create.
