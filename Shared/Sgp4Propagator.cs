using System;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// SGP4 (Simplified General Perturbations 4) orbital propagation algorithm.
    /// Based on the NORAD SGP4 model for satellite orbital calculations.
    /// </summary>
    public static class Sgp4Propagator
    {
        // SGP4 constants
        private static readonly double XKE = 60.0 / Math.Sqrt(OrbitalMath.EARTH_RADIUS_KM * OrbitalMath.EARTH_RADIUS_KM * OrbitalMath.EARTH_RADIUS_KM / OrbitalMath.EARTH_GRAVITATIONAL_PARAMETER);
        private const double QOMS2T = 1.880279159015270e-09; // (Q0 - S)^4 ER^4
        private const double S = 1.012229; // S parameter
        private const double AE = 1.0; // Earth radii
        private static readonly double A3OVK2 = -OrbitalMath.J2 * Math.Pow(AE, 3) / (OrbitalMath.J2 * OrbitalMath.J2);
        private static readonly double XJ2 = OrbitalMath.J2;
        private const double XJ3 = -2.53881e-6; // J3 coefficient
        private const double XJ4 = -1.65597e-6; // J4 coefficient

        /// <summary>
        /// Propagates satellite position and velocity using SGP4 algorithm.
        /// </summary>
        /// <param name="satellite">The satellite record containing orbital elements.</param>
        /// <param name="time">The time to propagate to.</param>
        /// <returns>Position and velocity vectors in ECI coordinates.</returns>
        public static PositionVelocity Propagate(SatelliteRecord satellite, DateTime time)
        {
            if (satellite == null)
                throw new ArgumentNullException(nameof(satellite));

            // Calculate minutes since epoch
            var minutesSinceEpoch = (time - satellite.Epoch).TotalMinutes;

            // Initialize SGP4 parameters
            var sgp4Data = InitializeSgp4(satellite);

            // Propagate using SGP4
            return Sgp4(sgp4Data, minutesSinceEpoch);
        }

        /// <summary>
        /// Initializes SGP4 parameters from satellite record.
        /// </summary>
        /// <param name="satellite">The satellite orbital elements.</param>
        /// <returns>Initialized SGP4 data structure.</returns>
        private static Sgp4Data InitializeSgp4(SatelliteRecord satellite)
        {
            var data = new Sgp4Data
            {
                Inclination = satellite.Inclination,
                Eccentricity = satellite.Eccentricity,
                ArgumentOfPerigee = satellite.ArgumentOfPerigee,
                RightAscension = satellite.RightAscensionOfAscendingNode,
                MeanAnomaly = satellite.MeanAnomaly,
                MeanMotion = satellite.MeanMotion,
                BStar = satellite.BStar,
                Epoch = satellite.Epoch
            };

            // Calculate derived parameters
            var cosio = Math.Cos(data.Inclination);
            var sinio = Math.Sin(data.Inclination);
            var ak = Math.Pow(XKE / data.MeanMotion, 2.0 / 3.0);
            var d1 = 0.75 * XJ2 * (3.0 * cosio * cosio - 1.0) / (ak * ak * Math.Pow(1.0 - data.Eccentricity * data.Eccentricity, 1.5));
            var del = d1 / (ak * ak);
            var adel = ak * (1.0 - del * del - del * (1.0 / 3.0 + 134.0 * del * del / 81.0));
            data.SemiMajorAxis = adel;
            
            // More initialization parameters
            var s4 = S;
            var pinvsq = 1.0 / (adel * adel * Math.Pow(1.0 - data.Eccentricity * data.Eccentricity, 2));
            var tsi = 1.0 / (adel - s4);
            data.Eta = adel * data.Eccentricity * tsi;
            var etasq = data.Eta * data.Eta;
            var eeta = data.Eccentricity * data.Eta;
            var psisq = Math.Abs(1.0 - etasq);
            var coef = QOMS2T * Math.Pow(tsi, 4);
            var coef1 = coef / Math.Pow(psisq, 3.5);
            
            var c2 = coef1 * data.MeanMotion * (adel * (1.0 + 1.5 * etasq + eeta * (4.0 + etasq)) +
                0.375 * XJ2 * tsi / psisq * cosio * (8.0 + 3.0 * etasq * (8.0 + etasq)));
            
            data.C1 = data.BStar * c2;
            data.C2 = c2;
            data.C3 = coef * tsi * A3OVK2 * data.MeanMotion * AE * sinio / data.Eccentricity;
            data.C4 = 2.0 * data.MeanMotion * coef1 * adel * Math.Pow(1.0 - etasq, 2) *
                ((2.0 * etasq * (1.0 + eeta) + 0.5 * data.Eccentricity + 0.5 * data.Eta * etasq) -
                 2.0 * XJ2 * tsi / (adel * psisq) *
                 (-3.0 * cosio * (1.0 - 2.0 * eeta + etasq * (1.5 - 0.5 * eeta)) +
                  0.75 * Math.Pow(1.0 - cosio * cosio, 2) * (2.0 * etasq - eeta * (1.0 + etasq)) * Math.Cos(2.0 * data.ArgumentOfPerigee)));

            data.C5 = 2.0 * coef1 * adel * Math.Pow(1.0 - etasq, 2) * (1.0 + 2.75 * (etasq + eeta) + eeta * etasq);

            var theta = cosio;
            data.X3THM1 = 3.0 * theta * theta - 1.0;
            data.X1MTH2 = 1.0 - theta * theta;
            data.X7THM1 = 7.0 * theta * theta - 1.0;

            return data;
        }

        /// <summary>
        /// Core SGP4 propagation calculation.
        /// </summary>
        /// <param name="data">SGP4 initialization data.</param>
        /// <param name="minutesSinceEpoch">Time offset in minutes from epoch.</param>
        /// <returns>Position and velocity in ECI coordinates.</returns>
        private static PositionVelocity Sgp4(Sgp4Data data, double minutesSinceEpoch)
        {
            // Secular perturbations
            var xmdf = data.MeanAnomaly + data.C1 * minutesSinceEpoch;
            var argpdf = data.ArgumentOfPerigee + data.C3 * minutesSinceEpoch;
            var nodedf = data.RightAscension + data.C4 * minutesSinceEpoch;
            var argpm = argpdf;
            var mm = xmdf;
            var t2cof = 1.5 * data.C1;
            var t3cof = data.C1 * data.C1;
            var t4cof = 0.25 * data.C1 * data.C1 * data.C1;

            // Update for secular gravity and atmospheric drag
            var delomg = data.BStar * data.C3 * Math.Cos(argpdf) * minutesSinceEpoch;
            var delm = -2.0 / 3.0 * data.BStar * data.C1 * minutesSinceEpoch * minutesSinceEpoch;
            var temp = delomg + delm;
            mm = data.MeanAnomaly + temp;
            argpm = data.ArgumentOfPerigee - temp;
            var t2 = minutesSinceEpoch * minutesSinceEpoch;
            var t3 = t2 * minutesSinceEpoch;
            var t4 = t3 * minutesSinceEpoch;
            var tempa = temp + data.C2 * t2 + data.C5 * t4;
            var tempe = data.BStar * data.C4 * minutesSinceEpoch;
            var templ = t2cof * t2 + t3cof * t3 + t4cof * t4;

            var a = data.SemiMajorAxis * Math.Pow(1.0 - data.C1 * minutesSinceEpoch - data.C2 * t2 - data.C5 * t4, 2);
            var e = data.Eccentricity - tempe;
            var xl = mm + argpm + nodedf + data.MeanMotion * templ;

            // Solve Kepler's equation
            var uKepler = xl - nodedf;
            var eo1 = uKepler;
            var tem5 = double.MaxValue;
            var sineo1 = 0.0;
            var coseo1 = 0.0;

            // Kepler iteration
            for (var ktr = 1; ktr <= 10 && Math.Abs(tem5) >= 1.0e-12; ktr++)
            {
                sineo1 = Math.Sin(eo1);
                coseo1 = Math.Cos(eo1);
                tem5 = 1.0 - coseo1 * a / data.SemiMajorAxis;
                tem5 = (uKepler - eo1 + e * sineo1) / tem5;
                if (Math.Abs(tem5) >= 0.95)
                    tem5 = tem5 > 0.0 ? 0.95 : -0.95;
                eo1 += tem5;
            }

            // Short period preliminary quantities
            var ecose = a / data.SemiMajorAxis * coseo1;
            var esine = a / data.SemiMajorAxis * sineo1;
            var el2 = a * a / (data.SemiMajorAxis * data.SemiMajorAxis);
            var pl = a * (1.0 - el2);
            var r = a * (1.0 - ecose);
            var rdotk = Math.Sqrt(a) * esine / r;
            var rfdotk = Math.Sqrt(pl) / r;
            var temp2 = a / r;
            var temp3 = 1.0 / (1.0 + Math.Sqrt(1.0 - el2));
            var betal = Math.Sqrt(1.0 - el2);
            var temp1 = esine / (1.0 + betal);
            var cos2u = temp2 * (coseo1 - temp1);
            var sin2u = temp2 * (sineo1 - temp1);
            var u = Math.Atan2(sin2u, cos2u);
            var sin2u2 = 2.0 * sin2u * cos2u;
            var cos2u2 = 2.0 * cos2u * cos2u - 1.0;

            // Update for short period periodics
            var cosio = Math.Cos(data.Inclination);
            var sinio = Math.Sin(data.Inclination);
            var rk = r * (1.0 - 1.5 * XJ2 * (3.0 * cosio * cosio - 1.0) / (pl * pl)) + 0.5 * XJ2 * data.X1MTH2 * cos2u2 / (pl * pl);
            var uk = u - 0.25 * XJ2 * data.X7THM1 * sin2u2 / (pl * pl);
            var xnodek = nodedf + 1.5 * XJ2 * cosio * sin2u2 / (pl * pl);
            var xinck = data.Inclination + 1.5 * XJ2 * cosio * sinio * cos2u2 / (pl * pl);
            var rdotk2 = rdotk - XJ2 * data.MeanMotion * data.X1MTH2 * sin2u2 / (pl * pl);
            var rfdotk2 = rfdotk + XJ2 * data.MeanMotion * (data.X1MTH2 * cos2u2 + 1.5 * data.X3THM1) / (pl * pl);

            // Orientation vectors
            var sinuk = Math.Sin(uk);
            var cosuk = Math.Cos(uk);
            var sinik = Math.Sin(xinck);
            var cosik = Math.Cos(xinck);
            var sinnok = Math.Sin(xnodek);
            var cosnok = Math.Cos(xnodek);
            var xmx = -sinnok * cosik;
            var xmy = cosnok * cosik;
            var ux = xmx * sinuk + cosnok * cosuk;
            var uy = xmy * sinuk + sinnok * cosuk;
            var uz = sinik * sinuk;
            var vx = xmx * cosuk - cosnok * sinuk;
            var vy = xmy * cosuk - sinnok * sinuk;
            var vz = sinik * cosuk;

            // Position and velocity
            var position = new Vector3D(rk * ux, rk * uy, rk * uz) * OrbitalMath.EARTH_RADIUS_KM;
            var velocity = new Vector3D(rdotk2 * ux + rfdotk2 * vx, rdotk2 * uy + rfdotk2 * vy, rdotk2 * uz + rfdotk2 * vz);
            velocity = velocity * (OrbitalMath.EARTH_RADIUS_KM * data.MeanMotion / 60.0);

            return new PositionVelocity(position, velocity);
        }

        /// <summary>
        /// Internal data structure for SGP4 calculations.
        /// </summary>
        private class Sgp4Data
        {
            public double Inclination { get; set; }
            public double Eccentricity { get; set; }
            public double ArgumentOfPerigee { get; set; }
            public double RightAscension { get; set; }
            public double MeanAnomaly { get; set; }
            public double MeanMotion { get; set; }
            public double BStar { get; set; }
            public DateTime Epoch { get; set; }
            public double SemiMajorAxis { get; set; }
            public double Eta { get; set; }
            public double C1 { get; set; }
            public double C2 { get; set; }
            public double C3 { get; set; }
            public double C4 { get; set; }
            public double C5 { get; set; }
            public double X3THM1 { get; set; }
            public double X1MTH2 { get; set; }
            public double X7THM1 { get; set; }
        }
    }
}