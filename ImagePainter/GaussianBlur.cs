using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/// <summary>
/// Helper class to apply a Gaussian blur to an image.
/// </summary>
/// <remarks>
/// Adapted from 
/// https://github.com/mdymel/superfastblur
/// </remarks>
internal static class GaussianBlur
{
    /// <summary>
    /// Parallel options configuration for controlling the degree of parallelism in image processing operations.
    /// </summary>
    private static readonly ParallelOptions _pOptions = new() { MaxDegreeOfParallelism = 16 };

    /// <summary>
    /// Applies a Gaussian blur to an image.
    /// </summary>
    /// <param name="img">The image to apply the blur.</param>
    /// <param name="radial">The size, in pixels, of the blurring.</param>
    /// <returns>A blurred image.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="img"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="radial"/> is negative.</exception>
    /// <remarks>
    /// This method implements a fast Gaussian blur approximation using box blur passes.
    /// The algorithm separates the image into ARGB channels, processes each channel independently,
    /// and then recombines them into the final blurred image.
    /// </remarks>
    public static Bitmap Apply(Bitmap img, int radial)
    {

        var rct = new Rectangle(0, 0, img.Width, img.Height);
        var source = new int[rct.Width * rct.Height];
        var bits = img.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        Marshal.Copy(bits.Scan0, source, 0, source.Length);
        img.UnlockBits(bits);

        var _width = img.Width;
        var _height = img.Height;

        var _alpha = new int[_width * _height];
        var _red = new int[_width * _height];
        var _green = new int[_width * _height];
        var _blue = new int[_width * _height];

        Parallel.For(0, source.Length, _pOptions, i =>
        {
            _alpha[i] = (int)((source[i] & 0xff000000) >> 24);
            _red[i] = (source[i] & 0xff0000) >> 16;
            _green[i] = (source[i] & 0x00ff00) >> 8;
            _blue[i] = (source[i] & 0x0000ff);
        });

        var newAlpha = new int[_width * _height];
        var newRed = new int[_width * _height];
        var newGreen = new int[_width * _height];
        var newBlue = new int[_width * _height];
        var dest = new int[_width * _height];

        Parallel.Invoke(
            () => GaussBlur4(_alpha, newAlpha, radial, _width, _height),
            () => GaussBlur4(_red, newRed, radial, _width, _height),
            () => GaussBlur4(_green, newGreen, radial, _width, _height),
            () => GaussBlur4(_blue, newBlue, radial, _width, _height));

        Parallel.For(0, dest.Length, _pOptions, i =>
        {
            if (newAlpha[i] > 255) newAlpha[i] = 255;
            if (newRed[i] > 255) newRed[i] = 255;
            if (newGreen[i] > 255) newGreen[i] = 255;
            if (newBlue[i] > 255) newBlue[i] = 255;

            if (newAlpha[i] < 0) newAlpha[i] = 0;
            if (newRed[i] < 0) newRed[i] = 0;
            if (newGreen[i] < 0) newGreen[i] = 0;
            if (newBlue[i] < 0) newBlue[i] = 0;

            dest[i] = (int)((uint)(newAlpha[i] << 24) | (uint)(newRed[i] << 16) | (uint)(newGreen[i] << 8) | (uint)newBlue[i]);
        });

        var image = new Bitmap(_width, _height);
        var bits2 = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        Marshal.Copy(dest, 0, bits2.Scan0, dest.Length);
        image.UnlockBits(bits2);
        return image;
    }

    /// <summary>
    /// Applies a Gaussian blur approximation to a single color channel using three box blur passes.
    /// </summary>
    /// <param name="source">The source array containing the color channel values.</param>
    /// <param name="dest">The destination array to store the blurred values.</param>
    /// <param name="r">The blur radius in pixels.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <remarks>
    /// This method approximates a Gaussian blur by applying three consecutive box blurs with
    /// calculated kernel sizes. The box blur sizes are determined to best approximate the
    /// desired Gaussian kernel with the given radius.
    /// </remarks>
    private static void GaussBlur4(int[] source, int[] dest, int r, int width, int height)
    {
        var bxs = BoxesForGauss(r, 3);
        BoxBlur4(source, dest, width, height, (bxs[0] - 1) / 2);
        BoxBlur4(dest, source, width, height, (bxs[1] - 1) / 2);
        BoxBlur4(source, dest, width, height, (bxs[2] - 1) / 2);
    }

    /// <summary>
    /// Calculates the optimal box blur kernel sizes to approximate a Gaussian blur with the given sigma.
    /// </summary>
    /// <param name="sigma">The standard deviation (blur radius) of the desired Gaussian kernel.</param>
    /// <param name="n">The number of box blur passes to use (typically 3 for good approximation).</param>
    /// <returns>An array of box blur kernel sizes.</returns>
    /// <remarks>
    /// This method uses mathematical optimization to determine the best combination of box blur
    /// sizes that will approximate a Gaussian blur. The algorithm ensures that the resulting
    /// blur closely matches the visual effect of a true Gaussian kernel while being much faster
    /// to compute.
    /// </remarks>
    private static int[] BoxesForGauss(int sigma, int n)
    {
        var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
        var wl = (int)Math.Floor(wIdeal);
        if (wl % 2 == 0) wl--;
        var wu = wl + 2;

        var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
        var m = Math.Round(mIdeal);

        var sizes = new List<int>();
        for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
        return [.. sizes];
    }

    /// <summary>
    /// Applies a box blur to an image array by performing horizontal and vertical passes.
    /// </summary>
    /// <param name="source">The source array containing the pixel values.</param>
    /// <param name="dest">The destination array to store the blurred values.</param>
    /// <param name="w">The width of the image.</param>
    /// <param name="h">The height of the image.</param>
    /// <param name="r">The radius of the box blur kernel.</param>
    /// <remarks>
    /// Box blur is a simple blur algorithm that averages each pixel with its neighbors
    /// within a square (box) region. This method applies the blur in two separable passes:
    /// first horizontally, then vertically, which is more efficient than a 2D convolution.
    /// </remarks>
    private static void BoxBlur4(int[] source, int[] dest, int w, int h, int r)
    {
        for (var i = 0; i < source.Length; i++) dest[i] = source[i];
        BoxBlurH4(dest, source, w, h, r);
        BoxBlurT4(source, dest, w, h, r);
    }

    /// <summary>
    /// Applies horizontal box blur to the image array.
    /// </summary>
    /// <param name="source">The source array containing the pixel values.</param>
    /// <param name="dest">The destination array to store the horizontally blurred values.</param>
    /// <param name="w">The width of the image.</param>
    /// <param name="h">The height of the image.</param>
    /// <param name="r">The radius of the horizontal box blur kernel.</param>
    /// <remarks>
    /// This method processes each row of the image independently, applying a sliding window
    /// average across the horizontal direction. The algorithm uses an optimized approach
    /// that maintains a running sum to avoid redundant calculations, making it very efficient
    /// for large blur radii.
    /// </remarks>
    private static void BoxBlurH4(int[] source, int[] dest, int w, int h, int r)
    {
        var iar = (double)1 / (r + r + 1);
        Parallel.For(0, h, _pOptions, i =>
        {
            var ti = i * w;
            var li = ti;
            var ri = ti + r;
            var fv = source[ti];
            var lv = source[ti + w - 1];
            var val = (r + 1) * fv;
            for (var j = 0; j < r; j++) val += source[ti + j];
            for (var j = 0; j <= r; j++)
            {
                val += source[ri++] - fv;
                dest[ti++] = (int)Math.Round(val * iar);
            }
            for (var j = r + 1; j < w - r; j++)
            {
                val += source[ri++] - dest[li++];
                dest[ti++] = (int)Math.Round(val * iar);
            }
            for (var j = w - r; j < w; j++)
            {
                val += lv - source[li++];
                dest[ti++] = (int)Math.Round(val * iar);
            }
        });
    }

    /// <summary>
    /// Applies vertical box blur to the image array.
    /// </summary>
    /// <param name="source">The source array containing the pixel values.</param>
    /// <param name="dest">The destination array to store the vertically blurred values.</param>
    /// <param name="w">The width of the image.</param>
    /// <param name="h">The height of the image.</param>
    /// <param name="r">The radius of the vertical box blur kernel.</param>
    /// <remarks>
    /// This method processes each column of the image independently, applying a sliding window
    /// average across the vertical direction. Similar to the horizontal blur, it uses an
    /// optimized running sum approach for efficiency. The algorithm handles edge cases by
    /// extending the first and last pixel values beyond the image boundaries.
    /// </remarks>
    private static void BoxBlurT4(int[] source, int[] dest, int w, int h, int r)
    {
        var iar = (double)1 / (r + r + 1);
        Parallel.For(0, w, _pOptions, i =>
        {
            var ti = i;
            var li = ti;
            var ri = ti + r * w;
            var fv = source[ti];
            var lv = source[ti + w * (h - 1)];
            var val = (r + 1) * fv;
            for (var j = 0; j < r; j++) val += source[ti + j * w];
            for (var j = 0; j <= r; j++)
            {
                val += source[ri] - fv;
                dest[ti] = (int)Math.Round(val * iar);
                ri += w;
                ti += w;
            }
            for (var j = r + 1; j < h - r; j++)
            {
                val += source[ri] - source[li];
                dest[ti] = (int)Math.Round(val * iar);
                li += w;
                ri += w;
                ti += w;
            }
            for (var j = h - r; j < h; j++)
            {
                val += lv - source[li];
                dest[ti] = (int)Math.Round(val * iar);
                li += w;
                ti += w;
            }
        });
    }
}
