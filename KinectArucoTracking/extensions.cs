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

        public static Matrix CreateEulerFromMatrix(this Matrix matrix, double[] row1, double[] row2, double[] row3)
        {
            const double RD_TO_DEG = 180 / Math.PI;
            double x, y, z; // angles in degrees

//             extract pitch
                        double sinP = -row2[2];// -matrix.M23;
                        if (sinP >= 1)
                        {
                            y = 90;
                        }       // pole
                        else if (sinP <= -1)
                        {
                            y = -90;
                        } // pole
                        else
                        {
                            y = Math.Asin(sinP);
                        }
            
                        // extract heading and bank
                        if (sinP < -0.9999 || sinP > 0.9999)
                        { // account for small angle errors
                            x = Math.Atan2(row3[0], row1[0]); //-matrix.M31, matrix.M11) * RD_TO_DEG;
                            z = 0;
                        }
                        else
                        {
                            x = Math.Atan2(row1[2], row3[2]);  //matrix.M13, matrix.M33) * RD_TO_DEG;
                            z = Math.Atan2(row2[0], row2[1]);  //matrix.M21, matrix.M22) * RD_TO_DEG;
                        }


//            if (row2[0] > 0.998)
//            { // singularity at north pole
//                x = 0;
//                y = Math.PI / 2;
//                z = Math.Atan2(row1[2], row3[2]);
//            }
//            else if (row2[0] < -0.998)
//            { // singularity at south pole
//                x = 0;
//                y = -Math.PI / 2;
//                z = Math.Atan2(row1[2], row3[2]);
//            }
//            else
//            {
//                x = Math.Atan2(-row2[2], row2[1]);
//                y = Math.Asin(row2[0]);
//                z = Math.Atan2(-row3[0], row1[0]);
//            }


//            Console.WriteLine(x + " : " + y + " : " + z);

//            return Matrix.CreateFromYawPitchRoll((float)z, (float)x, (float)y);
            return Matrix.CreateFromYawPitchRoll((float)-x, (float)-y, (float)z);
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
//                return new Mat(Path.Combine(Environment.CurrentDirectory, filename), ImreadModes.AnyColor);
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

                //                data.Save(filename);
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

//        public static float[] 
    }
}
