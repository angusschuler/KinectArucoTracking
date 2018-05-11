using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Emgu.CV;
using Microsoft.Xna.Framework.Graphics;

namespace KinectArucoTracking
{
    static class extensions
    {

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static bool LoadFile(this Mat data, string filename)
        {
            try
            {
                string path = Application.ExecutablePath;
                path = path.Replace(Path.GetFileName(path), filename);

                if (!File.Exists(path)) return false;

                using (FileStream fs = File.Open(path, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    data = (Mat)formatter.Deserialize(fs);

                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: LoadFile: " + e.Message);
                return false;
            }
        }

        public static bool SaveFile(this Mat data, string filename)
        {
            try
            {
                string path = Application.ExecutablePath;
                path = path.Replace(Path.GetFileName(path), filename);

                using (FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(fs, data);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: SaveFile: " + e.Message);
                return false;
            }

            return true;
        }


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
