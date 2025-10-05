using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Coordinate transformation functions for converting between different reference frames.
    /// </summary>
    public static class CoordinateTransforms
    {
        /// <summary>
        /// Converts Earth-Centered Inertial (ECI) coordinates to geodetic coordinates.
        /// </summary>
        /// <param name="eciPosition">ECI position vector in kilometers.</param>
        /// <param name="gmst">Greenwich Mean Sidereal Time in radians.</param>
        /// <returns>Geodetic coordinates (latitude, longitude, altitude).</returns>
        public static GeodeticCoordinates EciToGeodetic(Vector3D eciPosition, double gmst)
        {
            // Convert ECI to ECF (Earth-Centered Fixed) first
            var ecfPosition = EciToEcf(eciPosition, gmst);
            
            // Then convert ECF to geodetic
            return EcfToGeodetic(ecfPosition);
        }

        /// <summary>
        /// Converts Earth-Centered Inertial (ECI) coordinates to Earth-Centered Fixed (ECF) coordinates.
        /// </summary>
        /// <param name="eciPosition">ECI position vector in kilometers.</param>
        /// <param name="gmst">Greenwich Mean Sidereal Time in radians.</param>
        /// <returns>ECF position vector in kilometers.</returns>
        public static Vector3D EciToEcf(Vector3D eciPosition, double gmst)
        {
            var cosGmst = Math.Cos(gmst);
            var sinGmst = Math.Sin(gmst);

            // Rotation matrix from ECI to ECF
            var x = cosGmst * eciPosition.X + sinGmst * eciPosition.Y;
            var y = -sinGmst * eciPosition.X + cosGmst * eciPosition.Y;
            var z = eciPosition.Z;

            return new Vector3D(x, y, z);
        }

        /// <summary>
        /// Converts Earth-Centered Fixed (ECF) coordinates to geodetic coordinates.
        /// Uses iterative algorithm for accurate ellipsoidal Earth model.
        /// </summary>
        /// <param name="ecfPosition">ECF position vector in kilometers.</param>
        /// <returns>Geodetic coordinates (latitude, longitude, altitude).</returns>
        public static GeodeticCoordinates EcfToGeodetic(Vector3D ecfPosition)
        {
            var x = ecfPosition.X;
            var y = ecfPosition.Y;
            var z = ecfPosition.Z;

            // Earth ellipsoid parameters
            var a = OrbitalMath.EARTH_RADIUS_KM; // Semi-major axis
            var f = OrbitalMath.EARTH_FLATTENING; // Flattening
            var b = a * (1.0 - f); // Semi-minor axis
            var e2 = f * (2.0 - f); // First eccentricity squared

            // Calculate longitude
            var longitude = Math.Atan2(y, x);

            // Calculate latitude iteratively
            var p = Math.Sqrt(x * x + y * y); // Distance from z-axis
            var latitude = Math.Atan2(z, p * (1.0 - e2)); // Initial guess
            
            var iterations = 0;
            const int maxIterations = 20;
            const double tolerance = 1e-12;
            
            double prevLatitude;
            double n; // Prime vertical radius of curvature
            
            do
            {
                prevLatitude = latitude;
                var sinLat = Math.Sin(latitude);
                n = a / Math.Sqrt(1.0 - e2 * sinLat * sinLat);
                var tempAltitude = p / Math.Cos(latitude) - n;
                latitude = Math.Atan2(z, p * (1.0 - e2 * n / (n + tempAltitude)));
                iterations++;
            }
            while (Math.Abs(latitude - prevLatitude) > tolerance && iterations < maxIterations);

            // Calculate final altitude
            var sinLatFinal = Math.Sin(latitude);
            n = a / Math.Sqrt(1.0 - e2 * sinLatFinal * sinLatFinal);
            var altitude = p / Math.Cos(latitude) - n;

            return new GeodeticCoordinates(latitude, longitude, altitude);
        }

        /// <summary>
        /// Converts geodetic coordinates to Earth-Centered Fixed (ECF) coordinates.
        /// </summary>
        /// <param name="geodetic">Geodetic coordinates.</param>
        /// <returns>ECF position vector in kilometers.</returns>
        public static Vector3D GeodeticToEcf(GeodeticCoordinates geodetic)
        {
            var lat = geodetic.Latitude;
            var lon = geodetic.Longitude;
            var alt = geodetic.Altitude;

            // Earth ellipsoid parameters
            var a = OrbitalMath.EARTH_RADIUS_KM;
            var f = OrbitalMath.EARTH_FLATTENING;
            var e2 = f * (2.0 - f);

            var sinLat = Math.Sin(lat);
            var cosLat = Math.Cos(lat);
            var sinLon = Math.Sin(lon);
            var cosLon = Math.Cos(lon);

            // Prime vertical radius of curvature
            var n = a / Math.Sqrt(1.0 - e2 * sinLat * sinLat);

            var x = (n + alt) * cosLat * cosLon;
            var y = (n + alt) * cosLat * sinLon;
            var z = (n * (1.0 - e2) + alt) * sinLat;

            return new Vector3D(x, y, z);
        }

        /// <summary>
        /// Converts Earth-Centered Fixed (ECF) coordinates to Earth-Centered Inertial (ECI) coordinates.
        /// </summary>
        /// <param name="ecfPosition">ECF position vector in kilometers.</param>
        /// <param name="gmst">Greenwich Mean Sidereal Time in radians.</param>
        /// <returns>ECI position vector in kilometers.</returns>
        public static Vector3D EcfToEci(Vector3D ecfPosition, double gmst)
        {
            var cosGmst = Math.Cos(gmst);
            var sinGmst = Math.Sin(gmst);

            // Rotation matrix from ECF to ECI (inverse of ECI to ECF)
            var x = cosGmst * ecfPosition.X - sinGmst * ecfPosition.Y;
            var y = sinGmst * ecfPosition.X + cosGmst * ecfPosition.Y;
            var z = ecfPosition.Z;

            return new Vector3D(x, y, z);
        }

        /// <summary>
        /// Calculates look angles (azimuth, elevation, range) from observer to satellite.
        /// </summary>
        /// <param name="observerGeodetic">Observer's geodetic coordinates.</param>
        /// <param name="satelliteEci">Satellite's ECI position.</param>
        /// <param name="gmst">Greenwich Mean Sidereal Time in radians.</param>
        /// <returns>Look angles structure.</returns>
        public static LookAngles CalculateLookAngles(GeodeticCoordinates observerGeodetic, Vector3D satelliteEci, double gmst)
        {
            // Convert observer position to ECF
            var observerEcf = GeodeticToEcf(observerGeodetic);
            
            // Convert satellite position to ECF
            var satelliteEcf = EciToEcf(satelliteEci, gmst);
            
            // Calculate relative position vector
            var relativeEcf = satelliteEcf - observerEcf;
            
            // Transform to topocentric coordinates (East, North, Up)
            var lat = observerGeodetic.Latitude;
            var lon = observerGeodetic.Longitude;
            
            var sinLat = Math.Sin(lat);
            var cosLat = Math.Cos(lat);
            var sinLon = Math.Sin(lon);
            var cosLon = Math.Cos(lon);
            
            var topocentric = new Vector3D(
                -sinLon * relativeEcf.X + cosLon * relativeEcf.Y,
                -sinLat * cosLon * relativeEcf.X - sinLat * sinLon * relativeEcf.Y + cosLat * relativeEcf.Z,
                cosLat * cosLon * relativeEcf.X + cosLat * sinLon * relativeEcf.Y + sinLat * relativeEcf.Z
            );
            
            // Calculate range
            var range = topocentric.Magnitude;
            
            // Calculate azimuth (from North, clockwise)
            var azimuth = Math.Atan2(topocentric.X, topocentric.Y);
            if (azimuth < 0)
                azimuth += 2.0 * Math.PI;
            
            // Calculate elevation (angle above horizon)
            var elevation = Math.Asin(topocentric.Z / range);
            
            return new LookAngles(azimuth, elevation, range);
        }

        /// <summary>
        /// Calculates the sub-satellite point (satellite's projection on Earth surface).
        /// </summary>
        /// <param name="eciPosition">Satellite ECI position.</param>
        /// <param name="gmst">Greenwich Mean Sidereal Time in radians.</param>
        /// <returns>Sub-satellite point as geodetic coordinates.</returns>
        public static GeodeticCoordinates CalculateSubSatellitePoint(Vector3D eciPosition, double gmst)
        {
            return EciToGeodetic(eciPosition, gmst);
        }

        /// <summary>
        /// Calculates the great circle distance between two points on Earth.
        /// </summary>
        /// <param name="point1">First point geodetic coordinates.</param>
        /// <param name="point2">Second point geodetic coordinates.</param>
        /// <returns>Distance in kilometers.</returns>
        public static double GreatCircleDistance(GeodeticCoordinates point1, GeodeticCoordinates point2)
        {
            var lat1 = point1.Latitude;
            var lon1 = point1.Longitude;
            var lat2 = point2.Latitude;
            var lon2 = point2.Longitude;

            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return OrbitalMath.EARTH_RADIUS_KM * c;
        }

        /// <summary>
        /// Calculates the bearing (direction) from one point to another.
        /// </summary>
        /// <param name="from">Starting point.</param>
        /// <param name="to">Destination point.</param>
        /// <returns>Bearing in radians (0 = North, clockwise).</returns>
        public static double CalculateBearing(GeodeticCoordinates from, GeodeticCoordinates to)
        {
            var lat1 = from.Latitude;
            var lon1 = from.Longitude;
            var lat2 = to.Latitude;
            var lon2 = to.Longitude;

            var dLon = lon2 - lon1;

            var x = Math.Sin(dLon) * Math.Cos(lat2);
            var y = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            var bearing = Math.Atan2(x, y);
            
            // Normalize to [0, 2π]
            return OrbitalMath.NormalizeAngle(bearing);
        }
    }

    /// <summary>
    /// Represents look angles from an observer to a satellite.
    /// </summary>
    public struct LookAngles
    {
        /// <summary>
        /// Gets or sets the azimuth angle in radians (0 = North, clockwise).
        /// </summary>
        public double Azimuth { get; set; }

        /// <summary>
        /// Gets or sets the elevation angle in radians (0 = horizon, π/2 = zenith).
        /// </summary>
        public double Elevation { get; set; }

        /// <summary>
        /// Gets or sets the range (distance) in kilometers.
        /// </summary>
        public double Range { get; set; }

        /// <summary>
        /// Initializes a new instance of the LookAngles struct.
        /// </summary>
        /// <param name="azimuth">Azimuth in radians.</param>
        /// <param name="elevation">Elevation in radians.</param>
        /// <param name="range">Range in kilometers.</param>
        public LookAngles(double azimuth, double elevation, double range)
        {
            Azimuth = azimuth;
            Elevation = elevation;
            Range = range;
        }

        /// <summary>
        /// Gets the azimuth in degrees.
        /// </summary>
        public double AzimuthDegrees => OrbitalMath.RadiansToDegrees(Azimuth);

        /// <summary>
        /// Gets the elevation in degrees.
        /// </summary>
        public double ElevationDegrees => OrbitalMath.RadiansToDegrees(Elevation);

        /// <summary>
        /// Gets whether the satellite is visible (above horizon).
        /// </summary>
        public bool IsVisible => Elevation > 0;

        /// <summary>
        /// Returns a string representation of the look angles.
        /// </summary>
        /// <returns>Formatted look angles.</returns>
        public override string ToString()
        {
            return $"Az: {AzimuthDegrees:F1}°, El: {ElevationDegrees:F1}°, Range: {Range:F1} km";
        }
    }
}