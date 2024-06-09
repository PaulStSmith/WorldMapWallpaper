namespace WorldMapWallpaper
{
    /// <summary>
    /// Auxiliary helper class to calculate trigonometric functions in degrees.
    /// </summary>
    internal static class Trig
    {
        /// <summary>
        /// Constant to convert radians to degrees.
        /// </summary>
        public const double Rad2Deg = 180 / Math.PI;

        /// <summary>
        /// Constant to convert degrees to radians.
        /// </summary>
        public const double Deg2Rad = Math.PI / 180;

        /// <summary>
        /// Returns the sine of the specified angle.
        /// </summary>
        /// <param name="angle">An angle, measured in degrees.</param>
        /// <returns>The sine of <paramref name="angle"/>. 
        /// If <paramref name="angle"/> is equal to 
        /// <see cref="double.NaN"/>, 
        /// <see cref="double.NegativeInfinity"/>, or 
        /// <see cref="double.PositiveInfinity"/>, 
        /// this method return 
        /// <see cref="double.NaN"/>.</returns>
        public static double Sin(double angle)
        {
            if (double.IsNaN(angle) || double.IsInfinity(angle)) return double.NaN;
            return Math.Sin(angle * Deg2Rad);
        }

        /// <summary>
        /// Returns the cosine of the specified angle.
        /// </summary>
        /// <param name="angle">An angle, measured in degrees.</param>
        /// <returns>The cosine of <paramref name="angle"/>. 
        /// If <paramref name="angle"/> is equal to 
        /// <see cref="double.NaN"/>, 
        /// <see cref="double.NegativeInfinity"/>, or 
        /// <see cref="double.PositiveInfinity"/>, 
        /// this method return 
        /// <see cref="double.NaN"/>.</returns>
        public static double Cos(double angle)
        {
            if (double.IsNaN(angle) || double.IsInfinity(angle)) return double.NaN;
            return Math.Cos(angle * Deg2Rad);
        }

        /// <summary>
        /// Returns the tangent of the specified angle.
        /// </summary>
        /// <param name="angle">An angle, measured in degrees.</param>
        /// <returns>The tangent of <paramref name="angle"/>. 
        /// If <paramref name="angle"/> is equal to 
        /// <see cref="double.NaN"/>, 
        /// <see cref="double.NegativeInfinity"/>, or 
        /// <see cref="double.PositiveInfinity"/>, 
        /// this method return 
        /// <see cref="double.NaN"/>.</returns>
        public static double Tan(double angle)
        {
            if (double.IsNaN(angle) || double.IsInfinity(angle)) return double.NaN;
            return Math.Tan(angle * Deg2Rad);
        }

        /// <summary>
        /// Returns an angle whose sine is the specified number.
        /// </summary>
        /// <param name="value">A number representing a sine, where <paramref name="value"/> must be greater than or equal to -1, but less than or equal to 1.</param>
        /// <returns>An angle, Θ, measured in degrees, 
        /// such as -180 ≤ Θ ≤ 180, 
        /// -or-
        /// <see cref="double.NaN"/> 
        /// if <paramref name="value"/> &lt; -1 or &gt; 1 or 
        /// <paramref name="value"/> equals to <see cref="double.NaN"/>.</returns>
        public static double Asin(double value)
        {
            if (double.IsNaN(value) || value < -1 || value > 1) return double.NaN;
            return Math.Asin(value) * Rad2Deg;
        }

        /// <summary>
        /// Returns an angle whose cosine is the specified number.
        /// </summary>
        /// <param name="value">A number representing a cosine, where <paramref name="value"/> must be greater than or equal to -1, but less than or equal to 1.</param>
        /// <returns>An angle, Θ, measured in degrees, 
        /// such as -180 ≤ Θ ≤ 180, 
        /// -or-
        /// <see cref="double.NaN"/> 
        /// if <paramref name="value"/> &lt; -1 or &gt; 1 or 
        /// <paramref name="value"/> equals to <see cref="double.NaN"/>.</returns>
        public static double Acos(double value)
        {
            if (double.IsNaN(value) || value < -1 || value > 1) return double.NaN;
            return Math.Acos(value) * Rad2Deg;
        }

        /// <summary>
        /// Returns an angle whose tangent is the specified number.
        /// </summary>
        /// <param name="value">A number representing a tangent.</param>
        /// <returns>An angle, Θ, measured in degrees, 
        /// such as -180 ≤ Θ ≤ 180, 
        /// -or-
        /// <see cref="double.NaN"/> 
        /// if <paramref name="value"/> equals to <see cref="double.NaN"/>,
        /// -90 if <paramref name="value"/> equals to <see cref="double.NegativeInfinity"/>, or
        /// 90 if <paramref name="value"/> equals to <see cref="double.PositiveInfinity"/>.</returns>
        public static double Atan(double value)
        {
            if (double.IsNaN(value)) return double.NaN;
            if (double.IsNegativeInfinity(value)) return -90;
            if (double.IsPositiveInfinity(value)) return 90;
            return Math.Atan(value) * Rad2Deg;
        }
    }
}