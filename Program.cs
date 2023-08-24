using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using DesktopImageChanger.Properties;
using Microsoft.Win32;
using Windows.Devices.Spi;

namespace DesktopImageChanger
{
    internal class Program
    {
        // For setting a string parameter
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, String pvParam, SPIF fWinIni);

        // For reading a string parameter
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, StringBuilder pvParam, SPIF fWinIni);

        static void Main(string[] args)
        {
            /*
             * Get the maps
             */
            var day = Bitmap.FromStream(new MemoryStream(Resources.EarthDay));
            var night = Bitmap.FromStream(new MemoryStream(Resources.EarthNight));

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
                wpfn = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DesktopImage02" + ".jpg");
            }

            /*
             * Prepares the clip area
             */
            var pts = new List<PointF>(new PointF[] 
            {
                new Point(night.Width, 0)
               ,new Point(0,0)
            });

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
            var VernalEquinox = new DateTime(NOW.Year, 2, 20).DayOfYear;

            /*
             * Calculates the points or the terminator curve.
             */
            var MaxDeclination = 23.44;
            var declination = Trig.Sin(360 * (DayOfYear - VernalEquinox) / 365) * MaxDeclination;
            var y0 = (float)day.Height / 2;
            var x0 = (float)day.Width / 2;
            var xs = (float)day.Width / 360;
            var ys = (float)day.Height / 180;
            for (var a = -180; a <= 180; a++)
            {
                var longitude = a + TimeOffset;
                var tanLat = -Trig.Cos(longitude) / Trig.Tan(declination);
                var arctanLat = Trig.Atan(tanLat);
                var y = y0 + (float)(arctanLat * ys);
                var x = x0 + (a * xs);
                pts.Add(new PointF(x, y));
            }
            pts.Add(new Point(night.Width, 0));

            /*
             * Generates the alpha mask
             */
            var bmpAlphaMask = new Bitmap(day.Width, day.Height);
            using (var g = Graphics.FromImage(bmpAlphaMask))
                g.FillClosedCurve(Brushes.White, pts.ToArray());
            bmpAlphaMask = GaussianBlur.Apply(bmpAlphaMask, 16);

            /*
             * Draws the image
             */
            using (var g = Graphics.FromImage(night))
            {
                var bmpDayTime = (Bitmap)day.Clone();
                SetAlphaMask(bmpDayTime, bmpAlphaMask);
                g.DrawImage(bmpDayTime, 0, 0);
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
        /// The alpha mask must be a grayscale image.
        /// 
        /// Adapted from 
        /// https://danbystrom.se/2008/08/24/soft-edged-images-in-gdi/
        /// </remarks>
        /// <exception cref="ArgumentException"></exception>
        public static void SetAlphaMask(Bitmap image, Bitmap alphaMask)
        {
            if (image == null) throw new ArgumentNullException("image");
            if (image.Size != alphaMask.Size)
                throw new ArgumentException("The image and alpha mask must be the same size.");

            Rectangle r = new Rectangle(Point.Empty, image.Size);
            BitmapData bdDst = image.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bdSrc = alphaMask.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* bpSrc = (byte*)bdSrc.Scan0.ToPointer();
                byte* bpDst = (byte*)bdDst.Scan0.ToPointer() + 3; // 3 is the alpha channel | 0 - Blue | 1 - Green | 2 - Red | 3 - Alpha |
                for (int i = r.Height * r.Width; i > 0; i--)
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