using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Microsoft.Kinect;
using Microsoft.Xna.Framework.Graphics;

namespace KinectArucoTracking
{
    static class extensions
    {

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);


        public static Bitmap ToBitmap(this byte[] data, int width, int height
            , System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppRgb)
        {
            var bitmap = new Bitmap(width, height, format);

            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);
            Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public static Bitmap ToBitmap(this ColorFrame frame)
        {
            if (frame == null || frame.FrameDescription.LengthInPixels == 0)
                return null;

            var width = frame.FrameDescription.Width;
            var height = frame.FrameDescription.Height;

            var data = new byte[width * height * 32 / 8]; // PixelFormats.Bgra32.BitsPerPixel = 32
            frame.CopyConvertedFrameDataToArray(data, ColorImageFormat.Bgra);

            return data.ToBitmap(width, height);
        }

        public static Image<TColor, TDepth> ToOpenCVImage<TColor, TDepth>(this ColorFrame image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            var bitmap = image.ToBitmap();
            return new Image<TColor, TDepth>(bitmap);
        }

        public static Image<TColor, TDepth> ToOpenCVImage<TColor, TDepth>(this Bitmap bitmap)
            where TColor : struct, IColor
            where TDepth : new()
        {
            return new Image<TColor, TDepth>(bitmap);
        }

        public static Texture2D XNATextureFromBitmap(this Bitmap bitmap, Texture2D texture)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int bufferSize = data.Height * data.Stride;


            // copy bitmap data into texture

            byte[] rgbValues = new byte[bufferSize];

            Marshal.Copy(data.Scan0, rgbValues, 0, rgbValues.Length);

            for (int i = 0; i < bufferSize; i += 4)
            {
                byte dummy = rgbValues[i];
                rgbValues[i] = rgbValues[i + 2];
                rgbValues[i + 2] = dummy;
            }

            texture.SetData(rgbValues);
            bitmap.UnlockBits(data);
            bitmap.Dispose();
            rgbValues = null;

            GC.Collect();

            return texture;
        }
    }
}
