# TLE Integration Strategy for WorldMapWallpaper

## Overview
This document outlines the practical implementation strategy for integrating Two-Line Element (TLE) data with SGP4 calculations to replace the current ISS API tracking system.

## Current TLE Data Analysis

### ISS TLE Data Source: https://celestrak.org/NORAD/elements/stations.txt

**Current ISS Entry (October 4, 2025):**
```
ISS (ZARYA)
1 25544U 98067A   25277.53072227  .00012744  00000+0  23354-3 0  9998
2 25544  51.6320 125.1332 0000980 198.3499 161.7454 15.49674063532122
```

### TLE Field Analysis
- **Satellite Number**: 25544 (NORAD catalog number for ISS)
- **Epoch**: Day 277.53072227 of 2025 (October 4, 2025, 12:44:14 UTC)
- **Inclination**: 51.6320° (matches current hardcoded 51.6° in ISSTracker.cs)
- **Mean Motion**: 15.49674063 revolutions/day (~92.8 min orbital period)
- **Eccentricity**: 0.0000980 (nearly circular orbit)

## Implementation Strategy

### Phase 1: TLE Data Management

#### 1.1 TLE Parser Implementation
```csharp
namespace WorldMapWallpaper.Orbital
{
    public class TleData
    {
        public string SatelliteName { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public DateTime ParsedDate { get; set; }
        
        public static TleData ParseFromText(string tleText)
        {
            var lines = tleText.Split('\n');
            // Parse 3-line TLE format
            // Validate checksums
            // Extract satellite name
        }
    }
    
    public class TleParser
    {
        public static SatelliteRecord ParseTle(string line1, string line2)
        {
            // Extract all orbital elements from TLE format
            // Handle assumed decimal points and special formatting
            // Convert to internal SatelliteRecord structure
        }
    }
}
```

#### 1.2 TLE Data Fetcher
```csharp
public class TleDataService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private const string TLE_URL = "https://celestrak.org/NORAD/elements/stations.txt";
    private const int ISS_CATALOG_NUMBER = 25544;
    
    public async Task<TleData?> FetchIsstle()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(TLE_URL);
            return ExtractIssTle(response);
        }
        catch (Exception ex)
        {
            logger.Debug($"Failed to fetch TLE data: {ex.Message}");
            return null;
        }
    }
    
    private TleData? ExtractIssTle(string allTleData)
    {
        // Parse the full stations.txt file
        // Find ISS entry by catalog number 25544
        // Return structured TLE data
    }
}
```

### Phase 2: SGP4 Core Implementation

#### 2.1 Mathematical Utilities
```csharp
public static class OrbitalMath
{
    public const double EARTH_RADIUS_KM = 6378.137;
    public const double J2 = 1.08262668E-3;
    public const double PI = Math.PI;
    
    public static double DegreesToRadians(double degrees) => degrees * PI / 180.0;
    public static double RadiansToDegrees(double radians) => radians * 180.0 / PI;
    
    public static double JulianDateFromUtc(DateTime utc)
    {
        // Convert UTC to Julian date for SGP4 calculations
    }
    
    public static double GreenwichSiderealTime(double julianDate)
    {
        // Calculate GMST for coordinate transformations
    }
}
```

#### 2.2 SGP4 Algorithm Core
```csharp
public class Sgp4Propagator
{
    public static (Vector3 position, Vector3 velocity) Propagate(SatelliteRecord satrec, DateTime utc)
    {
        var minutesSinceEpoch = (utc - satrec.Epoch).TotalMinutes;
        
        // Apply SGP4 algorithm:
        // 1. Update orbital elements for secular variations
        // 2. Solve Kepler's equation for eccentric anomaly
        // 3. Calculate position and velocity in orbital plane
        // 4. Transform to Earth-Centered Inertial coordinates
        
        return (position, velocity);
    }
}
```

#### 2.3 Coordinate Transformations
```csharp
public static class CoordinateTransforms
{
    public static (double lat, double lon, double alt) EciToGeodetic(Vector3 eci, double gmst)
    {
        // Convert ECI coordinates to latitude/longitude/altitude
        // Apply Earth ellipsoid corrections
        // Handle coordinate system rotation
    }
}
```

### Phase 3: Integration with ISSTracker.cs

#### 3.1 Modified ISSTracker Architecture
```csharp
internal partial class ISSTracker
{
    private static SatelliteRecord? _issSatellite;
    private static DateTime _tleLastUpdated = DateTime.MinValue;
    private static readonly TleDataService _tleService = new();
    
    private const double TLE_UPDATE_INTERVAL_HOURS = 168; // Weekly updates
    private const double TLE_CACHE_VALID_DAYS = 14;      // Cache valid for 2 weeks
    
    private async Task<bool> UpdateTleDataIfNeeded()
    {
        // Check if TLE data needs refresh
        if ((DateTime.UtcNow - _tleLastUpdated).TotalHours > TLE_UPDATE_INTERVAL_HOURS)
        {
            var tleData = await _tleService.FetchIsstle();
            if (tleData != null)
            {
                _issSatellite = TleParser.ParseTle(tleData.Line1, tleData.Line2);
                _tleLastUpdated = DateTime.UtcNow;
                
                // Cache to disk
                await CacheTleData(tleData);
                return true;
            }
        }
        return _issSatellite != null;
    }
    
    private ISSPosition? GetCurrentPositionSgp4()
    {
        if (_issSatellite == null) return null;
        
        try
        {
            // Use SGP4 to calculate current position
            var (eciPos, _) = Sgp4Propagator.Propagate(_issSatellite, DateTime.UtcNow);
            var gmst = OrbitalMath.GreenwichSiderealTime(OrbitalMath.JulianDateFromUtc(DateTime.UtcNow));
            var (lat, lon, alt) = CoordinateTransforms.EciToGeodetic(eciPos, gmst);
            
            return new ISSPosition(
                OrbitalMath.RadiansToDegrees(lat),
                OrbitalMath.RadiansToDegrees(lon),
                DateTime.UtcNow,
                false // Sunlight calculation unchanged
            );
        }
        catch (Exception ex)
        {
            logger.Debug($"SGP4 calculation failed: {ex.Message}");
            return null;
        }
    }
}
```

#### 3.2 Fallback Strategy
```csharp
public ISSPosition? GetCurrentPosition()
{
    // 1. Try SGP4 method first
    if (await UpdateTleDataIfNeeded())
    {
        var sgp4Position = GetCurrentPositionSgp4();
        if (sgp4Position != null)
        {
            logger.Debug("ISS position calculated using SGP4");
            return sgp4Position;
        }
    }
    
    // 2. Fallback to current API method
    logger.Debug("SGP4 failed, falling back to API method");
    var apiPosition = TryGetPositionFromAPI();
    if (apiPosition != null) return apiPosition;
    
    // 3. Last resort: predict from cache
    logger.Debug("API failed, predicting from cache");
    return PredictPositionFromCache();
}
```

### Phase 4: Caching Strategy

#### 4.1 TLE Data Caching
```csharp
private async Task CacheTleData(TleData tleData)
{
    try
    {
        var cacheData = new
        {
            SatelliteName = tleData.SatelliteName,
            Line1 = tleData.Line1,
            Line2 = tleData.Line2,
            CachedAt = DateTime.UtcNow,
            Source = "CelesTrak"
        };
        
        var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
        var tleCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WorldMapWallpaper", 
            "iss_tle_cache.json");
            
        await File.WriteAllTextAsync(tleCachePath, json);
        logger.Debug($"Cached TLE data to {tleCachePath}");
    }
    catch (Exception ex)
    {
        logger.Debug($"Failed to cache TLE data: {ex.Message}");
    }
}
```

#### 4.2 Cache Validation
```csharp
private async Task<TleData?> LoadTleFromCache()
{
    try
    {
        var tleCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WorldMapWallpaper", 
            "iss_tle_cache.json");
            
        if (!File.Exists(tleCachePath)) return null;
        
        var json = await File.ReadAllTextAsync(tleCachePath);
        var cacheData = JsonConvert.DeserializeAnonymousType(json, new
        {
            SatelliteName = "",
            Line1 = "",
            Line2 = "",
            CachedAt = DateTime.MinValue,
            Source = ""
        });
        
        // Check if cache is still valid (within 2 weeks)
        if ((DateTime.UtcNow - cacheData.CachedAt).TotalDays > TLE_CACHE_VALID_DAYS)
        {
            logger.Debug("TLE cache expired");
            return null;
        }
        
        return new TleData
        {
            SatelliteName = cacheData.SatelliteName,
            Line1 = cacheData.Line1,
            Line2 = cacheData.Line2,
            ParsedDate = cacheData.CachedAt
        };
    }
    catch (Exception ex)
    {
        logger.Debug($"Failed to load TLE from cache: {ex.Message}");
        return null;
    }
}
```

### Phase 5: Configuration Options

#### 5.1 Settings Integration
Add to `Settings.cs`:
```csharp
public class Settings
{
    // Existing settings...
    
    [JsonProperty("use_sgp4_tracking")]
    public bool UseSgp4Tracking { get; set; } = true;
    
    [JsonProperty("tle_update_interval_hours")]
    public int TleUpdateIntervalHours { get; set; } = 168; // Weekly
    
    [JsonProperty("backup_api_enabled")]
    public bool BackupApiEnabled { get; set; } = true;
}
```

#### 5.2 Settings UI Integration
Add to `SettingsForm.cs`:
```csharp
// New checkbox for SGP4 tracking
private CheckBox chkUseSgp4;
private Label lblTleStatus;

private void InitializeSgp4Controls()
{
    chkUseSgp4 = new CheckBox
    {
        Text = "Use SGP4 orbital calculations (more accurate)",
        Checked = settings.UseSgp4Tracking
    };
    
    lblTleStatus = new Label
    {
        Text = "TLE Status: Checking...",
        ForeColor = Color.Gray
    };
    
    // Update TLE status display
    UpdateTleStatusDisplay();
}
```

## Validation Strategy

### Testing Approach
1. **Mathematical Validation**: Compare SGP4 output with satellite-js for same TLE data
2. **Position Accuracy**: Verify calculated positions against known ISS locations
3. **Performance Testing**: Ensure calculations complete within acceptable time
4. **Fallback Testing**: Verify graceful degradation when TLE data unavailable

### Reference Data
- Use satellite-js online demo for cross-validation
- Compare with NASA/NORAD official position data
- Test with historical TLE data for known ISS positions

## Migration Timeline

### Week 1: Core Implementation
- [ ] Implement TLE parser and data structures
- [ ] Create basic coordinate transformation functions
- [ ] Set up TLE data fetching from CelesTrak

### Week 2: SGP4 Algorithm
- [ ] Port core SGP4 mathematical functions
- [ ] Implement position calculation pipeline
- [ ] Add comprehensive error handling

### Week 3: Integration & Testing
- [ ] Integrate with existing ISSTracker.cs
- [ ] Implement caching and fallback logic
- [ ] Extensive testing and validation

### Week 4: Polish & Documentation
- [ ] Add Settings UI integration
- [ ] Performance optimization
- [ ] Update documentation and comments

## Benefits Summary

### Immediate Benefits
- **Reliability**: 99%+ uptime vs. API dependency
- **Accuracy**: Professional-grade orbital mechanics
- **Performance**: No network latency for position calculation

### Future Possibilities  
- **Multiple Satellites**: Easy extension to track Hubble, Tiangong, etc.
- **Pass Predictions**: Show future ISS visibility from user location
- **Orbital Visualization**: Enhanced orbital path display

## Risk Mitigation

### Technical Risks
- **Mathematical Complexity**: Incremental implementation with extensive testing
- **Precision Requirements**: Use double-precision throughout, validate against references
- **Integration Complexity**: Maintain existing fallback systems during transition

### Operational Risks
- **TLE Data Availability**: Multiple sources, local caching, graceful degradation
- **Performance Impact**: Optimize calculations, consider caching computed positions
- **User Experience**: Seamless transition, configuration options for power users