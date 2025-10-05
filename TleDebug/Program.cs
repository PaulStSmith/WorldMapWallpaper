using System;
using WorldMapWallpaper.Shared;

Console.WriteLine("TLE Debugging - Comparing with Reference Implementation");
Console.WriteLine("=====================================================");

// Get fresh TLE data
var tleService = new TleDataService(msg => Console.WriteLine($"[LOG] {msg}"));
var tleData = await tleService.FetchIssTleAsync();

if (tleData == null)
{
    Console.WriteLine("Failed to fetch TLE data");
    return;
}

Console.WriteLine($"\nUsing TLE Data:");
Console.WriteLine($"Satellite: {tleData.SatelliteName}");
Console.WriteLine($"Epoch: {tleData.EpochDate:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine($"Line 1: {tleData.Line1}");
Console.WriteLine($"Line 2: {tleData.Line2}");

var satellite = SatelliteRecord.ParseFromTle(tleData);
if (satellite == null)
{
    Console.WriteLine("Failed to parse TLE");
    return;
}

Console.WriteLine($"\nParsed Orbital Elements:");
Console.WriteLine($"Inclination: {OrbitalMath.RadiansToDegrees(satellite.Inclination):F4}°");
Console.WriteLine($"RAAN: {OrbitalMath.RadiansToDegrees(satellite.RightAscensionOfAscendingNode):F4}°");
Console.WriteLine($"Eccentricity: {satellite.Eccentricity:F6}");
Console.WriteLine($"Arg of Perigee: {OrbitalMath.RadiansToDegrees(satellite.ArgumentOfPerigee):F4}°");
Console.WriteLine($"Mean Anomaly: {OrbitalMath.RadiansToDegrees(satellite.MeanAnomaly):F4}°");
Console.WriteLine($"Mean Motion: {satellite.MeanMotion:F8} rad/min");
Console.WriteLine($"Period: {satellite.OrbitalPeriodMinutes:F2} minutes");

// Calculate position for current time
var currentTime = DateTime.UtcNow;
Console.WriteLine($"\nCalculating for time: {currentTime:yyyy-MM-dd HH:mm:ss} UTC");

var minutesSinceEpoch = (currentTime - satellite.Epoch).TotalMinutes;
Console.WriteLine($"Minutes since epoch: {minutesSinceEpoch:F2}");

// Test both implementations
var positionVelocity1 = BasicOrbitalPropagator.Propagate(satellite, currentTime);
var positionVelocity2 = FixedOrbitalPropagator.Propagate(satellite, currentTime);
Console.WriteLine($"\nBasic Implementation:");
Console.WriteLine($"ECI Position: {positionVelocity1.Position}");
Console.WriteLine($"Magnitude: {positionVelocity1.Position.Magnitude:F1} km");

Console.WriteLine($"\nFixed Implementation:");
Console.WriteLine($"ECI Position: {positionVelocity2.Position}");
Console.WriteLine($"Magnitude: {positionVelocity2.Position.Magnitude:F1} km");

// Convert both to geodetic
var gmst = OrbitalMath.GreenwichMeanSiderealTime(currentTime);
Console.WriteLine($"\nGMST: {gmst:F6} rad ({OrbitalMath.RadiansToDegrees(gmst):F2}°)");

// Validate GMST calculation with another method
var jd = OrbitalMath.JulianDateFromUtc(currentTime);
Console.WriteLine($"Julian Date: {jd:F6}");

// Simple GMST check - approximate formula
var gmstHours = (currentTime.Hour + currentTime.Minute / 60.0 + currentTime.Second / 3600.0);
var gmstDegrees = (gmstHours / 24.0) * 360.0;
Console.WriteLine($"Simple GMST check: {gmstDegrees:F2}° (just based on time of day)");

var geodetic1 = CoordinateTransforms.EciToGeodetic(positionVelocity1.Position, gmst);
var geodetic2 = CoordinateTransforms.EciToGeodetic(positionVelocity2.Position, gmst);

Console.WriteLine($"\nBasic Position:");
Console.WriteLine($"Latitude: {geodetic1.LatitudeDegrees:F6}°");
Console.WriteLine($"Longitude: {geodetic1.LongitudeDegrees:F6}°");
Console.WriteLine($"Altitude: {geodetic1.Altitude:F1} km");

Console.WriteLine($"\nFixed Position:");
Console.WriteLine($"Latitude: {geodetic2.LatitudeDegrees:F6}°");
Console.WriteLine($"Longitude: {geodetic2.LongitudeDegrees:F6}°");
Console.WriteLine($"Altitude: {geodetic2.Altitude:F1} km");

// Let's also try a simple position calculation using just mean motion
Console.WriteLine($"\nSimple Mean Motion Check:");
var simpleAngle = satellite.MeanAnomaly + satellite.MeanMotion * minutesSinceEpoch;
Console.WriteLine($"Simple angle after {minutesSinceEpoch:F1} min: {OrbitalMath.RadiansToDegrees(simpleAngle):F2}°");

// Check if TLE epoch is very old
var epochAge = (currentTime - satellite.Epoch).TotalHours;
Console.WriteLine($"\nTLE Age: {epochAge:F1} hours");
if (epochAge > 48)
{
    Console.WriteLine("WARNING: TLE data is more than 2 days old - position may be inaccurate");
}

Console.WriteLine("\nDone!");

// Run additional time test
TleDebug.TimeTest.RunTimeTests();