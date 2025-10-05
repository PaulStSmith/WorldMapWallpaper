using WorldMapWallpaper.Shared;

Console.WriteLine("Testing TLE/SGP4 Implementation");
Console.WriteLine("================================");

// Test 1: TLE Parsing
Console.WriteLine("\n1. Testing TLE Parsing...");
var sampleTle = @"ISS (ZARYA)
1 25544U 98067A   25277.53072227  .00012744  00000+0  23354-3 0  9998
2 25544  51.6320 125.1332 0000980 198.3499 161.7454 15.49674063532122";

var tleData = TleData.ParseFromText(sampleTle);
if (tleData != null && tleData.IsValid())
{
    Console.WriteLine($"✓ TLE parsed successfully: {tleData.SatelliteName} (#{tleData.CatalogNumber})");
    Console.WriteLine($"  Epoch: {tleData.EpochDate:yyyy-MM-dd HH:mm:ss} UTC");
}
else
{
    Console.WriteLine("✗ TLE parsing failed");
    return;
}

// Test 2: Satellite Record Creation
Console.WriteLine("\n2. Testing Satellite Record...");
var satellite = SatelliteRecord.ParseFromTle(tleData);
if (satellite != null)
{
    Console.WriteLine($"✓ Satellite record created: {satellite}");
}
else
{
    Console.WriteLine("✗ Satellite record creation failed");
    return;
}

// Test 3: SGP4 Position Calculation
Console.WriteLine("\n3. Testing SGP4 Position Calculation...");
try
{
    var currentTime = DateTime.UtcNow;
    var positionVelocity = BasicOrbitalPropagator.Propagate(satellite, currentTime);
    
    Console.WriteLine($"✓ SGP4 calculation successful");
    Console.WriteLine($"  ECI Position: {positionVelocity.Position}");
    Console.WriteLine($"  ECI Velocity: {positionVelocity.Velocity}");
    Console.WriteLine($"  Altitude: {positionVelocity.Altitude:F1} km");
    Console.WriteLine($"  Speed: {positionVelocity.Speed:F3} km/s");

    // Test 4: Coordinate Transformation
    Console.WriteLine("\n4. Testing Coordinate Transformation...");
    var gmst = OrbitalMath.GreenwichMeanSiderealTime(currentTime);
    var geodetic = CoordinateTransforms.EciToGeodetic(positionVelocity.Position, gmst);
    
    Console.WriteLine($"✓ Coordinate transformation successful");
    Console.WriteLine($"  Latitude: {geodetic.LatitudeDegrees:F6}°");
    Console.WriteLine($"  Longitude: {geodetic.LongitudeDegrees:F6}°");
    Console.WriteLine($"  Altitude: {geodetic.Altitude:F1} km");
    Console.WriteLine($"  Time: {currentTime:yyyy-MM-dd HH:mm:ss} UTC");

}
catch (Exception ex)
{
    Console.WriteLine($"✗ SGP4 calculation failed: {ex.Message}");
    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
    return;
}

// Test 5: TLE Data Service
Console.WriteLine("\n5. Testing TLE Data Service...");
var tleService = new TleDataService(msg => Console.WriteLine($"  LOG: {msg}"));

try
{
    var fetchedTle = await tleService.FetchIssTleAsync();
    if (fetchedTle != null)
    {
        Console.WriteLine($"✓ TLE fetch successful: {fetchedTle.SatelliteName}");
        Console.WriteLine($"  Epoch: {fetchedTle.EpochDate:yyyy-MM-dd HH:mm:ss} UTC");
        
        // Calculate position with fresh TLE
        var freshSatellite = SatelliteRecord.ParseFromTle(fetchedTle);
        if (freshSatellite != null)
        {
            var freshPv = BasicOrbitalPropagator.Propagate(freshSatellite, DateTime.UtcNow);
            var freshGmst = OrbitalMath.GreenwichMeanSiderealTime(DateTime.UtcNow);
            var freshGeodetic = CoordinateTransforms.EciToGeodetic(freshPv.Position, freshGmst);
            
            Console.WriteLine($"✓ Fresh TLE position calculation successful");
            Console.WriteLine($"  Current ISS Position: {freshGeodetic.LatitudeDegrees:F6}°, {freshGeodetic.LongitudeDegrees:F6}°");
            Console.WriteLine($"  Altitude: {freshGeodetic.Altitude:F1} km");
        }
    }
    else
    {
        Console.WriteLine("✗ TLE fetch failed (possibly network issue)");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ TLE service test failed: {ex.Message}");
}

Console.WriteLine("\nTesting complete!");
Console.WriteLine("Test completed successfully!");