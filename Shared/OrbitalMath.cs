using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Mathematical utilities and constants for orbital calculations.
    /// </summary>
    public static class OrbitalMath
    {
        // Physical constants
        public const double EARTH_RADIUS_KM = 6378.137;
        public const double EARTH_FLATTENING = 1.0 / 298.257223563;
        public const double EARTH_SEMI_MINOR_AXIS_KM = EARTH_RADIUS_KM * (1.0 - EARTH_FLATTENING);
        
        // Gravitational constants
        public const double EARTH_GRAVITATIONAL_PARAMETER = 398600.4418; // km³/s²
        public const double J2 = 1.08262668E-3; // Earth's oblateness coefficient
        
        // Time constants
        public const double MINUTES_PER_DAY = 1440.0;
        public const double SECONDS_PER_DAY = 86400.0;
        public const double JULIAN_DAYS_PER_CENTURY = 36525.0;
        
        // Reference epoch: January 1, 2000, 12:00:00 UTC (J2000.0)
        public static readonly DateTime J2000_EPOCH = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        
        // Unix epoch: January 1, 1970, 00:00:00 UTC
        public static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">Angle in degrees.</param>
        /// <returns>Angle in radians.</returns>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">Angle in radians.</param>
        /// <returns>Angle in degrees.</returns>
        public static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        /// <summary>
        /// Normalizes an angle to the range [0, 2π] radians.
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>Normalized angle in the range [0, 2π].</returns>
        public static double NormalizeAngle(double angle)
        {
            while (angle < 0)
                angle += 2.0 * Math.PI;
            while (angle >= 2.0 * Math.PI)
                angle -= 2.0 * Math.PI;
            return angle;
        }

        /// <summary>
        /// Normalizes an angle to the range [-π, π] radians.
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>Normalized angle in the range [-π, π].</returns>
        public static double NormalizeAngleSigned(double angle)
        {
            while (angle < -Math.PI)
                angle += 2.0 * Math.PI;
            while (angle > Math.PI)
                angle -= 2.0 * Math.PI;
            return angle;
        }

        /// <summary>
        /// Converts a UTC DateTime to Julian Date.
        /// </summary>
        /// <param name="utc">UTC DateTime.</param>
        /// <returns>Julian Date.</returns>
        public static double JulianDateFromUtc(DateTime utc)
        {
            // Ensure we're working with UTC
            if (utc.Kind != DateTimeKind.Utc)
                utc = utc.ToUniversalTime();

            // Calculate Julian Date using standard algorithm
            var year = utc.Year;
            var month = utc.Month;
            var day = utc.Day;
            var hour = utc.Hour;
            var minute = utc.Minute;
            var second = utc.Second + utc.Millisecond / 1000.0;

            // Adjust for January and February
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            var a = year / 100;
            var b = 2 - a + (a / 4);

            var jd = Math.Floor(365.25 * (year + 4716)) +
                     Math.Floor(30.6001 * (month + 1)) +
                     day + b - 1524.5 +
                     (hour + minute / 60.0 + second / 3600.0) / 24.0;

            return jd;
        }

        /// <summary>
        /// Calculates Greenwich Mean Sidereal Time (GMST) in radians for a given Julian Date.
        /// Uses the IAU-82 formula as implemented in NASA/Vallado SGP4.
        /// </summary>
        /// <param name="julianDate">Julian Date.</param>
        /// <returns>Greenwich Mean Sidereal Time in radians.</returns>
        public static double GreenwichMeanSiderealTime(double julianDate)
        {
            // Julian centuries since J2000.0 epoch (2451545.0)
            var tut1 = (julianDate - 2451545.0) / JULIAN_DAYS_PER_CENTURY;

            // GMST in seconds (IAU-82 formula from Vallado 2007, Eq 3-43)
            var gmstSeconds = -6.2e-6 * tut1 * tut1 * tut1 + 
                             0.093104 * tut1 * tut1 + 
                             (876600.0 * 3600 + 8640184.812866) * tut1 + 
                             67310.54841;

            // Convert seconds to radians (1 day = 86400 seconds = 2π radians)
            var gmstRadians = (gmstSeconds / SECONDS_PER_DAY) * 2.0 * Math.PI;
            
            return NormalizeAngle(gmstRadians);
        }

        /// <summary>
        /// Calculates Greenwich Mean Sidereal Time (GMST) in radians for a given UTC DateTime.
        /// </summary>
        /// <param name="utc">UTC DateTime.</param>
        /// <returns>Greenwich Mean Sidereal Time in radians.</returns>
        public static double GreenwichMeanSiderealTime(DateTime utc)
        {
            return GreenwichMeanSiderealTime(JulianDateFromUtc(utc));
        }

        /// <summary>
        /// Solves Kepler's equation iteratively to find the eccentric anomaly.
        /// </summary>
        /// <param name="meanAnomaly">Mean anomaly in radians.</param>
        /// <param name="eccentricity">Orbital eccentricity.</param>
        /// <param name="tolerance">Convergence tolerance (default: 1e-12).</param>
        /// <param name="maxIterations">Maximum number of iterations (default: 50).</param>
        /// <returns>Eccentric anomaly in radians.</returns>
        public static double SolveKeplerEquation(double meanAnomaly, double eccentricity, 
            double tolerance = 1e-12, int maxIterations = 50)
        {
            // Normalize mean anomaly
            meanAnomaly = NormalizeAngle(meanAnomaly);

            // Initial guess for eccentric anomaly
            var eccentricAnomaly = meanAnomaly;

            // Newton-Raphson iteration
            for (var i = 0; i < maxIterations; i++)
            {
                var delta = (eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly) - meanAnomaly) /
                           (1.0 - eccentricity * Math.Cos(eccentricAnomaly));
                
                eccentricAnomaly -= delta;

                if (Math.Abs(delta) < tolerance)
                    break;
            }

            return eccentricAnomaly;
        }

        /// <summary>
        /// Calculates the true anomaly from eccentric anomaly and eccentricity.
        /// </summary>
        /// <param name="eccentricAnomaly">Eccentric anomaly in radians.</param>
        /// <param name="eccentricity">Orbital eccentricity.</param>
        /// <returns>True anomaly in radians.</returns>
        public static double TrueAnomalyFromEccentricAnomaly(double eccentricAnomaly, double eccentricity)
        {
            var sinE = Math.Sin(eccentricAnomaly);
            var cosE = Math.Cos(eccentricAnomaly);
            
            var beta = eccentricity / (1.0 + Math.Sqrt(1.0 - eccentricity * eccentricity));
            var trueAnomaly = eccentricAnomaly + 2.0 * Math.Atan(beta * sinE / (1.0 - beta * cosE));
            
            return NormalizeAngle(trueAnomaly);
        }

        /// <summary>
        /// Calculates the radius vector (distance from Earth center) given eccentric anomaly and semi-major axis.
        /// </summary>
        /// <param name="eccentricAnomaly">Eccentric anomaly in radians.</param>
        /// <param name="semiMajorAxis">Semi-major axis in kilometers.</param>
        /// <param name="eccentricity">Orbital eccentricity.</param>
        /// <returns>Radius vector in kilometers.</returns>
        public static double RadiusVector(double eccentricAnomaly, double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * (1.0 - eccentricity * Math.Cos(eccentricAnomaly));
        }

        /// <summary>
        /// Calculates the semi-major axis from mean motion.
        /// </summary>
        /// <param name="meanMotionRadPerMin">Mean motion in radians per minute.</param>
        /// <returns>Semi-major axis in kilometers.</returns>
        public static double SemiMajorAxisFromMeanMotion(double meanMotionRadPerMin)
        {
            // Convert rad/min to rad/s
            var meanMotionRadPerSec = meanMotionRadPerMin / 60.0;
            
            // Calculate semi-major axis using Kepler's third law
            var n2 = meanMotionRadPerSec * meanMotionRadPerSec;
            return Math.Pow(EARTH_GRAVITATIONAL_PARAMETER / n2, 1.0 / 3.0);
        }

        /// <summary>
        /// Performs modulo operation that always returns a positive result.
        /// </summary>
        /// <param name="value">The value to apply modulo to.</param>
        /// <param name="modulus">The modulus value.</param>
        /// <returns>The positive modulo result.</returns>
        public static double Modulo(double value, double modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        /// <summary>
        /// Checks if a number is approximately zero within a tolerance.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="tolerance">The tolerance (default: 1e-12).</param>
        /// <returns>True if the value is approximately zero.</returns>
        public static bool IsApproximatelyZero(double value, double tolerance = 1e-12)
        {
            return Math.Abs(value) < tolerance;
        }

        /// <summary>
        /// Checks if two values are approximately equal within a tolerance.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <param name="tolerance">The tolerance (default: 1e-12).</param>
        /// <returns>True if the values are approximately equal.</returns>
        public static bool AreApproximatelyEqual(double a, double b, double tolerance = 1e-12)
        {
            return Math.Abs(a - b) < tolerance;
        }
    }
}