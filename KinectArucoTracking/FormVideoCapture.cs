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
        private string fileExt = ".xml";

        private VideoCapture _capture = null;

        int calibrated = 0;

        int calibrationFreq = 40;

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
        private float markersLength = 1780f; //250f;
        private float markersSeparation = 100f;

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
        private bool calibrationFilesLoaded = false;

        public void InitCapture()
        {
            _detectorParameters = DetectorParameters.GetDefault();
            _detectorParameters.CornerRefinementMethod = DetectorParameters.RefinementMethod.Subpix;

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

            try
            {
                _cameraMatrix = _cameraMatrix.LoadFile("cameraMatrix" + fileExt);
                _distCoeffs = _distCoeffs.LoadFile("distCoeffs" + fileExt);
                if (!_cameraMatrix.IsEmpty && !_distCoeffs.IsEmpty)
                {
                    calibrationFilesLoaded = true;
                }
                else
                {
                    Console.WriteLine("Failed Loading Calibration Files");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("No Calibration Files");
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
//                Console.WriteLine(_capture.Width);
//                Console.WriteLine(_capture.Height);

                _capture.Retrieve(_frame);
                _frame.CopyTo(_frameCopy);
                //var image = _frameCopy.ToImage<Bgr, byte>();
                if (!_frame.IsEmpty)
                {
                

                using (VectorOfInt ids = new VectorOfInt())
                using (VectorOfVectorOfPointF corners = new VectorOfVectorOfPointF())
                    using (VectorOfVectorOfPointF rejected = new VectorOfVectorOfPointF())
                    {
                        ArucoInvoke.DetectMarkers(_frameCopy, ArucoDictionary, corners, ids, _detectorParameters,
                            rejected);

                        if (ids.Size > 0)
                        {
                            int[] test = ids.ToArray();
                            for (int i = 0; i < test.Length; i++)
                            {
                                Console.WriteLine(test[i]);

                            }
                            if (!_cameraMatrix.IsEmpty && !_distCoeffs.IsEmpty)
                            {
                                ArucoInvoke.RefineDetectedMarkers(_frameCopy, ArucoBoard, corners, ids, rejected,
                                    _cameraMatrix,
                                    _distCoeffs,
                                    9, 4, true, null, _detectorParameters);
                            }

                            ArucoInvoke.DrawDetectedMarkers(_frameCopy, corners, ids, new MCvScalar(0, 255, 0));

                            if (!_cameraMatrix.IsEmpty && !_distCoeffs.IsEmpty)
                            {
                                ArucoInvoke.EstimatePoseSingleMarkers(corners, markersLength, _cameraMatrix,
                                    _distCoeffs,
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

//                                        if (ids[i] == 5)
                                            ArucoInvoke.DrawAxis(_frameCopy, _cameraMatrix, _distCoeffs, rvec, tvec,
                                                markersLength * 0.5f);
                                    }
                                }
                            }

                            if (calibrate && (calibrated <= calibrationFreq) && !calibrationFilesLoaded)
                            {
                                _allCorners.Push(corners);
                                _allIds.Push(ids);
                                _markerCounterPerFrame.Push(new int[] {corners.Size});
                                _imageSize = _frameCopy.Size;
                                calibrated += 1;
                            

                                int totalPoints = _markerCounterPerFrame.ToArray().Sum();
                                if ((calibrated == calibrationFreq && totalPoints > 0))
                                {

                                    ArucoInvoke.CalibrateCameraAruco(_allCorners, _allIds, _markerCounterPerFrame,
                                        ArucoBoard,
                                        _imageSize,
                                        _cameraMatrix, _distCoeffs, null, null, CalibType.Default,
                                        new MCvTermCriteria(30, double.Epsilon));

                                    _allCorners.Clear();
                                    _allIds.Clear();
                                    _markerCounterPerFrame.Clear();
                                    _imageSize = System.Drawing.Size.Empty;
                                    calibrate = false;
                                    Console.WriteLine("Calibrated");
                                    _cameraMatrix.SaveFile("cameraMatrix" + fileExt);
                                    _distCoeffs.SaveFile("distCoeffs" + fileExt);
                                }
                            }
                        }

                        this.background = _frameCopy.Bitmap.XNATextureFromBitmap(background);
                    }
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

        public void calibrateCamera()
        {
            calibrationFilesLoaded = false;
            calibrate = true;
        }

        public Mat getRvecs()
        {
            return rvecs;
        }
        public Mat getTvecs()
        {
            return tvecs;
        }
    }
}

