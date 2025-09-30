# WorldMapWallpaper

A dynamic Windows desktop wallpaper application that generates real-time world maps showing the current day/night terminator line, timezone clocks, and International Space Station position.

## Features

- **Real-time day/night cycle** - Calculates and displays the actual solar terminator line based on current UTC time and astronomical calculations
- **Timezone visualization** - Shows 24 analog clocks across time zones with accurate local times
- **ISS tracking** - Displays the current position of the International Space Station (when internet connection is available)
- **Automatic scheduling** - Installer sets up scheduled tasks to update wallpaper on boot, login, wake, and hourly
- **High-performance rendering** - Uses optimized image processing with Gaussian blur for smooth day/night transitions

## How It Works

The application performs sophisticated astronomical calculations:
1. Calculates solar declination using vernal equinox as reference
2. Generates the terminator curve using spherical trigonometry 
3. Creates alpha masks for smooth day/night blending
4. Composites multiple image layers (day earth, night earth, political boundaries)
5. Overlays timezone clocks and ISS position
6. Sets the result as desktop wallpaper

## Installation

1. Run `Install.exe` to install the application and set up automatic scheduling
2. The installer will:
   - Deploy files to `C:\Program Files\WorldMapWallpaper`
   - Create Windows Event Log source for logging
   - Set up scheduled task to run automatically
   - Generate the first wallpaper immediately

## Manual Usage

Run the executable manually to generate and set a new wallpaper:
```
WorldMapWallpaper.exe
```

## Requirements

- Windows 10 or later
- .NET 7.0 Runtime (included in self-contained build)
- Internet connection (optional, for ISS tracking)

## File Locations

- **Generated wallpapers**: `%USERPROFILE%\Pictures\WorldMap01.jpg` or `WorldMap02.jpg`
- **Logs**: `C:\Program Files\WorldMapWallpaper\log\WorldMapWallpaper.log`
- **Event Log**: Windows Event Log under "World Map Wallpaper Source"

## Task Scheduler Configuration

If using Windows Task Scheduler manually, ensure:
- "Run only when user is logged on" option is selected
- Task runs with highest privileges
- Multiple instances policy is set to "Do not start a new instance"

## Technical Details

- **ISS API**: Uses Open Notify API (http://api.open-notify.org/iss-now.json)
- **Image format**: Generates JPEG wallpapers for optimal file size
- **Performance**: Uses parallel processing for image operations

## Example Output

![World map with day/night cycle](https://paulstsmith.github.io/images/worldTimeMap.jpg)

*The actual wallpaper includes real-time terminator calculations, timezone clocks, and ISS position overlay.*
