using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Represents a 3D vector for orbital calculations.
    /// </summary>
    public struct Vector3D
    {
        /// <summary>
        /// Gets or sets the X component.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y component.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the Z component.
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// Initializes a new instance of the Vector3D struct.
        /// </summary>
        /// <param name="x">X component.</param>
        /// <param name="y">Y component.</param>
        /// <param name="z">Z component.</param>
        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Gets the magnitude (length) of the vector.
        /// </summary>
        public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

        /// <summary>
        /// Gets the squared magnitude of the vector (avoids square root calculation).
        /// </summary>
        public double MagnitudeSquared => X * X + Y * Y + Z * Z;

        /// <summary>
        /// Gets a normalized version of this vector (unit vector).
        /// </summary>
        public Vector3D Normalized
        {
            get
            {
                var mag = Magnitude;
                return mag > 0 ? new Vector3D(X / mag, Y / mag, Z / mag) : Zero;
            }
        }

        /// <summary>
        /// Gets a zero vector.
        /// </summary>
        public static Vector3D Zero => new(0, 0, 0);

        /// <summary>
        /// Gets a unit vector along the X-axis.
        /// </summary>
        public static Vector3D UnitX => new(1, 0, 0);

        /// <summary>
        /// Gets a unit vector along the Y-axis.
        /// </summary>
        public static Vector3D UnitY => new(0, 1, 0);

        /// <summary>
        /// Gets a unit vector along the Z-axis.
        /// </summary>
        public static Vector3D UnitZ => new(0, 0, 1);

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>Sum of the vectors.</returns>
        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>Difference of the vectors.</returns>
        public static Vector3D operator -(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>Scaled vector.</returns>
        public static Vector3D operator *(Vector3D vector, double scalar)
        {
            return new Vector3D(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar.</param>
        /// <param name="vector">The vector.</param>
        /// <returns>Scaled vector.</returns>
        public static Vector3D operator *(double scalar, Vector3D vector)
        {
            return vector * scalar;
        }

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>Scaled vector.</returns>
        public static Vector3D operator /(Vector3D vector, double scalar)
        {
            return new Vector3D(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
        }

        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>Negated vector.</returns>
        public static Vector3D operator -(Vector3D vector)
        {
            return new Vector3D(-vector.X, -vector.Y, -vector.Z);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>Dot product.</returns>
        public static double Dot(Vector3D a, Vector3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>Cross product vector.</returns>
        public static Vector3D Cross(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        /// <summary>
        /// Calculates the distance between two points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Distance between the points.</returns>
        public static double Distance(Vector3D a, Vector3D b)
        {
            return (a - b).Magnitude;
        }

        /// <summary>
        /// Calculates the squared distance between two points (avoids square root).
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Squared distance between the points.</returns>
        public static double DistanceSquared(Vector3D a, Vector3D b)
        {
            return (a - b).MagnitudeSquared;
        }

        /// <summary>
        /// Linearly interpolates between two vectors.
        /// </summary>
        /// <param name="a">Start vector.</param>
        /// <param name="b">End vector.</param>
        /// <param name="t">Interpolation parameter (0 = a, 1 = b).</param>
        /// <returns>Interpolated vector.</returns>
        public static Vector3D Lerp(Vector3D a, Vector3D b, double t)
        {
            return a + t * (b - a);
        }

        /// <summary>
        /// Determines if two vectors are approximately equal within a tolerance.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <param name="tolerance">Tolerance for comparison.</param>
        /// <returns>True if vectors are approximately equal.</returns>
        public static bool AreApproximatelyEqual(Vector3D a, Vector3D b, double tolerance = 1e-12)
        {
            return Math.Abs(a.X - b.X) < tolerance &&
                   Math.Abs(a.Y - b.Y) < tolerance &&
                   Math.Abs(a.Z - b.Z) < tolerance;
        }

        /// <summary>
        /// Determines equality of two vectors.
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns>True if vectors are equal.</returns>
        public bool Equals(Vector3D other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        /// <summary>
        /// Determines equality with another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if objects are equal.</returns>
        public override bool Equals(object? obj)
        {
            return obj is Vector3D other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this vector.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="left">Left vector.</param>
        /// <param name="right">Right vector.</param>
        /// <returns>True if vectors are equal.</returns>
        public static bool operator ==(Vector3D left, Vector3D right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="left">Left vector.</param>
        /// <param name="right">Right vector.</param>
        /// <returns>True if vectors are not equal.</returns>
        public static bool operator !=(Vector3D left, Vector3D right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns a string representation of the vector.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"({X:F6}, {Y:F6}, {Z:F6})";
        }

        /// <summary>
        /// Returns a string representation with specified format.
        /// </summary>
        /// <param name="format">Number format string.</param>
        /// <returns>Formatted string representation.</returns>
        public string ToString(string format)
        {
            return $"({X.ToString(format)}, {Y.ToString(format)}, {Z.ToString(format)})";
        }
    }

    /// <summary>
    /// Represents position and velocity vectors for orbital calculations.
    /// </summary>
    public struct PositionVelocity
    {
        /// <summary>
        /// Gets or sets the position vector in kilometers.
        /// </summary>
        public Vector3D Position { get; set; }

        /// <summary>
        /// Gets or sets the velocity vector in kilometers per second.
        /// </summary>
        public Vector3D Velocity { get; set; }

        /// <summary>
        /// Initializes a new instance of the PositionVelocity struct.
        /// </summary>
        /// <param name="position">Position vector in kilometers.</param>
        /// <param name="velocity">Velocity vector in kilometers per second.</param>
        public PositionVelocity(Vector3D position, Vector3D velocity)
        {
            Position = position;
            Velocity = velocity;
        }

        /// <summary>
        /// Gets the altitude above Earth's surface in kilometers.
        /// </summary>
        public double Altitude => Position.Magnitude - OrbitalMath.EARTH_RADIUS_KM;

        /// <summary>
        /// Gets the speed in kilometers per second.
        /// </summary>
        public double Speed => Velocity.Magnitude;

        /// <summary>
        /// Returns a string representation of the position and velocity.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"Pos: {Position}, Vel: {Velocity}, Alt: {Altitude:F1} km";
        }
    }

    /// <summary>
    /// Represents geodetic coordinates (latitude, longitude, altitude).
    /// </summary>
    public struct GeodeticCoordinates
    {
        /// <summary>
        /// Gets or sets the latitude in radians.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude in radians.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the altitude in kilometers.
        /// </summary>
        public double Altitude { get; set; }

        /// <summary>
        /// Initializes a new instance of the GeodeticCoordinates struct.
        /// </summary>
        /// <param name="latitude">Latitude in radians.</param>
        /// <param name="longitude">Longitude in radians.</param>
        /// <param name="altitude">Altitude in kilometers.</param>
        public GeodeticCoordinates(double latitude, double longitude, double altitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        /// <summary>
        /// Gets the latitude in degrees.
        /// </summary>
        public double LatitudeDegrees => OrbitalMath.RadiansToDegrees(Latitude);

        /// <summary>
        /// Gets the longitude in degrees.
        /// </summary>
        public double LongitudeDegrees => OrbitalMath.RadiansToDegrees(Longitude);

        /// <summary>
        /// Returns a string representation of the coordinates.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"{LatitudeDegrees:F6}°, {LongitudeDegrees:F6}°, {Altitude:F3} km";
        }
    }
}