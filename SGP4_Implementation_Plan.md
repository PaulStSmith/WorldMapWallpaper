# SGP4 Implementation Plan for WorldMapWallpaper

## Executive Summary
**YES, we can definitely extract and port the necessary calculations from satellite-js to C#.** The JavaScript library is well-structured, uses standard algorithms, and has a clean separation of concerns that will translate well to C#.

## Current vs. Proposed Approach

### Current ISSTracker.cs Limitations
- **API Dependency**: Relies on `http://api.open-notify.org/iss-now.json`
- **Simplified Orbital Mechanics**: Uses basic constants (92.68min period, 51.6° inclination)
- **Limited Prediction**: Basic linear extrapolation from cached positions
- **Network Reliability**: Fails if API is down or slow

### SGP4-Based Advantages
- **Professional Accuracy**: Industry-standard orbital propagation (~1km precision)
- **Complete Offline Operation**: No real-time API dependency
- **Predictive Capability**: Calculate future ISS positions for any time
- **Real Orbital Mechanics**: Accounts for atmospheric drag, gravitational perturbations

## Implementation Architecture

### Core Components to Port

#### 1. **TLE Parser** (`io.ts` equivalent)
```csharp
public class TleParser
{
    public static SatelliteRecord ParseTle(string line1, string line2)
    {
        // Parse standardized TLE format
        // Extract orbital elements: inclination, RAAN, eccentricity, etc.
        // Handle checksums and validation
    }
}
```

#### 2. **SGP4 Algorithm** (`sgp4.ts` equivalent)
```csharp
public class Sgp4Propagator
{
    public static PositionVelocity Propagate(SatelliteRecord satrec, DateTime time)
    {
        // Core SGP4 orbital propagation algorithm
        // Apply secular gravity and atmospheric drag
        // Solve Kepler's equation iteratively
        // Return ECI position/velocity vectors
    }
}
```

#### 3. **Coordinate Transformations** (`transforms.ts` equivalent)
```csharp
public static class CoordinateTransforms
{
    public static GeodeticPosition EciToGeodetic(EciVector eci, double gmst)
    {
        // Convert Earth-Centered Inertial to Lat/Lon/Alt
        // Apply Earth ellipsoid calculations
        // Handle Greenwich sidereal time conversion
    }
    
    public static double GreenwichSiderealTime(DateTime utc)
    {
        // Calculate GMST for coordinate transformation
    }
}
```

#### 4. **Data Structures**
```csharp
public class SatelliteRecord
{
    public double Inclination { get; set; }        // Orbital inclination
    public double RightAscension { get; set; }     // RAAN
    public double Eccentricity { get; set; }       // Orbital eccentricity
    public double ArgumentOfPerigee { get; set; }  // Argument of perigee
    public double MeanAnomaly { get; set; }        // Mean anomaly
    public double MeanMotion { get; set; }         // Mean motion (rev/day)
    public DateTime Epoch { get; set; }            // TLE epoch time
    // Additional SGP4-specific parameters...
}

public struct GeodeticPosition
{
    public double Latitude { get; init; }    // Radians
    public double Longitude { get; init; }   // Radians  
    public double Altitude { get; init; }    // Kilometers
}

public struct EciVector
{
    public double X { get; init; }           // Kilometers
    public double Y { get; init; }           // Kilometers
    public double Z { get; init; }           // Kilometers
}
```

### Integration with Existing ISSTracker.cs

#### Modified Architecture
```csharp
internal partial class ISSTracker
{
    private static SatelliteRecord? _issSatRec;
    private static DateTime _tleLastUpdated;
    private const double TLE_UPDATE_HOURS = 168; // Weekly updates

    public ISSPosition? GetCurrentPosition()
    {
        // 1. Check if TLE data needs refresh
        if (NeedsTleUpdate())
            UpdateTleData();
            
        // 2. Use SGP4 to calculate current position
        if (_issSatRec != null)
        {
            var eci = Sgp4Propagator.Propagate(_issSatRec, DateTime.UtcNow);
            var gmst = CoordinateTransforms.GreenwichSiderealTime(DateTime.UtcNow);
            var geodetic = CoordinateTransforms.EciToGeodetic(eci.Position, gmst);
            
            return new ISSPosition(
                Math.Toz(geodetic.Latitude) * 180 / Math.PI,  // Convert to degrees
                Math.Toz(geodetic.Longitude) * 180 / Math.PI, // Convert to degrees
                DateTime.UtcNow,
                false // Sunlight calculation remains the same
            );
        }
        
        // 3. Fallback to current API method if SGP4 fails
        return TryGetPositionFromAPI();
    }
}
```

## Mathematical Complexity Assessment

### Core Algorithms Needed
1. **Kepler Equation Solver**: Iterative solution for eccentric anomaly
2. **Orbital Element Updates**: Apply secular perturbations over time  
3. **Coordinate Rotations**: 3D vector transformations between reference frames
4. **Time Conversions**: UTC to Julian dates, sidereal time calculations

### Numerical Considerations
- **Double Precision**: Sufficient for ~1km accuracy requirements
- **Trigonometric Functions**: Standard Math library functions adequate
- **Iterative Convergence**: Kepler equation typically converges in 3-5 iterations
- **Edge Cases**: Handle near-circular orbits, orbital decay detection

## Implementation Phases

### Phase 1: Core Mathematical Functions
- [ ] Implement basic coordinate transformation utilities
- [ ] Port trigonometric helper functions
- [ ] Create time conversion utilities (UTC ↔ Julian dates)

### Phase 2: TLE Parsing
- [ ] Create TLE format parser with validation
- [ ] Implement checksum verification
- [ ] Add error handling for malformed TLE data

### Phase 3: SGP4 Algorithm
- [ ] Port main SGP4 propagation algorithm
- [ ] Implement orbital element initialization
- [ ] Add perturbation calculations (drag, gravity)

### Phase 4: Integration
- [ ] Modify ISSTracker.cs to use SGP4 calculations
- [ ] Add TLE data source management (CelesTrak API)
- [ ] Implement graceful fallback to existing API method

### Phase 5: Testing & Validation
- [ ] Compare results with satellite-js test cases
- [ ] Validate against known ISS positions
- [ ] Performance testing and optimization

## TLE Data Management Strategy

### Data Sources
- **Primary**: CelesTrak stations.txt (https://celestrak.org/NORAD/elements/stations.txt)
- **Backup**: Space-Track.org (requires registration)
- **Fallback**: Current Open Notify API

### Update Strategy
```csharp
private async Task<bool> UpdateTleData()
{
    try
    {
        // Download latest TLE data for ISS (catalog #25544)
        var tleData = await FetchIsseTle();
        _issSatRec = TleParser.ParseTle(tleData.Line1, tleData.Line2);
        _tleLastUpdated = DateTime.UtcNow;
        
        // Cache to disk for offline operation
        await CacheTleData(tleData);
        return true;
    }
    catch
    {
        // Load from cache if network fails
        return LoadTleFromCache();
    }
}
```

### Caching Strategy
- **TLE Cache**: Store TLE data locally (update weekly)
- **Position Cache**: Keep calculated positions for verification
- **Fallback Logic**: Use cached TLE → current API → last known position

## Benefits Analysis

### Reliability Improvements
- **99% Uptime**: No dependency on external real-time APIs
- **Offline Capability**: Works without internet connection
- **Predictive**: Can show future ISS passes and trajectories

### Accuracy Improvements  
- **Professional Grade**: Same algorithms used by NASA/NORAD
- **Real Orbital Mechanics**: Accounts for atmospheric drag, gravitational perturbations
- **Updated Elements**: Weekly TLE updates vs. static orbital parameters

### Feature Possibilities
- **Future Passes**: Show when ISS will be visible from specific locations
- **Orbital Visualization**: More accurate orbital path prediction
- **Multiple Satellites**: Easy to extend to other satellites (Hubble, etc.)

## Risk Assessment

### Implementation Risks
- **Mathematical Complexity**: SGP4 algorithm has many edge cases
- **Numerical Precision**: Need to ensure C# math matches reference implementation
- **Testing Requirements**: Need comprehensive validation against known results

### Mitigation Strategies
- **Incremental Implementation**: Port core functions first, test extensively
- **Reference Validation**: Compare against satellite-js output for same TLE data
- **Fallback Preservation**: Keep existing API method as backup

## Recommendation

**PROCEED WITH IMPLEMENTATION** - This is a worthwhile upgrade that will significantly improve the reliability and accuracy of ISS tracking in WorldMapWallpaper. The satellite-js library provides an excellent reference implementation that can be systematically ported to C#.

**Estimated Development Time**: 2-3 days for core implementation + testing
**Complexity**: Medium - mostly mathematical translation, well-documented algorithms  
**Risk**: Low - can implement incrementally with existing system as fallback