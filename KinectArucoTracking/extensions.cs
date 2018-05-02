using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Microsoft.Xna.Framework.Graphics;

namespace KinectArucoTracking
{
    static class extensions
    {

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static Texture2D XNATextureFromBitmap(this Bitmap bitmap, Texture2D texture)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height),
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

            return texture;
        }
    }
}
