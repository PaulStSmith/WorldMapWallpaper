using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Basic orbital propagator using Keplerian orbital mechanics.
    /// Designed for reliability and reasonable accuracy for ISS tracking.
    /// </summary>
    public static class BasicOrbitalPropagator
    {
        // Constants
        private const double EARTH_RADIUS = 6378.137; // km
        private const double MU = 398600.4418; // km³/s²
        private const double J2 = 1.08262668E-3;
        private const double MINUTES_PER_DAY = 1440.0;
        
        /// <summary>
        /// Propagates satellite position using basic Keplerian orbital mechanics.
        /// </summary>
        /// <param name="satellite">Satellite orbital elements.</param>
        /// <param name="time">Target time for propagation.</param>
        /// <returns>Position and velocity in ECI coordinates.</returns>
        public static PositionVelocity Propagate(SatelliteRecord satellite, DateTime time)
        {
            try
            {
                // Time since epoch in minutes
                var deltaMinutes = (time - satellite.Epoch).TotalMinutes;
                
                // Basic orbital elements
                var n = satellite.MeanMotion; // rad/min
                var e = satellite.Eccentricity;
                var i = satellite.Inclination;
                var omega = satellite.ArgumentOfPerigee;
                var Omega = satellite.RightAscensionOfAscendingNode;
                var M0 = satellite.MeanAnomaly;
                
                // Validate inputs
                if (double.IsNaN(n) || double.IsNaN(e) || double.IsNaN(i) ||
                    double.IsNaN(omega) || double.IsNaN(Omega) || double.IsNaN(M0) ||
                    n <= 0 || e < 0 || e >= 1)
                {
                    return GetDefaultPosition();
                }
                
                // Semi-major axis from mean motion
                var nRadPerSec = n / 60.0; // Convert to rad/s
                var a = Math.Pow(MU / (nRadPerSec * nRadPerSec), 1.0 / 3.0); // km
                
                // Simple secular perturbations (J2 effects)
                var cosI = Math.Cos(i);
                var p = a * (1.0 - e * e);
                
                // Secular rates due to J2
                var factor = 1.5 * J2 * Math.Pow(EARTH_RADIUS / p, 2);
                var omegaDot = factor * n * (2.0 - 2.5 * Math.Sin(i) * Math.Sin(i));
                var OmegaDot = -factor * n * cosI;
                var MDot = n; // Mean motion stays approximately constant
                
                // Apply secular perturbations
                var M = M0 + (MDot * deltaMinutes);
                var omegaCurrent = omega + (omegaDot * deltaMinutes);
                var OmegaCurrent = Omega + (OmegaDot * deltaMinutes);
                
                // Normalize angles to [0, 2π]
                M = NormalizeAngle(M);
                omegaCurrent = NormalizeAngle(omegaCurrent);
                OmegaCurrent = NormalizeAngle(OmegaCurrent);
                
                // Solve Kepler's equation for eccentric anomaly
                var E = SolveKeplersEquation(M, e);
                if (double.IsNaN(E))
                {
                    return GetDefaultPosition();
                }
                
                // True anomaly
                var nu = 2.0 * Math.Atan2(
                    Math.Sqrt(1.0 + e) * Math.Sin(E / 2.0),
                    Math.Sqrt(1.0 - e) * Math.Cos(E / 2.0)
                );
                
                // Distance from Earth center
                var r = a * (1.0 - e * Math.Cos(E));
                
                // Position in orbital plane
                var cosNu = Math.Cos(nu);
                var sinNu = Math.Sin(nu);
                var xOrb = r * cosNu;
                var yOrb = r * sinNu;
                var zOrb = 0.0;
                
                // Velocity in orbital plane
                var h = Math.Sqrt(MU * a * (1.0 - e * e)); // Angular momentum
                var vxOrb = -(MU / h) * Math.Sin(E);
                var vyOrb = (MU / h) * Math.Sqrt(1.0 - e * e) * Math.Cos(E);
                var vzOrb = 0.0;
                
                // Transform to ECI coordinates using rotation matrices
                var cosOmega = Math.Cos(OmegaCurrent);
                var sinOmega = Math.Sin(OmegaCurrent);
                var cosomega = Math.Cos(omegaCurrent);
                var sinomega = Math.Sin(omegaCurrent);
                var cosi = Math.Cos(i);
                var sini = Math.Sin(i);
                
                // ECI position
                var x = (cosOmega * cosomega - sinOmega * sinomega * cosi) * xOrb +
                       (-cosOmega * sinomega - sinOmega * cosomega * cosi) * yOrb;
                       
                var y = (sinOmega * cosomega + cosOmega * sinomega * cosi) * xOrb +
                       (-sinOmega * sinomega + cosOmega * cosomega * cosi) * yOrb;
                       
                var z = (sinomega * sini) * xOrb + (cosomega * sini) * yOrb;
                
                // ECI velocity (already in km/s)
                var vx = (cosOmega * cosomega - sinOmega * sinomega * cosi) * vxOrb +
                        (-cosOmega * sinomega - sinOmega * cosomega * cosi) * vyOrb;
                        
                var vy = (sinOmega * cosomega + cosOmega * sinomega * cosi) * vxOrb +
                        (-sinOmega * sinomega + cosOmega * cosomega * cosi) * vyOrb;
                        
                var vz = (sinomega * sini) * vxOrb + (cosomega * sini) * vyOrb;
                
                // Validate results
                if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z) ||
                    double.IsNaN(vx) || double.IsNaN(vy) || double.IsNaN(vz))
                {
                    return GetDefaultPosition();
                }
                
                return new PositionVelocity(
                    new Vector3D(x, y, z),
                    new Vector3D(vx, vy, vz)
                );
            }
            catch (Exception)
            {
                return GetDefaultPosition();
            }
        }
        
        /// <summary>
        /// Solves Kepler's equation using Newton-Raphson method.
        /// </summary>
        /// <param name="M">Mean anomaly in radians.</param>
        /// <param name="e">Eccentricity.</param>
        /// <returns>Eccentric anomaly in radians.</returns>
        private static double SolveKeplersEquation(double M, double e)
        {
            const double tolerance = 1e-8;
            const int maxIterations = 30;
            
            var E = M; // Initial guess
            
            for (int i = 0; i < maxIterations; i++)
            {
                var sinE = Math.Sin(E);
                var cosE = Math.Cos(E);
                var f = E - e * sinE - M;
                var df = 1.0 - e * cosE;
                
                if (Math.Abs(df) < tolerance) break;
                
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
            const double twoPi = 2.0 * Math.PI;
            while (angle < 0)
                angle += twoPi;
            while (angle >= twoPi)
                angle -= twoPi;
            return angle;
        }
        
        /// <summary>
        /// Returns a default ISS position when calculations fail.
        /// </summary>
        /// <returns>Default position at ~400km altitude.</returns>
        private static PositionVelocity GetDefaultPosition()
        {
            // Default ISS position: over equator at 400km altitude
            var altitude = 400.0; // km
            var radius = EARTH_RADIUS + altitude;
            
            return new PositionVelocity(
                new Vector3D(radius, 0, 0), // Position over equator
                new Vector3D(0, 7.66, 0)   // Approximate ISS orbital velocity
            );
        }
    }
}