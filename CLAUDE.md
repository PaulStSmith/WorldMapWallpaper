# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WorldMapWallpaper is a comprehensive C# Windows desktop application suite that generates and manages dynamic desktop wallpapers showing a world map with real-time day/night cycle visualization. The application calculates the current terminator line (day/night boundary) based on UTC time and solar declination, then composites day and night earth images with political boundaries, timezone clocks, and ISS tracking.

## Build Commands

### Development Build
```bash
build.bat
```
or
```bash
dotnet build WorldMapWallpaper.sln
```

### Release Build, Package & GitHub Release
```bash
build.bat Release
```
This builds all projects, publishes the main application, creates an NSIS installer, and automatically creates a GitHub release.

### Manual Commands
```bash
# Build specific project
dotnet build ImagePainter\WorldMapWallpaper.csproj

# Publish main application
dotnet publish ImagePainter\WorldMapWallpaper.csproj -p:PublishProfile=Publish_x64

# Create installer (after publishing)
makensis ImagePainter\InstallMaker.nsi
```

## Solution Architecture

The solution consists of three projects:

### 1. ImagePainter (Main Application)
**Path**: `ImagePainter\WorldMapWallpaper.csproj`
**Type**: WinExe (Windows Application)
**Purpose**: Core wallpaper generation and rendering engine

Key Components:
- **Program.cs** - Main entry point with complete wallpaper generation logic
- **ISSTracker.cs** - International Space Station tracking and visualization
- **Logger.cs** - Custom logging system (file + Windows Event Log)
- **GaussianBlur.cs** - High-performance image blur implementation
- **Trig.cs** - Trigonometric helper functions in degrees

### 2. Settings (Configuration UI)
**Path**: `Settings\WorldMapWallpaper.Settings.csproj`
**Type**: WinExe (Windows Forms Application)
**Purpose**: Modern settings interface with system theme support

Key Components:
- **SettingsForm.cs** - Main configuration interface with dark/light theme support
- **Program.cs** - Settings application entry point

### 3. Shared (Common Library)
**Path**: `Shared\WorldMapWallpaper.Shared.csproj`
**Type**: Library
**Purpose**: Shared components and utilities used by both applications

Key Components:
- **Settings.cs** - Centralized configuration management
- **ThemeManager.cs** - Windows theme detection and color scheme management
- **TaskManager.cs** - Windows scheduled task management using TaskScheduler library
- **WallpaperMonitor.cs** - Monitors wallpaper changes and detects when user switches away
- **UpdateInterval.cs** - Enumeration for update frequency options
- **SPI.cs/SPIF.cs** - Windows API enums for SystemParametersInfo

## Key Features & Algorithms

### Core Rendering Engine
- **Solar Position Calculation**: Uses vernal equinox as reference point to calculate sun's declination
- **Terminator Curve**: Calculates day/night boundary using spherical trigonometry 
- **ISS Tracking**: Real-time position fetching with sunlight calculations and orbital visualization
- **Time Zone Visualization**: Draws 24 analog clocks showing local time for each timezone
- **Image Blending**: Uses alpha masks and unsafe bitmap manipulation for performance

### Settings & Management
- **Theme-Aware UI**: Automatically detects and applies Windows dark/light theme
- **Real-time Configuration**: Settings changes are immediately applied and saved
- **Task Scheduling**: Manages Windows scheduled tasks for automatic wallpaper updates
- **Wallpaper Monitoring**: Detects when user changes to different wallpaper and disables automation

### Build & Distribution
- **Automated Versioning**: Date-based versioning (YY.MMdd format) with automatic Assembly updates
- **Self-Contained Publishing**: Creates portable executables with embedded dependencies
- **NSIS Installer**: Professional installer with proper permissions and task setup
- **GitHub Integration**: Automatic release creation with versioned installers

## Dependencies

### Main Application (ImagePainter)
- `Costura.Fody` (6.0.0) - Assembly embedding and merging
- `Newtonsoft.Json` (13.0.4) - JSON parsing for ISS API data
- `System.Diagnostics.EventLog` (9.0.9) - Windows Event Log integration
- `System.Drawing.Common` (9.0.9) - Image manipulation and graphics
- `System.Drawing.Primitives` (4.3.0) - Basic drawing types
- `System.Resources.Extensions` (9.0.9) - Enhanced resource management

### Settings Application
- `Costura.Fody` (6.0.0) - Assembly embedding and merging

### Shared Library
- `TaskScheduler` (2.12.1) - Windows Task Scheduler management

## Resources

### Embedded Resources (`ImagePainter\Properties\Resources.resx`)
- World political map overlay
- Day/night earth textures (multiple resolutions)
- Clock face graphics (32px and 48px)
- ISS icon for position display
- Application icons

### External Resources (`ImagePainter\Resources\`)
- High-resolution earth textures (4096x2048)
- Vector world map (SVG)
- Multiple wallpaper resolutions (1920x1080)

## Build System Features

### Advanced Build Script (`build.bat`)
- **Multi-Configuration Support**: Handles both Debug and Release builds
- **Git Integration**: Checks for clean working tree before releases
- **Automatic MSBuild Detection**: Finds and uses appropriate Visual Studio installation
- **Solution-Wide Building**: Builds all projects in dependency order
- **Publishing Pipeline**: Automated publish with profile-based configuration
- **NSIS Integration**: Creates professional Windows installer
- **GitHub Releases**: Automatic release creation with version management and asset uploads
- **Version Conflict Resolution**: Handles existing tags with automatic suffixing

### Versioning System (`DateVersioning.targets`)
- **Automatic Version Updates**: Updates AssemblyVersion and AssemblyFileVersion with current date
- **YY.MMdd Format**: Uses year and day-of-year for version numbering
- **Build Integration**: Runs before each build to maintain version consistency
- **Assembly Synchronization**: Keeps all projects in sync with consistent versioning

## Installation & Automation

### Professional Installer (`ImagePainter\InstallMaker.nsi`)
- **Program Files Deployment**: Installs to `C:\Program Files\WorldMapWallpaper`
- **Event Log Setup**: Creates Windows Event Log source for application logging
- **Scheduled Task Creation**: Sets up automated wallpaper updates with multiple triggers:
  - System startup
  - User logon
  - System wake from sleep
  - Configurable time intervals (5min, 10min, 15min, 30min, hourly)
- **Permissions Configuration**: Sets up proper access rights for log directories and task execution

### Task Management Integration
- **Dynamic Schedule Updates**: Settings UI can modify task intervals without reinstallation
- **Task Status Monitoring**: Real-time display of task state and next execution time
- **Multi-Trigger Support**: Maintains boot, logon, and wake triggers while allowing interval changes
- **Privilege Management**: Handles UAC and administrative requirements for task modification

## Development Notes

### Technical Requirements
- **Target Framework**: .NET 9.0 Windows (10.0.17763.0 minimum)
- **Platform**: Windows 10 version 1809 or later
- **Architecture**: AnyCPU with x64 publishing
- **Unsafe Code**: Enabled for high-performance bitmap manipulation
- **Assembly Embedding**: Uses Costura.Fody for single-file distribution

### Performance Optimizations
- **Parallel Processing**: Gaussian blur and image operations use multi-threading
- **File Locking Avoidance**: Alternates between output filenames to prevent conflicts
- **Memory Management**: Proper disposal patterns for graphics resources
- **Caching Strategy**: ISS data caching for offline operation

### Error Handling & Logging
- **Comprehensive Logging**: Custom logger with dual output (file + Windows Event Log)
- **Graceful Degradation**: Continues operation when optional features fail
- **Network Resilience**: Handles ISS API failures with cached data fallback
- **Exception Management**: Try-catch blocks with meaningful error reporting

### Modern UI Features
- **System Theme Integration**: Automatic dark/light theme detection and application
- **Responsive Design**: Proper DPI scaling and modern control styling
- **Real-time Feedback**: Immediate visual feedback for setting changes
- **Status Monitoring**: Live display of system state and task information