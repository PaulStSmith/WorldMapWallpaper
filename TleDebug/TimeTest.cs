using System;
using WorldMapWallpaper.Shared;

namespace TleDebug
{
    public static class TimeTest
    {
        public static void RunTimeTests()
        {
            Console.WriteLine("=== Time Synchronization Test ===");
            
            // Test specific time when live tracker showed São Paulo position
            var testTime = new DateTime(2025, 10, 5, 6, 40, 0, DateTimeKind.Utc);
            Console.WriteLine($"Test Time: {testTime:yyyy-MM-dd HH:mm:ss} UTC");
            
            // Load TLE
            var tleService = new TleDataService(msg => Console.WriteLine($"[LOG] {msg}"));
            var tleData = tleService.FetchIssTleAsync().Result;
            
            if (tleData == null)
            {
                Console.WriteLine("Failed to get TLE data");
                return;
            }
            
            var satellite = SatelliteRecord.ParseFromTle(tleData);
            if (satellite == null)
            {
                Console.WriteLine("Failed to parse TLE");
                return;
            }
            
            Console.WriteLine($"TLE Epoch: {satellite.Epoch:yyyy-MM-dd HH:mm:ss} UTC");
            var ageMinutes = (testTime - satellite.Epoch).TotalMinutes;
            Console.WriteLine($"Minutes since epoch: {ageMinutes:F2}");
            
            // Calculate position
            var posVel = FixedOrbitalPropagator.Propagate(satellite, testTime);
            var gmst = OrbitalMath.GreenwichMeanSiderealTime(testTime);
            var geodetic = CoordinateTransforms.EciToGeodetic(posVel.Position, gmst);
            
            Console.WriteLine($"GMST: {OrbitalMath.RadiansToDegrees(gmst):F2}°");
            Console.WriteLine($"Our Position: Lat {geodetic.LatitudeDegrees:F3}°, Lon {geodetic.LongitudeDegrees:F3}°");
            Console.WriteLine($"Expected (São Paulo area): Lat -23.5°, Lon -46.6°");
            
            var latDiff = Math.Abs(geodetic.LatitudeDegrees - (-23.5));
            var lonDiff = Math.Abs(geodetic.LongitudeDegrees - (-46.6));
            Console.WriteLine($"Differences: Lat {latDiff:F3}°, Lon {lonDiff:F3}°");
            
            // Test orbital velocity
            var expectedSpeed = 2 * Math.PI * (6378.137 + geodetic.Altitude) / (92.9 / 60.0); // km/h
            var actualSpeed = posVel.Velocity.Magnitude * 3600; // Convert km/s to km/h
            Console.WriteLine($"Expected orbital speed: {expectedSpeed:F0} km/h");
            Console.WriteLine($"Calculated speed: {actualSpeed:F0} km/h");
            
            // Test several times to see orbital motion
            Console.WriteLine("\n=== Position Over Time ===");
            for (int i = 0; i < 6; i++)
            {
                var t = testTime.AddMinutes(i * 5);
                var pv = FixedOrbitalPropagator.Propagate(satellite, t);
                var g = OrbitalMath.GreenwichMeanSiderealTime(t);
                var geo = CoordinateTransforms.EciToGeodetic(pv.Position, g);
                
                Console.WriteLine($"T+{i*5:D2}min: Lat {geo.LatitudeDegrees:F2}°, Lon {geo.LongitudeDegrees:F2}°");
            }
        }
    }
}