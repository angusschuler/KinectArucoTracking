using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Xml;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = System.Drawing.Rectangle;

namespace KinectArucoTracking
{
    static class extensions
    {

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static Matrix CreateEulerFromMatrix(this Matrix matrix, double[] row0, double[] row1, double[] row2)
        {
            const double RD_TO_DEG = 180 / Math.PI;
            double roll, pitch, yaw; // angles in degrees

            //             extract pitch
            double sinP = -row1[2];// -matrix.M23;
            if (sinP >= 1)
            {
                pitch = Math.PI / 2;
            }       // pole
            else if (sinP <= -1)
            {
                pitch = -Math.PI/2;
            } // pole
            else
            {
                pitch = Math.Asin(sinP);
            }

            // extract heading and bank
            if (sinP < -0.998 || sinP > 0.998)
            { // account for small angle errors
                roll = Math.Atan2(row2[0], row0[0]); //-matrix.M31, matrix.M11) * RD_TO_DEG;
                yaw = 0;
            }
            else
            {
                roll = Math.Atan2(row0[2], row2[2]);  //matrix.M13, matrix.M33) * RD_TO_DEG;
                yaw = Math.Atan2(row1[0], row1[1]);  //matrix.M21, matrix.M22) * RD_TO_DEG;
            }

          
            return Matrix.CreateFromYawPitchRoll(-(float)roll, (float)pitch, -(float)yaw);
        }

        public static Mat LoadFile(this Mat data, string filename)
        {
            try
            {
                var path = Path.Combine(Environment.CurrentDirectory, filename);

                using (FileStream fs = File.Open(path, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    data = (Mat) formatter.Deserialize(fs);

                    return data;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: LoadFile: " + e.Message);
                return new Mat();
            }
        }

        public static void SaveFile(this Mat data, string filename)
        {
            if (data == null) return;
            try
            {  
                string path = Path.Combine(Environment.CurrentDirectory, filename);

                using (FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    
                    formatter.Serialize(fs, data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: SaveFile: " + e.Message);
            }
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
