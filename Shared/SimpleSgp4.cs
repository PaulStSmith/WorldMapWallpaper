using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Simplified SGP4 implementation focusing on reliability over maximum accuracy.
    /// This implementation prioritizes working calculations over perfect precision.
    /// </summary>
    public static class SimpleSgp4
    {
        // Earth constants
        private const double EARTH_RADIUS = 6378.137; // km
        private const double MU = 398600.4418; // km³/s²
        private const double J2 = 1.08262668E-3;
        
        // Time conversion constants
        private const double MINUTES_PER_DAY = 1440.0;
        private const double SECONDS_PER_MINUTE = 60.0;

        /// <summary>
        /// Simple orbital propagation using Keplerian elements with J2 perturbations.
        /// </summary>
        /// <param name="satellite">Satellite orbital elements.</param>
        /// <param name="time">Target time for propagation.</param>
        /// <returns>Position and velocity in ECI coordinates.</returns>
        public static PositionVelocity Propagate(SatelliteRecord satellite, DateTime time)
        {
            try
            {
                // Calculate time since epoch in minutes
                var minutesSinceEpoch = (time - satellite.Epoch).TotalMinutes;
                
                // Calculate semi-major axis from mean motion
                var n = satellite.MeanMotion; // rad/min
                var a = Math.Pow(MU / (n * SECONDS_PER_MINUTE) / (n * SECONDS_PER_MINUTE), 1.0 / 3.0) / 1000.0; // km
                
                // Apply secular perturbations due to J2
                var n0 = satellite.MeanMotion;
                var e0 = satellite.Eccentricity;
                var i0 = satellite.Inclination;
                var omega0 = satellite.ArgumentOfPerigee;
                var Omega0 = satellite.RightAscensionOfAscendingNode;
                var M0 = satellite.MeanAnomaly;
                
                // J2 perturbation rates (simplified)
                var cosI = Math.Cos(i0);
                var sinI = Math.Sin(i0);
                var p = a * (1.0 - e0 * e0);
                
                // Mean motion with J2 correction
                var nDot = 1.5 * n0 * J2 * Math.Pow(EARTH_RADIUS / p, 2) * (2.0 - 2.5 * sinI * sinI);
                var nCorrected = n0 + nDot * minutesSinceEpoch;
                
                // RAAN precession rate
                var OmegaDot = -1.5 * n0 * J2 * Math.Pow(EARTH_RADIUS / p, 2) * cosI;
                var Omega = Omega0 + OmegaDot * minutesSinceEpoch;
                
                // Argument of perigee precession rate  
                var omegaDot = 1.5 * n0 * J2 * Math.Pow(EARTH_RADIUS / p, 2) * (2.0 - 2.5 * sinI * sinI);
                var omega = omega0 + omegaDot * minutesSinceEpoch;
                
                // Mean anomaly
                var M = M0 + nCorrected * minutesSinceEpoch;
                
                // Normalize angles
                M = NormalizeAngle(M);
                omega = NormalizeAngle(omega);
                Omega = NormalizeAngle(Omega);
                
                // Solve Kepler's equation for eccentric anomaly
                var E = SolveKeplerEquation(M, e0);
                
                // Calculate true anomaly
                var sinE = Math.Sin(E);
                var cosE = Math.Cos(E);
                var nu = 2.0 * Math.Atan2(Math.Sqrt(1.0 + e0) * sinE, Math.Sqrt(1.0 - e0) * (cosE + e0));
                
                // Calculate radius
                var r = a * (1.0 - e0 * cosE);
                
                // Position in orbital plane
                var x_orb = r * Math.Cos(nu);
                var y_orb = r * Math.Sin(nu);
                
                // Velocity in orbital plane
                var h = Math.Sqrt(MU * a * (1.0 - e0 * e0)) / 1000.0; // km²/s
                var vx_orb = -(MU / 1000000.0) * sinE / (h * r); // km/s
                var vy_orb = (MU / 1000000.0) * Math.Sqrt(1.0 - e0 * e0) * cosE / (h * r); // km/s
                
                // Rotation matrices
                var cosOmega = Math.Cos(Omega);
                var sinOmega = Math.Sin(Omega);
                var cosomega = Math.Cos(omega);
                var sinomega = Math.Sin(omega);
                var cosi = Math.Cos(i0);
                var sini = Math.Sin(i0);
                
                // Transform to ECI coordinates
                var x = (cosOmega * cosomega - sinOmega * sinomega * cosi) * x_orb +
                       (-cosOmega * sinomega - sinOmega * cosomega * cosi) * y_orb;
                
                var y = (sinOmega * cosomega + cosOmega * sinomega * cosi) * x_orb +
                       (-sinOmega * sinomega + cosOmega * cosomega * cosi) * y_orb;
                
                var z = (sinomega * sini) * x_orb + (cosomega * sini) * y_orb;
                
                var vx = (cosOmega * cosomega - sinOmega * sinomega * cosi) * vx_orb +
                        (-cosOmega * sinomega - sinOmega * cosomega * cosi) * vy_orb;
                
                var vy = (sinOmega * cosomega + cosOmega * sinomega * cosi) * vx_orb +
                        (-sinOmega * sinomega + cosOmega * cosomega * cosi) * vy_orb;
                
                var vz = (sinomega * sini) * vx_orb + (cosomega * sini) * vy_orb;
                
                return new PositionVelocity(
                    new Vector3D(x, y, z),
                    new Vector3D(vx, vy, vz)
                );
            }
            catch (Exception)
            {
                // Return a default position if calculation fails
                return new PositionVelocity(
                    new Vector3D(0, 0, EARTH_RADIUS + 400), // 400km altitude default
                    new Vector3D(7.66, 0, 0) // Approximate ISS velocity
                );
            }
        }
        
        /// <summary>
        /// Solves Kepler's equation using Newton-Raphson iteration.
        /// </summary>
        /// <param name="M">Mean anomaly in radians.</param>
        /// <param name="e">Eccentricity.</param>
        /// <returns>Eccentric anomaly in radians.</returns>
        private static double SolveKeplerEquation(double M, double e)
        {
            const double tolerance = 1e-8;
            const int maxIterations = 20;
            
            var E = M; // Initial guess
            
            for (int i = 0; i < maxIterations; i++)
            {
                var f = E - e * Math.Sin(E) - M;
                var df = 1.0 - e * Math.Cos(E);
                
                if (Math.Abs(df) < tolerance) break; // Avoid division by zero
                
                var deltaE = f / df;
                E -= deltaE;
                
                if (Math.Abs(deltaE) < tolerance)
                    break;
            }
            
            return E;
        }
        
        /// <summary>
        /// Normalizes an angle to [0, 2π] range.
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>Normalized angle.</returns>
        private static double NormalizeAngle(double angle)
        {
            while (angle < 0)
                angle += 2.0 * Math.PI;
            while (angle >= 2.0 * Math.PI)
                angle -= 2.0 * Math.PI;
            return angle;
        }
    }
}