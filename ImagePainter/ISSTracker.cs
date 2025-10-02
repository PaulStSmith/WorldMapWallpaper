using Newtonsoft.Json;
using System.Drawing;
using WorldMapWallpaper.Properties;

namespace WorldMapWallpaper
{
    /// <summary>
    /// Tracks the International Space Station position using the Open Notify API.<br/>
    /// Provides functionality to fetch ISS coordinates, calculate sunlight status,
    /// and render the ISS position on a world map.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ISSTracker"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance for debug and info messages.</param>
    /// <param name="timeOffset">Time offset for terminator calculation.</param>
    /// <param name="declination">Solar declination for terminator calculation.</param>
    /// <param name="showOrbit">Whether to draw the ISS orbital path.</param>
    internal partial class ISSTracker(Logger logger, double timeOffset, double declination, bool showOrbit = true)
    {
        /// <summary>
        /// Shared HTTP client instance with a 5-second timeout for API requests.
        /// </summary>
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        /// <summary>
        /// Cache file path for storing last known ISS position.
        /// </summary>
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "WorldMapWallpaper", 
            "iss_cache.json");

        /// <summary>
        /// ISS icon for day conditions (in sunlight).
        /// </summary>
        private readonly Bitmap _dayIcon = Resources.ISSIcon;

        /// <summary>
        /// ISS icon for night conditions (in shadow).
        /// </summary>
        private readonly Bitmap _nightIcon = Resources.ISSIcon;

        /// <summary>
        /// ISS orbital parameters for calculations.
        /// </summary>
        private static class OrbitalConstants
        {
            /// <summary>
            /// ISS orbital period in minutes.
            /// </summary>
            public const double OrbitalPeriodMinutes = 92.68;

            /// <summary>
            /// Earth's rotation rate in degrees per minute.
            /// </summary>
            public const double EarthRotationRate = 360.0 / (24.0 * 60.0);

            /// <summary>
            /// ISS maximum latitude (orbital inclination) in degrees.
            /// </summary>
            public const double MaxLatitude = 51.6;

            /// <summary>
            /// ISS ground speed relative to Earth's surface in degrees per minute.
            /// </summary>
            public static double GroundSpeed => 360.0 / OrbitalPeriodMinutes - EarthRotationRate;
        }

        /// <summary>
        /// Calculates the current orbital phase of the ISS based on its position.
        /// </summary>
        /// <param name="position">Current ISS position.</param>
        /// <param name="isAscending">Whether the ISS is on an ascending orbital pass.</param>
        /// <returns>Orbital phase in degrees.</returns>
        private static double CalculateOrbitalPhase(ISSPosition position, bool isAscending)
        {
            var basePhaseDegrees = Math.Asin(position.Latitude / OrbitalConstants.MaxLatitude) * (180.0 / Math.PI);
            return isAscending ? basePhaseDegrees : (180.0 - basePhaseDegrees);
        }

        /// <summary>
        /// Calculates ISS latitude at a given orbital phase.
        /// </summary>
        /// <param name="phaseInDegrees">Orbital phase in degrees.</param>
        /// <returns>Latitude in degrees.</returns>
        private static double CalculateLatitudeFromPhase(double phaseInDegrees)
        {
            return OrbitalConstants.MaxLatitude * Math.Sin(phaseInDegrees * Math.PI / 180.0);
        }

        /// <summary>
        /// Calculates ISS longitude at a given time offset from a reference position.
        /// </summary>
        /// <param name="referenceLongitude">Reference longitude in degrees.</param>
        /// <param name="minutesFromReference">Time offset in minutes from reference position.</param>
        /// <returns>Predicted longitude in degrees, normalized to [-180, 180].</returns>
        private static double CalculateLongitudeFromTimeOffset(double referenceLongitude, double minutesFromReference)
        {
            var longitude = referenceLongitude - (minutesFromReference * OrbitalConstants.GroundSpeed);
            
            // Normalize longitude to [-180, 180]
            while (longitude > 180) longitude -= 360;
            while (longitude < -180) longitude += 360;
            
            return longitude;
        }

        /// <summary>
        /// Calculates the orbital phase change over a given time period.
        /// </summary>
        /// <param name="minutes">Time period in minutes.</param>
        /// <returns>Phase change in degrees.</returns>
        private static double CalculatePhaseChange(double minutes)
        {
            return (minutes / OrbitalConstants.OrbitalPeriodMinutes) * 360.0;
        }

        /// <summary>
        /// Unified ISS data class that handles both API responses and cache storage.
        /// System.Text.Json automatically handles string-to-double conversions.
        /// </summary>
        private class ISSData
        {
            /// <summary>
            /// Gets or sets the message from the API response.
            /// </summary>
            [JsonProperty("message")]
            public string? Message { get; set; }
            
            /// <summary>
            /// Gets or sets the timestamp of the position reading as Unix timestamp.
            /// </summary>
            [JsonProperty("timestamp")]
            public long Timestamp { get; set; }
            
            /// <summary>
            /// Gets or sets the ISS coordinates from the API response.
            /// </summary>
            [JsonProperty("iss_position")]
            public ISSCoordinates? ISSPosition { get; set; }
            
            /// <summary>
            /// Gets or sets the orbital phase in degrees (cache-specific field).
            /// </summary>
            [JsonProperty("orbital_phase")]
            public double? OrbitalPhase { get; set; }
            
            /// <summary>
            /// Gets or sets a value indicating whether the ISS is on an ascending pass (cache-specific field).
            /// </summary>
            [JsonProperty("is_ascending")]
            public bool? IsAscending { get; set; }
        }

        /// <summary>
        /// Represents ISS coordinates with strong typing.
        /// System.Text.Json handles conversion from API strings to doubles automatically.
        /// </summary>
        private class ISSCoordinates
        {
            /// <summary>
            /// Gets or sets the latitude coordinate in degrees.
            /// </summary>
            [JsonProperty("latitude")]
            public double Latitude { get; set; }
            
            /// <summary>
            /// Gets or sets the longitude coordinate in degrees.
            /// </summary>
            [JsonProperty("longitude")]
            public double Longitude { get; set; }
        }

        /// <summary>
        /// Represents the International Space Station position and tracking state.
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of the <see cref="ISSPosition"/> class.
        /// </remarks>
        /// <param name="lat">The latitude coordinate in degrees.</param>
        /// <param name="lon">The longitude coordinate in degrees.</param>
        /// <param name="timestamp">The timestamp of the position reading.</param>
        /// <param name="inSunlight">Whether the ISS is in sunlight at this position.</param>
        public class ISSPosition(double lat, double lon, DateTime timestamp, bool inSunlight)
        {
            /// <summary>
            /// Gets the latitude coordinate of the ISS in degrees (-90 to 90).
            /// </summary>
            public double Latitude { get; init; } = lat;

            /// <summary>
            /// Gets the longitude coordinate of the ISS in degrees (-180 to 180).
            /// </summary>
            public double Longitude { get; init; } = lon;

            /// <summary>
            /// Gets the timestamp when this position was recorded.
            /// </summary>
            public DateTime Timestamp { get; init; } = timestamp;

            /// <summary>
            /// Gets a value indicating whether the ISS is currently in sunlight.
            /// </summary>
            public bool IsInSunlight { get; init; } = inSunlight;
        }

        /// <summary>
        /// Plots the ISS position on the provided world map bitmap.
        /// </summary>
        /// <param name="map">The world map bitmap to draw the ISS position on.</param>
        /// <returns>The map bitmap with ISS position plotted, or the original bitmap if ISS data is unavailable.</returns>
        /// <remarks>
        /// This method fetches the current ISS position, determines if it's in sunlight,
        /// and draws the appropriate icon on the map. If no icons were provided in the constructor
        /// or if the ISS position cannot be retrieved, the original map is returned unchanged.
        /// </remarks>
        public Bitmap PlotISS(Bitmap map)
        {
            try
            {
                var issPosition = GetCurrentPosition();
                if (issPosition == null)
                {
                    logger.Debug("ISS position unavailable, returning original map.");
                    return map;
                }

                // Calculate sunlight status
                issPosition = CalculateSunlightStatus(issPosition, timeOffset, declination);

                // Choose appropriate icon
                var icon = issPosition.IsInSunlight ? _dayIcon : _nightIcon;
                if (icon == null)
                {
                    logger.Debug("No ISS icon available, returning original map.");
                    return map;
                }

                // Create a copy of the map to avoid modifying the original
                var mapCopy = new Bitmap(map);
                using var graphics = Graphics.FromImage(mapCopy);
                
                // Draw the orbit first (so ISS appears on top)
                if (showOrbit)
                {
                    DrawISSOrbit(graphics, issPosition, mapCopy.Width, mapCopy.Height);
                }
                
                // Draw the ISS on top of the orbit
                DrawISS(graphics, issPosition, mapCopy.Width, mapCopy.Height, icon);
                
                return mapCopy;
            }
            catch (Exception ex)
            {
                logger.Debug($"Error plotting ISS: {ex.Message}");
                return map;
            }
        }

        /// <summary>
        /// Gets the current ISS position from the Open Notify API or predicts from cached data.
        /// </summary>
        /// <returns>
        /// The current ISS position or null if neither API nor cached data is available.
        /// </returns>
        private ISSPosition? GetCurrentPosition()
        {
            // Try to get fresh data from API first
            var apiPosition = TryGetPositionFromAPI();
            if (apiPosition != null)
            {
                // Cache the successful API response
                CacheISSPosition(apiPosition);
                return apiPosition;
            }

            // API failed, try to predict from cached data
            logger.Debug("API failed, attempting to predict ISS position from cached data.");
            return PredictPositionFromCache();
        }

        /// <summary>
        /// Attempts to fetch ISS position from the Open Notify API.
        /// </summary>
        /// <returns>ISS position from API or null if failed.</returns>
        private ISSPosition? TryGetPositionFromAPI()
        {
            try
            {
                logger.Debug("Fetching ISS position from Open Notify API.");

                var response = _httpClient.GetStringAsync("http://api.open-notify.org/iss-now.json")
                                          .GetAwaiter()
                                          .GetResult();
                
                // Deserialize JSON response - Newtonsoft.Json handles string-to-double conversion automatically
                var apiData = JsonConvert.DeserializeObject<ISSData>(response);
                
                if (apiData?.ISSPosition != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds(apiData.Timestamp).DateTime;
                    
                    logger.Debug($"ISS position from API: Lat={apiData.ISSPosition.Latitude:F3}, Lon={apiData.ISSPosition.Longitude:F3}");
                    return new ISSPosition(apiData.ISSPosition.Latitude, apiData.ISSPosition.Longitude, timestamp, false);
                }
                else
                {
                    logger.Debug("Invalid API response format or missing data.");
                }
            }
            catch (JsonException ex)
            {
                logger.Debug($"Failed to parse JSON from API: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to fetch ISS position from API: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Caches the ISS position to disk for offline prediction.
        /// </summary>
        /// <param name="position">ISS position to cache.</param>
        private void CacheISSPosition(ISSPosition position)
        {
            try
            {
                // Try to determine if ISS is on ascending or descending pass
                var isAscending = DetermineOrbitalDirection(position);
                
                // Calculate orbital phase for future predictions using shared method
                var orbitalPhase = CalculateOrbitalPhase(position, isAscending);
                
                // Create cache data - Newtonsoft.Json handles type conversions automatically
                var cacheData = new ISSData
                {
                    Message = "cached",
                    Timestamp = ((DateTimeOffset)position.Timestamp).ToUnixTimeSeconds(),
                    ISSPosition = new ISSCoordinates
                    {
                        Latitude = position.Latitude,
                        Longitude = position.Longitude
                    },
                    OrbitalPhase = orbitalPhase,
                    IsAscending = isAscending
                };

                // Ensure cache directory exists
                var cacheDir = Path.GetDirectoryName(CacheFilePath);
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir!);

                // Serialize to JSON using Newtonsoft.Json
                var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
                
                File.WriteAllText(CacheFilePath, json);
                logger.Debug($"Cached ISS position to {CacheFilePath}");
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to cache ISS position: {ex.Message}");
            }
        }

        /// <summary>
        /// Predicts ISS position from cached data using orbital mechanics.
        /// </summary>
        /// <returns>Predicted ISS position or null if no valid cache.</returns>
        private ISSPosition? PredictPositionFromCache()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                {
                    logger.Debug("No ISS cache file found.");
                    return null;
                }

                var json = File.ReadAllText(CacheFilePath);
                
                // Deserialize JSON - Newtonsoft.Json handles type conversions automatically
                var cacheData = JsonConvert.DeserializeObject<ISSData>(json);
                if (cacheData?.ISSPosition == null || cacheData.OrbitalPhase == null)
                {
                    logger.Debug("Failed to deserialize cache file or missing data.");
                    return null;
                }

                // Calculate time elapsed since cache
                var now = DateTime.UtcNow;
                var cachedTime = DateTimeOffset.FromUnixTimeSeconds(cacheData.Timestamp).UtcDateTime;
                var minutesElapsed = (now - cachedTime).TotalMinutes;

                // Predict current position using shared orbital mechanics methods
                var phaseChange = CalculatePhaseChange(minutesElapsed);
                var currentPhase = cacheData.OrbitalPhase.Value + phaseChange;

                // Calculate predicted latitude using shared method
                var predictedLat = CalculateLatitudeFromPhase(currentPhase);

                // Calculate predicted longitude using shared method
                var predictedLon = CalculateLongitudeFromTimeOffset(cacheData.ISSPosition.Longitude, minutesElapsed);

                logger.Debug($"Predicted ISS position from {minutesElapsed:F1}min old cache: Lat={predictedLat:F3}, Lon={predictedLon:F3}");
                
                return new ISSPosition(predictedLat, predictedLon, now, false);
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to predict ISS position from cache: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines if the ISS is on an ascending pass (moving north) or descending pass (moving south).
        /// Uses cached data if available, otherwise makes an educated guess based on longitude.
        /// </summary>
        /// <param name="currentPosition">Current ISS position.</param>
        /// <returns>True if ascending (moving north), false if descending (moving south).</returns>
        private static bool DetermineOrbitalDirection(ISSPosition currentPosition)
        {
            try
            {
                // Try to read previous position from cache
                if (File.Exists(CacheFilePath))
                {
                    var json = File.ReadAllText(CacheFilePath);
                    var previousData = JsonConvert.DeserializeObject<ISSData>(json);
                    
                    if (previousData?.ISSPosition != null)
                    {
                        var timeDiff = (currentPosition.Timestamp - DateTimeOffset.FromUnixTimeSeconds(previousData.Timestamp).DateTime).TotalMinutes;
                        
                        // Only use previous data if it's recent (within 10 minutes)
                        if (Math.Abs(timeDiff) < 10)
                        {
                            var latDiff = currentPosition.Latitude - previousData.ISSPosition.Latitude;
                            
                            // If latitude is increasing, ISS is ascending (moving north)
                            if (Math.Abs(latDiff) > 0.01) // Avoid noise
                            {
                                return latDiff > 0;
                            }
                        }
                    }
                }
                
                // Fallback: Use longitude-based heuristic
                // ISS moves faster over equator, slower at extremes
                // This is a rough approximation based on orbital mechanics
                var absLatitude = Math.Abs(currentPosition.Latitude);
                
                // If near equator, make educated guess based on longitude pattern
                if (absLatitude < 30)
                {
                    // Simple heuristic: assume ascending if in western hemisphere during certain longitude ranges
                    // This is very approximate but better than random
                    var normalizedLon = ((currentPosition.Longitude + 180) % 360) - 180;
                    return normalizedLon < 0;
                }
                
                // If at high latitude, assume based on which extreme we're closer to
                return currentPosition.Latitude > 0; // Ascending if in northern hemisphere
            }
            catch
            {
                // Default fallback: assume ascending
                return true;
            }
        }

        /// <summary>
        /// Determines if the ISS is in sunlight based on the solar terminator calculation.
        /// </summary>
        /// <param name="position">The current ISS position to evaluate.</param>
        /// <param name="timeOffset">The current time offset for terminator calculation in degrees.</param>
        /// <param name="declination">The solar declination angle in radians.</param>
        /// <returns>
        /// A new <see cref="ISSPosition"/> instance with updated sunlight status,
        /// or the original position if calculation fails.
        /// </returns>
        private ISSPosition CalculateSunlightStatus(ISSPosition position, double timeOffset, double declination)
        {
            try
            {
                // Use the shared terminator calculation from the main program
                var terminatorLat = Program.GetTerminatorLatitude(position.Longitude, timeOffset, declination);

                // ISS is in sunlight if it's on the day side of the terminator
                var inSunlight = declination >= 0 && position.Latitude >= terminatorLat ||
                                 declination < 0 && position.Latitude <= terminatorLat;

                logger.Debug($"ISS sunlight status: {(inSunlight ? "Day" : "Night")} (terminator lat: {terminatorLat:F1}°)");
                
                return new ISSPosition(position.Latitude, position.Longitude, position.Timestamp, inSunlight);
            }
            catch
            {
                // If calculation fails, return original position
                return position;
            }
        }

        /// <summary>
        /// Converts ISS latitude and longitude coordinates to pixel coordinates on a world map.
        /// Assumes equirectangular projection - may need adjustment based on actual map projection.
        /// </summary>
        /// <param name="position">The ISS position containing latitude and longitude.</param>
        /// <param name="mapWidth">The width of the map in pixels.</param>
        /// <param name="mapHeight">The height of the map in pixels.</param>
        /// <returns>
        /// A <see cref="Point"/> representing the pixel coordinates on the map,
        /// clamped to map boundaries.
        /// </returns>
        private static Point GetPixelCoordinates(ISSPosition position, int mapWidth, int mapHeight)
        {
            // Convert lat/lon to pixel coordinates (assuming equirectangular projection)
            var x = (int)((position.Longitude + 180.0) * mapWidth / 360.0);
            var y = (int)((90.0 - position.Latitude) * mapHeight / 180.0);
            
            // Ensure coordinates are within bounds
            x = Math.Max(0, Math.Min(mapWidth - 1, x));
            y = Math.Max(0, Math.Min(mapHeight - 1, y));
            
            return new Point(x, y);
        }

        /// <summary>
        /// Draws the ISS icon on the provided graphics context at the calculated position.
        /// </summary>
        /// <param name="graphics">The graphics context to draw on.</param>
        /// <param name="position">The ISS position to render.</param>
        /// <param name="mapWidth">The width of the map in pixels.</param>
        /// <param name="mapHeight">The height of the map in pixels.</param>
        /// <param name="issIcon">The ISS icon bitmap (expected to be 31x31 pixels).</param>
        /// <remarks>
        /// The icon is centered on the ISS position. If the icon is null, no drawing occurs.
        /// </remarks>
        private void DrawISS(Graphics graphics, ISSPosition position, int mapWidth, int mapHeight, Bitmap issIcon)
        {
            if (issIcon == null) return;

            var pixelPos = GetPixelCoordinates(position, mapWidth, mapHeight);
            
            // Center the 31x31 icon on the ISS position
            var drawX = pixelPos.X - (issIcon.Width / 2);
            var drawY = pixelPos.Y - (issIcon.Height / 2);

            graphics.DrawImage(issIcon, drawX, drawY);
            
            // Draw ISS info text under the icon
            DrawISSInfo(graphics, position, pixelPos.X, drawY + issIcon.Height + 2);
            
            logger.Debug($"Drew ISS at pixel coordinates ({pixelPos.X}, {pixelPos.Y})");
        }

        /// <summary>
        /// Draws ISS information text under the icon with name, coordinates, and UTC time.
        /// </summary>
        /// <param name="graphics">The graphics context to draw on.</param>
        /// <param name="position">The ISS position data.</param>
        /// <param name="centerX">X coordinate to center the text horizontally.</param>
        /// <param name="startY">Y coordinate where the text starts.</param>
        private static void DrawISSInfo(Graphics graphics, ISSPosition position, int centerX, int startY)
        {
            using var font = new Font("Arial", 7f, FontStyle.Regular);
            using var whiteBrush = new SolidBrush(Color.White);
            using var shadowBrush = new SolidBrush(Color.FromArgb(128, Color.Black));
            
            var format = new StringFormat { Alignment = StringAlignment.Center };
            
            // Format coordinates with proper hemisphere indicators
            var latDir = position.Latitude >= 0 ? "N" : "S";
            var lonDir = position.Longitude >= 0 ? "E" : "W";
            var latValue = Math.Abs(position.Latitude);
            var lonValue = Math.Abs(position.Longitude);
            
            var lines = new[]
            {
                "ISS",
                $"{latValue:F2}{latDir} {lonValue:F2}{lonDir}",
                $"{position.Timestamp:HH:mm} UTC"
            };
            
            var lineHeight = graphics.MeasureString("A", font).Height;
            
            for (var i = 0; i < lines.Length; i++)
            {
                var y = startY + (i * lineHeight);
                
                // Draw shadow offset by 1 pixel
                graphics.DrawString(lines[i], font, shadowBrush, centerX + 1, y + 1, format);
                // Draw main text
                graphics.DrawString(lines[i], font, whiteBrush, centerX, y, format);
            }
        }

        /// <summary>
        /// Draws the ISS orbital path on the map.
        /// </summary>
        /// <param name="graphics">Graphics context to draw on.</param>
        /// <param name="currentPosition">Current ISS position.</param>
        /// <param name="mapWidth">Map width in pixels.</param>
        /// <param name="mapHeight">Map height in pixels.</param>
        private void DrawISSOrbit(Graphics graphics, ISSPosition currentPosition, int mapWidth, int mapHeight)
        {
            try
            {
                var orbitPoints = new List<PointF>();
                
                // Orbit visualization parameters
                const int orbitSegments = 100;
                const double orbitSpan = 30.0; // ±15 minutes from current
                
                // Calculate what phase of the orbit the ISS is currently in using shared methods
                var isAscending = DetermineOrbitalDirection(currentPosition);
                var currentPhase = CalculateOrbitalPhase(currentPosition, isAscending);
                
                // Calculate orbit points
                for (var i = 0; i < orbitSegments; i++)
                {
                    // Time offset from current position (-15 to +15 minutes)
                    var minutesFromNow = (i - orbitSegments / 2.0) * (orbitSpan / orbitSegments);

                    // Orbital phase at this time using shared method
                    var phaseChange = CalculatePhaseChange(minutesFromNow);
                    var phaseAtTime = currentPhase + phaseChange;

                    // Calculate latitude using shared method
                    var latitude = CalculateLatitudeFromPhase(phaseAtTime);
                    
                    // Calculate longitude using shared method
                    var longitude = CalculateLongitudeFromTimeOffset(currentPosition.Longitude, minutesFromNow);
                    
                    var point = GetPixelCoordinates(
                        new ISSPosition(latitude, longitude, DateTime.UtcNow, false), 
                        mapWidth, mapHeight);
                    orbitPoints.Add(new PointF(point.X, point.Y));
                }
                
                // Draw orbit segments
                if (orbitPoints.Count > 1)
                {
                    using var orbitPen = new Pen(Color.FromArgb(80, Color.Cyan), 2);
                    orbitPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    
                    for (var i = 1; i < orbitPoints.Count; i++)
                    {
                        var p1 = orbitPoints[i - 1];
                        var p2 = orbitPoints[i];
                        
                        // Handle map edge wrapping (don't draw lines across the entire map)
                        var deltaX = Math.Abs(p2.X - p1.X);
                        if (deltaX < mapWidth / 2) // Only draw if points are close
                        {
                            graphics.DrawLine(orbitPen, p1, p2);
                        }
                    }
                    
                    logger.Debug($"Drew ISS orbit with {orbitPoints.Count} segments, current phase: {currentPhase:F1}°");
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Error drawing ISS orbit: {ex.Message}");
            }
        }

    }
}