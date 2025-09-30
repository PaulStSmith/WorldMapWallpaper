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
    internal partial class ISSTracker
    {
        /// <summary>
        /// Shared HTTP client instance with a 5-second timeout for API requests.
        /// </summary>
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        /// <summary>
        /// Logger instance for debugging and informational messages.
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// Time offset for terminator calculation.
        /// </summary>
        private readonly double _timeOffset;

        /// <summary>
        /// Solar declination for terminator calculation.
        /// </summary>
        private readonly double _declination;

        /// <summary>
        /// Whether to draw the ISS orbital path.
        /// </summary>
        private readonly bool _showOrbit;

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
        /// Unified ISS data class that handles both API responses and cache storage.
        /// System.Text.Json automatically handles string-to-double conversions.
        /// </summary>
        private class ISSData
        {
            // API response fields
            [JsonProperty("message")]
            public string? Message { get; set; }
            [JsonProperty("timestamp")]
            public long Timestamp { get; set; }
            [JsonProperty("iss_position")]
            public ISSCoordinates? ISSPosition { get; set; }
            
            // Cache-specific fields (optional, only used for cache storage)
            [JsonProperty("orbital_phase")]
            public double? OrbitalPhase { get; set; }
            [JsonProperty("is_ascending")]
            public bool? IsAscending { get; set; }
        }

        /// <summary>
        /// Represents ISS coordinates with strong typing.
        /// System.Text.Json handles conversion from API strings to doubles automatically.
        /// </summary>
        private class ISSCoordinates
        {
            [JsonProperty("latitude")]
            public double Latitude { get; set; }
            [JsonProperty("longitude")]
            public double Longitude { get; set; }
        }

        /// <summary>
        /// Represents the International Space Station position and tracking state.
        /// </summary>
        public class ISSPosition
        {
            /// <summary>
            /// Gets the latitude coordinate of the ISS in degrees (-90 to 90).
            /// </summary>
            public double Latitude { get; init; }

            /// <summary>
            /// Gets the longitude coordinate of the ISS in degrees (-180 to 180).
            /// </summary>
            public double Longitude { get; init; }

            /// <summary>
            /// Gets the timestamp when this position was recorded.
            /// </summary>
            public DateTime Timestamp { get; init; }

            /// <summary>
            /// Gets a value indicating whether the ISS is currently in sunlight.
            /// </summary>
            public bool IsInSunlight { get; init; }

            /// <summary>
            /// Initializes a new instance of the <see cref="ISSPosition"/> class.
            /// </summary>
            /// <param name="lat">The latitude coordinate in degrees.</param>
            /// <param name="lon">The longitude coordinate in degrees.</param>
            /// <param name="timestamp">The timestamp of the position reading.</param>
            /// <param name="inSunlight">Whether the ISS is in sunlight at this position.</param>
            public ISSPosition(double lat, double lon, DateTime timestamp, bool inSunlight)
            {
                Latitude = lat;
                Longitude = lon;
                Timestamp = timestamp;
                IsInSunlight = inSunlight;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ISSTracker"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for debug and info messages.</param>
        /// <param name="timeOffset">Time offset for terminator calculation.</param>
        /// <param name="declination">Solar declination for terminator calculation.</param>
        /// <param name="showOrbit">Whether to draw the ISS orbital path.</param>
        public ISSTracker(Logger logger, double timeOffset, double declination, bool showOrbit = true)
        {
            _logger = logger;
            _timeOffset = timeOffset;
            _declination = declination;
            _showOrbit = showOrbit;
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
                    _logger.Debug("ISS position unavailable, returning original map.");
                    return map;
                }

                // Calculate sunlight status
                issPosition = CalculateSunlightStatus(issPosition, _timeOffset, _declination);

                // Choose appropriate icon
                var icon = issPosition.IsInSunlight ? _dayIcon : _nightIcon;
                if (icon == null)
                {
                    _logger.Debug("No ISS icon available, returning original map.");
                    return map;
                }

                // Create a copy of the map to avoid modifying the original
                var mapCopy = new Bitmap(map);
                using var graphics = Graphics.FromImage(mapCopy);
                
                // Draw the orbit first (so ISS appears on top)
                if (_showOrbit)
                {
                    DrawISSOrbit(graphics, issPosition, mapCopy.Width, mapCopy.Height);
                }
                
                // Draw the ISS on top of the orbit
                DrawISS(graphics, issPosition, mapCopy.Width, mapCopy.Height, icon);
                
                return mapCopy;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error plotting ISS: {ex.Message}");
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
            _logger.Debug("API failed, attempting to predict ISS position from cached data.");
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
                _logger.Debug("Fetching ISS position from Open Notify API.");

                var response = _httpClient.GetStringAsync("http://api.open-notify.org/iss-now.json")
                                          .GetAwaiter()
                                          .GetResult();
                
                // Deserialize JSON response - Newtonsoft.Json handles string-to-double conversion automatically
                var apiData = JsonConvert.DeserializeObject<ISSData>(response);
                
                if (apiData?.ISSPosition != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds(apiData.Timestamp).DateTime;
                    
                    _logger.Debug($"ISS position from API: Lat={apiData.ISSPosition.Latitude:F3}, Lon={apiData.ISSPosition.Longitude:F3}");
                    return new ISSPosition(apiData.ISSPosition.Latitude, apiData.ISSPosition.Longitude, timestamp, false);
                }
                else
                {
                    _logger.Debug("Invalid API response format or missing data.");
                }
            }
            catch (JsonException ex)
            {
                _logger.Debug($"Failed to parse JSON from API: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to fetch ISS position from API: {ex.Message}");
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
                
                // Calculate orbital phase for future predictions
                var basePhaseDegrees = Math.Asin(position.Latitude / 51.6) * (180.0 / Math.PI);
                
                // Adjust phase based on orbital direction
                var orbitalPhase = isAscending ? basePhaseDegrees : (180.0 - basePhaseDegrees);
                
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
                _logger.Debug($"Cached ISS position to {CacheFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to cache ISS position: {ex.Message}");
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
                    _logger.Debug("No ISS cache file found.");
                    return null;
                }

                var json = File.ReadAllText(CacheFilePath);
                
                // Deserialize JSON - Newtonsoft.Json handles type conversions automatically
                var cacheData = JsonConvert.DeserializeObject<ISSData>(json);
                if (cacheData?.ISSPosition == null || cacheData.OrbitalPhase == null)
                {
                    _logger.Debug("Failed to deserialize cache file or missing data.");
                    return null;
                }

                // Calculate time elapsed since cache
                var now = DateTime.UtcNow;
                var cachedTime = DateTimeOffset.FromUnixTimeSeconds(cacheData.Timestamp).DateTime;
                var minutesElapsed = (now - cachedTime).TotalMinutes;

                // Don't use cache if it's too old (more than 2 hours)
                if (Math.Abs(minutesElapsed) > 120)
                {
                    _logger.Debug($"Cache too old ({minutesElapsed:F1} minutes), not using for prediction.");
                    return null;
                }

                // Predict current position using orbital mechanics
                const double orbitalPeriod = 92.68; // minutes
                const double earthRotationRate = 360.0 / (24.0 * 60.0); // degrees per minute

                // Calculate new orbital phase
                var phaseChange = (minutesElapsed / orbitalPeriod) * 360.0;
                var currentPhase = cacheData.OrbitalPhase.Value + phaseChange;

                // Calculate predicted latitude - use same formula as orbit drawing for consistency
                var predictedLat = 51.6 * Math.Sin(currentPhase * Math.PI / 180.0);

                // Calculate predicted longitude (ISS moves west relative to Earth)
                var issGroundSpeed = 360.0 / orbitalPeriod - earthRotationRate;
                var predictedLon = cacheData.ISSPosition.Longitude - (minutesElapsed * issGroundSpeed);

                // Normalize longitude
                while (predictedLon > 180) predictedLon -= 360;
                while (predictedLon < -180) predictedLon += 360;

                _logger.Debug($"Predicted ISS position from {minutesElapsed:F1}min old cache: Lat={predictedLat:F3}, Lon={predictedLon:F3}");
                
                return new ISSPosition(predictedLat, predictedLon, now, false);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to predict ISS position from cache: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines if the ISS is on an ascending pass (moving north) or descending pass (moving south).
        /// Uses cached data if available, otherwise makes an educated guess based on longitude.
        /// </summary>
        /// <param name="currentPosition">Current ISS position</param>
        /// <returns>True if ascending (moving north), false if descending (moving south)</returns>
        private bool DetermineOrbitalDirection(ISSPosition currentPosition)
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

                _logger.Debug($"ISS sunlight status: {(inSunlight ? "Day" : "Night")} (terminator lat: {terminatorLat:F1}°)");
                
                return new ISSPosition(position.Latitude, position.Longitude, position.Timestamp, inSunlight);
            }
            catch
            {
                // If calculation fails, return original position
                return position;
            }
        }

#pragma warning disable CA1822 // Mark members as static

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
        private Point GetPixelCoordinates(ISSPosition position, int mapWidth, int mapHeight)
        {
            // Convert lat/lon to pixel coordinates (assuming equirectangular projection)
            var x = (int)((position.Longitude + 180.0) * mapWidth / 360.0);
            var y = (int)((90.0 - position.Latitude) * mapHeight / 180.0);
            
            // Ensure coordinates are within bounds
            x = Math.Max(0, Math.Min(mapWidth - 1, x));
            y = Math.Max(0, Math.Min(mapHeight - 1, y));
            
            return new Point(x, y);
        }

#pragma warning restore CA1822 

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
            
            _logger.Debug($"Drew ISS at pixel coordinates ({pixelPos.X}, {pixelPos.Y})");
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
                
                // ISS orbital parameters
                const double orbitalPeriod = 92.68; // minutes (more accurate)
                const double earthRotationRate = 360.0 / (24.0 * 60.0); // degrees per minute
                const int orbitSegments = 100;
                const double orbitSpan = 30.0; // ±15 minutes from current
                
                // Calculate what phase of the orbit the ISS is currently in
                // Determine orbital direction to resolve sine function ambiguity
                var isAscending = DetermineOrbitalDirection(currentPosition);
                var basePhaseDegrees = Math.Asin(currentPosition.Latitude / 51.6) * (180.0 / Math.PI);
                var currentPhase = isAscending ? basePhaseDegrees : (180.0 - basePhaseDegrees);
                
                // Calculate orbit points
                for (var i = 0; i < orbitSegments; i++)
                {
                    // Time offset from current position (-45 to +45 minutes)
                    var minutesFromNow = (i - orbitSegments / 2.0) * (orbitSpan / orbitSegments);

                    // Orbital phase at this time
                    var phaseAtTime = currentPhase + (minutesFromNow / orbitalPeriod) * 360.0;

                    // Calculate latitude from orbital inclination and phase
                    var latitude = 51.6 * Math.Sin((phaseAtTime * Math.PI / 180.0));
                    
                    // Calculate longitude: ISS ground track moves west due to Earth's rotation
                    // ISS orbital velocity relative to Earth's surface
                    var issGroundSpeed = 360.0 / orbitalPeriod - earthRotationRate; // degrees per minute
                    var longitude = currentPosition.Longitude - (minutesFromNow * issGroundSpeed);
                    
                    // Normalize longitude to [-180, 180]
                    while (longitude > 180) longitude -= 360;
                    while (longitude < -180) longitude += 360;
                    
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
                    
                    _logger.Debug($"Drew ISS orbit with {orbitPoints.Count} segments, current phase: {currentPhase:F1}°");
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error drawing ISS orbit: {ex.Message}");
            }
        }

    }
}