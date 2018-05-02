using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Aruco;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Xna.Framework.Graphics;

namespace KinectArucoTracking
{
    public class FormVideoCapture
    {
        private VideoCapture _capture = null;

        int calibrated = 0;

        int calibrationFreq = 10;

        private Dictionary _dict;
        private DetectorParameters _detectorParameters;

        private Mat _frame = new Mat();
        Mat _frameCopy = new Mat();

        Mat _cameraMatrix = new Mat();
        Mat _distCoeffs = new Mat();
        Mat rvecs = new Mat();
        Mat tvecs = new Mat();

        private VectorOfInt _allIds = new VectorOfInt();
        private VectorOfVectorOfPointF _allCorners = new VectorOfVectorOfPointF();
        private VectorOfInt _markerCounterPerFrame = new VectorOfInt();
        private Size _imageSize = Size.Empty;

        int markersX = 4;
        int markersY = 4;
        int markersLength = 80;
        int markersSeparation = 30;

        private GridBoard _gridBoard;
        private GridBoard ArucoBoard
        {
            get
            {
                if (_gridBoard == null)
                {
                    _gridBoard = new GridBoard(markersX, markersY, markersLength, markersSeparation, ArucoDictionary);
                }
                return _gridBoard;
            }
        }

        private bool calibrate = false;

        private Texture2D background;
        private GraphicsDevice graphicsDevice;

        public void InitCapture()
        {
            _detectorParameters = DetectorParameters.GetDefault();

            try
            {
                _capture = new VideoCapture();
                if (!_capture.IsOpened)
                {
                    _capture = null;
                    throw new NullReferenceException("Unable to open video capture");
                }
                else
                {
                    _capture.ImageGrabbed += ImageArrived;
                }
            }
            catch (NullReferenceException excpt)
            {

            }
        }

        public FormVideoCapture(Texture2D background, GraphicsDevice graphicsDevice)
        {
            this.background = background;
            this.graphicsDevice = graphicsDevice;
            InitCapture();
        }

        private Dictionary ArucoDictionary
        {
            get
            {
                if (_dict == null)
                    _dict = new Dictionary(Dictionary.PredefinedDictionaryName.DictArucoOriginal);
                return _dict;
            }

        }

        void ImageArrived(object sender, EventArgs e)
        {
            
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                
                _capture.Retrieve(_frame);
                _frame.CopyTo(_frameCopy);
                //var image = _frameCopy.ToImage<Bgr, byte>();

              
                using (VectorOfInt ids = new VectorOfInt())
                using (VectorOfVectorOfPointF corners = new VectorOfVectorOfPointF())
                using (VectorOfVectorOfPointF rejected = new VectorOfVectorOfPointF())
                {
                    ArucoInvoke.DetectMarkers(_frameCopy, ArucoDictionary, corners, ids, _detectorParameters, rejected);

                    if (ids.Size > 0)
                    {
                        ArucoInvoke.RefineDetectedMarkers(_frameCopy, ArucoBoard, corners, ids, rejected, null, null,
                            10, 3, true, null, _detectorParameters);

                        ArucoInvoke.DrawDetectedMarkers(_frameCopy, corners, ids, new MCvScalar(0, 255, 0));

                        if (!_cameraMatrix.IsEmpty && !_distCoeffs.IsEmpty)
                        {
                            ArucoInvoke.EstimatePoseSingleMarkers(corners, markersLength, _cameraMatrix, _distCoeffs,
                                rvecs, tvecs);
                            for (int i = 0; i < ids.Size; i++)
                            {
                                using (Mat rvecmat = rvecs.Row(i))
                                using (Mat tvecmat = tvecs.Row(i))
                                using (VectorOfDouble rvec = new VectorOfDouble())
                                using (VectorOfDouble tvec = new VectorOfDouble())
                                {
                                    double[] values = new double[3];
                                    rvecmat.CopyTo(values);
                                    rvec.Push(values);
                                    tvecmat.CopyTo(values);
                                    tvec.Push(values);

                                    ArucoInvoke.DrawAxis(_frameCopy, _cameraMatrix, _distCoeffs, rvec, tvec,
                                        markersLength * 0.5f);
                                }
                            }
                        }

                        if (calibrate)
                        {
                            _allCorners.Push(corners);
                            _allIds.Push(ids);
                            _markerCounterPerFrame.Push(new int[] {corners.Size});
                            _imageSize = _frameCopy.Size;
                            calibrated += 1;

                            if (calibrated >= calibrationFreq)
                            {
                                calibrate = false;
                            }
                        }

                        int totalPoints = _markerCounterPerFrame.ToArray().Sum();
                        if (calibrated >= calibrationFreq && totalPoints > 0)
                        {

                            ArucoInvoke.CalibrateCameraAruco(_allCorners, _allIds, _markerCounterPerFrame, ArucoBoard,
                                _imageSize,
                                _cameraMatrix, _distCoeffs, null, null, CalibType.Default,
                                new MCvTermCriteria(30, double.Epsilon));

                            _allCorners.Clear();
                            _allIds.Clear();
                            _markerCounterPerFrame.Clear();
                            _imageSize = System.Drawing.Size.Empty;
                            calibrated = 0;
                            Console.WriteLine("Calibrated");
                        }
                    }

                    this.background = _frameCopy.Bitmap.XNATextureFromBitmap(background);
                }
            }
            
        }

        public Texture2D getBackground()
        {
            return background;
        }

        public void closeCapture()
        {
            //if (this._rgbReader != null)
            //{
            //    // ColorFrameReder is IDisposable
            //    this._rgbReader.Dispose();
            //    this._rgbReader = null;
            //}

            //if (this._sensor != null)
            //{
            //    this._sensor.Close();
            //    this._sensor = null;
            //}
        }

        private void calibrateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrate = true;
        }

        public VideoCapture getCapture()
        {
            return _capture;
        }

        public void startCapture()
        {
            _capture.Start();
        }

        public void SetTexture(Texture2D texture)
        {
            this.background = texture;
        }
    }
}

