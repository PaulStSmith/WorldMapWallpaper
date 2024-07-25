using WorldMapWallpaper.Properties;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace WorldMapWallpaper
{
    /// <summary>
    /// The main class of the WorldMapWallpaper application.
    /// </summary>
    internal class Program
    {
        // For setting a string parameter
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, String pvParam, SPIF fWinIni);

        // For reading a string parameter
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, StringBuilder pvParam, SPIF fWinIni);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            /*
             * Short names for the resources
             */
            var map = Resources.WorldPoliticalMap;
            var day = Resources.EarthDay;
            var night = Resources.EarthNight;
            var clock = Resources.ClockFace32;

            /*
             *  Get current desktop wallpaper name
             */
            var sbWPFN = new StringBuilder(256);
            SystemParametersInfo(SPI.SPI_GETDESKWALLPAPER, 256, sbWPFN, SPIF.None);
            var wpfn = sbWPFN.ToString();

            var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DesktopImage01" + ".jpg");
            if (Path.GetFileName(wpfn).StartsWith("DesktopImage",StringComparison.InvariantCultureIgnoreCase))
            {
                if (wpfn.EndsWith("01.jpg", StringComparison.InvariantCultureIgnoreCase))
                    fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DesktopImage02" + ".jpg");
                else
                    fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DesktopImage01" + ".jpg");
            }
            else
            {
                /*
                 * This was added because we delete the previous wallpaper.
                 * And before we were, inadevertedly, deleting any wallpaper
                 * file that was set prioir to our setting the wall paper
                 */
                wpfn = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DesktopImage02" + ".jpg");
            }

            /*
             * The current (or specified) UTC time in seconds.
             */
            var NOW = DateTime.UtcNow;
            var TimeSeconds = NOW.Hour * 3600 + NOW.Minute * 60 + NOW.Second;

            /*
             * Since the world's borders are at longitude +-180 degrees but we are
             * are comparing to UTC time (which takes place at longitude 0 degrees),
             * we have to shift the time by exactly 12 hours using NOON_SECS.
             */
            var NoonSeconds = 86400 / 2;

            /*
             * We calculate the horizontal offset on the basis of seconds. Therefore we
             * divide the maximum offset (360) by the amount of seconds in a day.
             */
            var AngleStep = (double)360 / 86400;

            /*
             * Now let's add everything together... the offset is now in the
             * range of [0, 360)
             */
            var TimeOffset = (TimeSeconds + NoonSeconds) * AngleStep;

            /*
             * And now the vertical offset... throughout the year, the sun's position
             * varies between +-23.44 degrees around the equatorial line (it's exactly
             * over the equator on the vernal and autumnal equinox, 23.44° north at the
             * summer solstice and 23.44° south at the winter solstice). Between those
             * dates, the sun moves on a sine wave.
             *
             * The first thing we do is calculating the sun's position by using the
             * vernal equinox as a reference point.
             */
            var DayOfYear = NOW.DayOfYear;
            /*
             * Calculate the Vernal Equinox.
             * found here:
             * https://astronomy.stackexchange.com/questions/43283/accuracy-of-calculating-the-vernal-equinox
             * 
             * Vernal Equinox for year 2000 A.D. was March 20, 7:36 GMT
             * Autumnal Equinox for year 2000 A.D. was September 22 at 13:11 GMT
             */
            var VE2000 = new DateTime(2000, 03, 20, 07, 36, 0);
            var VernalEquinox = VE2000.AddDays((double)(NOW.Year - 2000) * 365.2425).DayOfYear;
            // var AE2000 = new DateTime(2000, 09, 22, 13, 11, 0);
            // var AutumnalEquinox = AE2000.AddDays((double)(NOW.Year - 2000) * 365.2425).DayOfYear;

            /*
             * Prepares the clip area
             */
            var pts = new List<PointF>(new PointF[]
            {
                new Point(day.Width, 0)
               ,new Point(0,0)
            });

            /*
             * Calculates the points of the terminator curve.
             */
            var MaxDeclination = 23.44;
            var declination = Trig.Sin(360.0 * (DayOfYear - VernalEquinox) / 365) * MaxDeclination;
            var y0 = (float)day.Height / 2;
            var x0 = (float)day.Width / 2;
            var xs = (float)day.Width / 360;
            var ys = (float)day.Height / 180;
            for (var a = -180; a <= 180; a++)
            {
                var longitude = a + TimeOffset;
                var tanLat = Trig.Cos(longitude) / Trig.Tan(declination);
                var arctanLat = Trig.Atan(tanLat);
                var y = y0 + (float)(arctanLat * ys);
                var x = x0 + (a * xs);
                pts.Add(new PointF(x, y));
            }
            pts.Add(new Point(day.Width, 0));

            /*
             * Generates the alpha mask
             */
            var bmpAlphaMask = new Bitmap(day.Width, day.Height);
            using (var g = Graphics.FromImage(bmpAlphaMask))
            {
                g.FillRectangle((declination >= 0) ? Brushes.Black : Brushes.White, new RectangleF(0, 0, day.Width, day.Height));
                g.FillClosedCurve((declination >= 0) ? Brushes.White : Brushes.Black, pts.ToArray());
            }
            bmpAlphaMask = GaussianBlur.Apply(bmpAlphaMask, 16);
            // bmpAlphaMask.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "AlphaMask.jpg"), ImageFormat.Jpeg);

            /*
             * Draws the image
             */
            using (var g = Graphics.FromImage(night))
            {
                var bmpDayTime = (Bitmap)day.Clone();
                SetAlphaMask(bmpDayTime, bmpAlphaMask);
                g.DrawImage(bmpDayTime, 0, 0);

                /*
                 * Draw the political map
                 */
                g.DrawImage(map, 0, 0, bmpDayTime.Width, bmpDayTime.Height);

                /*
                 * Draws the clock faces
                 */
                var tz = (float)day.Width / 24;
                var dx = tz / 2;
                for (var a = -12; a <= 12; a++)
                {
                    /*
                     * Calculates the time zone line
                     */
                    var x = x0 + (float)(a * tz);

                    /*
                     * Draw the clock line
                     */
                    using (var p = new Pen(Color.FromArgb(6, 255, 255, 255), 1))
                        g.DrawLine(p, x - dx, 0, x - dx, bmpDayTime.Height);

                    /*
                     * I think that the internal resolution 
                     * of the images are different.
                     * Writing this way solved the problem.
                     */
                    g.DrawImage(clock, new RectangleF(x - clock.Width / 2, 5, clock.Width, clock.Height), new RectangleF(0, 0, clock.Width, clock.Height), GraphicsUnit.Pixel);

                    /*
                     * Draw the clock hands
                     */
                    var y = (float)(5 + (clock.Height / 2));
                    var handLength = (float)(clock.Width / 3);
                    var centerPoint = new PointF(x, y);
                    const int alphaHand = 64;

                    var hc = NOW.AddHours(a);
                    var ah = 30 * ((hc.Minute / 360) + hc.Hour < 12 ? hc.Hour : hc.Hour - 12) - 90;
                    var xh = (float)(x + handLength * Trig.Cos(ah));
                    var yh = (float)((5 + (clock.Height / 2)) + handLength * Trig.Sin(ah));

                    var am = 6 * hc.Minute - 90;
                    var xm = (float)(x + handLength * Trig.Cos(am));
                    var ym = (float)((5 + (clock.Height / 2)) + handLength * Trig.Sin(am));

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    using (var p = new Pen(Color.FromArgb(alphaHand, Color.Red), 3))
                        g.DrawLine(p, centerPoint, new PointF(xh, yh));

                    using (var p = new Pen(Color.FromArgb(alphaHand, Color.Red), 2))
                        g.DrawLine(p, centerPoint, new PointF(xm, ym));
                }
            }
            night.Save(fileName);

            /*
             * Waits for the file to  be saved.
             */
            var timeOut = false;
            while (!File.Exists(fileName))
            {
                var elapsed = DateTime.UtcNow - NOW;
                if (elapsed.TotalSeconds > 5)
                {
                    timeOut = true;
                    break;
                }
                Thread.Sleep(1);
            }

            /*
             * Sets the desktop image.6
             */
            if (!timeOut)
            {
                SystemParametersInfo(SPI.SPI_SETDESKWALLPAPER, 0, fileName, SPIF.UpdateIniFile | SPIF.SendChange);
                /*
                 * The following line was added 
                 * because the wallpaper was not 
                 * updated immediately.
                 * 
                 * This workaround was found here:
                 * https://stackoverflow.com/a/19732915/44375
                 */
                Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true).Close();

                /*
                 * Deletes the previous wallpaper file
                 */
                try
                {
                    File.Delete(wpfn);
                }
                catch { }
            }
        }

        /// <summary>
        /// Applies the specified alpha mask to the specified image.
        /// </summary>
        /// <param name="image">An image to apply the alpha mask.</param>
        /// <param name="alphaMask">An alpha mask.</param>
        /// <remarks>
        /// The alpha mask must be a grayscale image. <br/>
        /// <br/>
        /// Adapted from  <br/>
        /// https://danbystrom.se/2008/08/24/soft-edged-images-in-gdi/
        /// </remarks>
        /// <exception cref="ArgumentException"></exception>
        public static void SetAlphaMask(Bitmap image, Bitmap alphaMask)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (alphaMask == null) throw new ArgumentNullException(nameof(alphaMask));
            if (image.Size != alphaMask.Size)
                throw new ArgumentException("The image and alpha mask must be the same size.");

            var r = new Rectangle(Point.Empty, image.Size);
            var bdDst = image.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bdSrc = alphaMask.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            unsafe
            {
                var bpSrc = (byte*)bdSrc.Scan0.ToPointer();
                var bpDst = (byte*)bdDst.Scan0.ToPointer() + 3; // 3 is the alpha channel | 0 - Blue | 1 - Green | 2 - Red | 3 - Alpha |
                for (var i = r.Height * r.Width; i > 0; i--)
                {
                    *bpDst = *bpSrc;
                    bpSrc += 4;
                    bpDst += 4;
                }
            }
            image.UnlockBits(bdSrc);
            alphaMask.UnlockBits(bdDst);
        }
    }
}