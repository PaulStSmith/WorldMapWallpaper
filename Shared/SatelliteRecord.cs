using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Represents parsed satellite orbital elements from TLE data for use in SGP4 calculations.
    /// </summary>
    public class SatelliteRecord
    {
        /// <summary>
        /// Gets or sets the satellite name.
        /// </summary>
        public string SatelliteName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the NORAD catalog number.
        /// </summary>
        public int CatalogNumber { get; set; }

        /// <summary>
        /// Gets or sets the epoch time when the orbital elements were calculated.
        /// </summary>
        public DateTime Epoch { get; set; }

        /// <summary>
        /// Gets or sets the orbital inclination in radians.
        /// </summary>
        public double Inclination { get; set; }

        /// <summary>
        /// Gets or sets the right ascension of ascending node in radians.
        /// </summary>
        public double RightAscensionOfAscendingNode { get; set; }

        /// <summary>
        /// Gets or sets the orbital eccentricity (0 = circular, approaching 1 = highly elliptical).
        /// </summary>
        public double Eccentricity { get; set; }

        /// <summary>
        /// Gets or sets the argument of perigee in radians.
        /// </summary>
        public double ArgumentOfPerigee { get; set; }

        /// <summary>
        /// Gets or sets the mean anomaly in radians.
        /// </summary>
        public double MeanAnomaly { get; set; }

        /// <summary>
        /// Gets or sets the mean motion in radians per minute.
        /// </summary>
        public double MeanMotion { get; set; }

        /// <summary>
        /// Gets or sets the first derivative of mean motion (rad/min²).
        /// </summary>
        public double MeanMotionDot { get; set; }

        /// <summary>
        /// Gets or sets the second derivative of mean motion (rad/min³).
        /// </summary>
        public double MeanMotionDotDot { get; set; }

        /// <summary>
        /// Gets or sets the drag term (1/earth radii).
        /// </summary>
        public double BStar { get; set; }

        /// <summary>
        /// Gets or sets the element set number.
        /// </summary>
        public int ElementSetNumber { get; set; }

        /// <summary>
        /// Gets or sets the revolution number at epoch.
        /// </summary>
        public int RevolutionNumberAtEpoch { get; set; }

        /// <summary>
        /// Parses a TLE into a SatelliteRecord with all orbital elements.
        /// </summary>
        /// <param name="tleData">The TLE data to parse.</param>
        /// <returns>A new SatelliteRecord instance or null if parsing fails.</returns>
        public static SatelliteRecord? ParseFromTle(TleData tleData)
        {
            if (tleData == null || !tleData.IsValid())
                return null;

            try
            {
                var line1 = tleData.Line1;
                var line2 = tleData.Line2;

                var record = new SatelliteRecord
                {
                    SatelliteName = tleData.SatelliteName,
                    CatalogNumber = tleData.CatalogNumber,
                    Epoch = tleData.EpochDate
                };

                // Parse Line 1
                record.MeanMotionDot = ParseScientificNotation(line1.Substring(33, 10)) * 2.0 * Math.PI / (24.0 * 60.0 * 24.0 * 60.0);
                record.MeanMotionDotDot = ParseScientificNotation(line1.Substring(44, 8)) * 6.0 * Math.PI / (24.0 * 60.0 * 24.0 * 60.0 * 24.0 * 60.0);
                record.BStar = ParseScientificNotation(line1.Substring(53, 8));
                record.ElementSetNumber = int.Parse(line1.Substring(64, 4).Trim());

                // Parse Line 2
                record.Inclination = double.Parse(line2.Substring(8, 8)) * Math.PI / 180.0;
                record.RightAscensionOfAscendingNode = double.Parse(line2.Substring(17, 8)) * Math.PI / 180.0;
                record.Eccentricity = double.Parse("0." + line2.Substring(26, 7));
                record.ArgumentOfPerigee = double.Parse(line2.Substring(34, 8)) * Math.PI / 180.0;
                record.MeanAnomaly = double.Parse(line2.Substring(43, 8)) * Math.PI / 180.0;
                record.MeanMotion = double.Parse(line2.Substring(52, 11)) * 2.0 * Math.PI / (24.0 * 60.0);
                record.RevolutionNumberAtEpoch = int.Parse(line2.Substring(63, 5).Trim());

                return record;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Parses scientific notation used in TLE format (e.g., "-11606-4" means -0.11606e-4).
        /// </summary>
        /// <param name="value">The scientific notation string from TLE.</param>
        /// <returns>The parsed double value.</returns>
        private static double ParseScientificNotation(string value)
        {
            value = value.Trim();
            
            if (string.IsNullOrEmpty(value))
                return 0.0;

            // Handle special TLE scientific notation format
            if (value.Contains("-") && !value.StartsWith("-"))
            {
                // Format like "23354-3" means 0.23354e-3
                var parts = value.Split('-');
                if (parts.Length == 2)
                {
                    var mantissa = double.Parse("0." + parts[0]);
                    var exponent = -int.Parse(parts[1]);
                    return mantissa * Math.Pow(10, exponent);
                }
            }
            else if (value.Contains("+"))
            {
                // Format like "23354+3" means 0.23354e+3
                var parts = value.Split('+');
                if (parts.Length == 2)
                {
                    var mantissa = double.Parse("0." + parts[0]);
                    var exponent = int.Parse(parts[1]);
                    return mantissa * Math.Pow(10, exponent);
                }
            }
            else if (value.StartsWith("-"))
            {
                // Handle negative values like "-11606-4"
                value = value.Substring(1); // Remove leading minus
                var negativeResult = ParseScientificNotation(value);
                return -negativeResult;
            }

            // Fallback to standard parsing
            return double.TryParse(value, out var result) ? result : 0.0;
        }

        /// <summary>
        /// Gets the orbital period in minutes.
        /// </summary>
        public double OrbitalPeriodMinutes => (2.0 * Math.PI) / MeanMotion;

        /// <summary>
        /// Gets the orbital period as a TimeSpan.
        /// </summary>
        public TimeSpan OrbitalPeriod => TimeSpan.FromMinutes(OrbitalPeriodMinutes);

        /// <summary>
        /// Returns a string representation of the satellite record.
        /// </summary>
        /// <returns>A formatted string with key orbital parameters.</returns>
        public override string ToString()
        {
            return $"{SatelliteName} (#{CatalogNumber}) - Epoch: {Epoch:yyyy-MM-dd HH:mm:ss} UTC, " +
                   $"Inc: {Inclination * 180 / Math.PI:F2}°, Period: {OrbitalPeriodMinutes:F1}min";
        }
    }
}