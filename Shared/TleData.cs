using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Represents Two-Line Element (TLE) data for satellite orbital calculations.
    /// </summary>
    public class TleData
    {
        /// <summary>
        /// Gets or sets the satellite name from line 0 of the TLE.
        /// </summary>
        public string SatelliteName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first line of TLE data (line 1).
        /// </summary>
        public string Line1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the second line of TLE data (line 2).
        /// </summary>
        public string Line2 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when this TLE data was parsed or retrieved.
        /// </summary>
        public DateTime ParsedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the NORAD catalog number from the TLE data.
        /// </summary>
        public int CatalogNumber
        {
            get
            {
                if (string.IsNullOrEmpty(Line1) || Line1.Length < 7)
                    return 0;
                return int.TryParse(Line1.Substring(2, 5).Trim(), out var number) ? number : 0;
            }
        }

        /// <summary>
        /// Gets the epoch date from the TLE data.
        /// </summary>
        public DateTime EpochDate
        {
            get
            {
                if (string.IsNullOrEmpty(Line1) || Line1.Length < 32)
                    return DateTime.MinValue;

                try
                {
                    var epochYearStr = Line1.Substring(18, 2);
                    var epochDayStr = Line1.Substring(20, 12);

                    if (!int.TryParse(epochYearStr, out var epochYear) || 
                        !double.TryParse(epochDayStr, out var epochDay))
                        return DateTime.MinValue;

                    // Convert 2-digit year to 4-digit (00-56 = 2000-2056, 57-99 = 1957-1999)
                    var fullYear = epochYear < 57 ? 2000 + epochYear : 1900 + epochYear;

                    // Convert day of year to DateTime
                    var yearStart = new DateTime(fullYear, 1, 1);
                    return yearStart.AddDays(epochDay - 1);
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// Validates that this TLE data has the correct format and checksums.
        /// </summary>
        /// <returns>True if the TLE data is valid, false otherwise.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Line1) || string.IsNullOrEmpty(Line2))
                return false;

            if (Line1.Length != 69 || Line2.Length != 69)
                return false;

            if (Line1[0] != '1' || Line2[0] != '2')
                return false;

            // Validate checksums
            return ValidateChecksum(Line1) && ValidateChecksum(Line2);
        }

        /// <summary>
        /// Validates the checksum of a TLE line.
        /// </summary>
        /// <param name="line">The TLE line to validate.</param>
        /// <returns>True if the checksum is valid, false otherwise.</returns>
        private static bool ValidateChecksum(string line)
        {
            if (line.Length != 69)
                return false;

            var checksum = 0;
            for (var i = 0; i < 68; i++)
            {
                var c = line[i];
                if (char.IsDigit(c))
                    checksum += c - '0';
                else if (c == '-')
                    checksum += 1;
            }

            var expectedChecksum = checksum % 10;
            var actualChecksum = line[68] - '0';

            return expectedChecksum == actualChecksum;
        }

        /// <summary>
        /// Parses TLE data from a 3-line string format.
        /// </summary>
        /// <param name="tleText">The TLE text containing satellite name and two data lines.</param>
        /// <returns>A new TleData instance or null if parsing fails.</returns>
        public static TleData? ParseFromText(string tleText)
        {
            if (string.IsNullOrWhiteSpace(tleText))
                return null;

            var lines = tleText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length < 2)
                return null;

            // Handle both 2-line and 3-line formats
            if (lines.Length == 2)
            {
                // 2-line format (no satellite name)
                var tle = new TleData
                {
                    SatelliteName = "Unknown",
                    Line1 = lines[0].Trim(),
                    Line2 = lines[1].Trim(),
                    ParsedDate = DateTime.UtcNow
                };

                return tle.IsValid() ? tle : null;
            }
            else if (lines.Length >= 3)
            {
                // 3-line format (satellite name + two data lines)
                var tle = new TleData
                {
                    SatelliteName = lines[0].Trim(),
                    Line1 = lines[1].Trim(),
                    Line2 = lines[2].Trim(),
                    ParsedDate = DateTime.UtcNow
                };

                return tle.IsValid() ? tle : null;
            }

            return null;
        }

        /// <summary>
        /// Extracts ISS TLE data from a multi-satellite TLE file.
        /// </summary>
        /// <param name="allTleData">The complete TLE file content.</param>
        /// <param name="catalogNumber">The NORAD catalog number to search for (default: 25544 for ISS).</param>
        /// <returns>The ISS TLE data or null if not found.</returns>
        public static TleData? ExtractSatelliteTle(string allTleData, int catalogNumber = 25544)
        {
            if (string.IsNullOrWhiteSpace(allTleData))
                return null;

            var lines = allTleData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (var i = 0; i < lines.Length - 2; i++)
            {
                // Look for a line starting with "1 " followed by the catalog number
                if (lines[i + 1].StartsWith("1 ") && lines[i + 1].Length >= 7)
                {
                    var lineOneCatalog = lines[i + 1].Substring(2, 5).Trim();
                    if (int.TryParse(lineOneCatalog, out var number) && number == catalogNumber)
                    {
                        // Found the satellite, extract its 3-line TLE
                        var satelliteName = lines[i].Trim();
                        var line1 = lines[i + 1].Trim();
                        var line2 = lines[i + 2].Trim();

                        var tle = new TleData
                        {
                            SatelliteName = satelliteName,
                            Line1 = line1,
                            Line2 = line2,
                            ParsedDate = DateTime.UtcNow
                        };

                        return tle.IsValid() ? tle : null;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a string representation of the TLE data.
        /// </summary>
        /// <returns>A formatted TLE string.</returns>
        public override string ToString()
        {
            return $"{SatelliteName}\n{Line1}\n{Line2}";
        }
    }
}