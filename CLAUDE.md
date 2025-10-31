# TazUO Project Guide for Claude

## Project Overview

**TazUO** is a feature-rich fork of ClassicUO, an open-source implementation of the Ultima Online Classic Client. Originally forked to add quality-of-life features requested by users, TazUO has evolved into an independent project that selectively incorporates updates from the original ClassicUO while focusing on enhanced gameplay features.

- **Repository**: https://github.com/PlayTazUO/TazUO
- **Language**: C# targeting .NET 9
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

## Core Features

### Custom Scripting System
TazUO includes two powerful scripting systems:

1. **Legion Scripting** (`src/ClassicUO.Client/LegionScripting/`)
   - Custom scripting language designed for UO automation
   - Documentation: `src/ClassicUO.Client/LegionScripting/LScript.md`
   - Commands for movement, combat, item manipulation, etc.

2. **Python Integration** (`external/iplib/`)
   - Full IronPython runtime included
   - Python API classes in `src/ClassicUO.Client/LegionScripting/PyClasses/`
   - Auto-generated documentation via `src/APIToMarkdown/`

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
- **Framework**: .NET 9
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

### Legion Script
- **Location**: `src/ClassicUO.Client/LegionScripting/`
- **Language Files**: `Lexer.cs`, `Interpreter.cs`, `TextParser.cs`
- **Commands**: Movement, combat, item manipulation, UI interaction
- **Documentation**: Comprehensive command reference in `LScript.md`

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
- Scripting features should have both Legion Script and Python APIs
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
