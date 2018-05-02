using Emgu.CV;
using Emgu.CV.Aruco;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Kinect;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms; 

public class FormVideoCapture
{
    KinectSensor _sensor;
    ColorFrameReader _rgbReader;

    int calibrated = 0;

    int calibrationFreq = 10;

    private Dictionary _dict;
    private DetectorParameters _detectorParameters;

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

    Texture2D background;
    GraphicsDevice graphicsDevice;

    int frameCount = 0;

    Texture2D bit;

    public void InitKinect()
    {
        _sensor = KinectSensor.GetDefault();
        //_sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
        _rgbReader = _sensor.ColorFrameSource.OpenReader();
        //_rgbReader = _sensor.ColorFrameSource.OpenReader();
        _rgbReader.FrameArrived += rgbReader_FrameArrived;
        _sensor.Open();
        _detectorParameters = DetectorParameters.GetDefault();
    }

    public FormVideoCapture(Texture2D background, GraphicsDevice graphicsDevice)
    {
        //InitializeComponent();
        this.background = background;
        this.graphicsDevice = graphicsDevice;
        InitKinect();
    }

    //private void FormVideoCapture_Load(object sender, EventArgs e)
    //{
    //    InitKinect();

    //}

    private Dictionary ArucoDictionary
    {
        get
        {
            if (_dict == null)
                _dict = new Dictionary(Dictionary.PredefinedDictionaryName.DictArucoOriginal);
            return _dict;
        }

    }

    void rgbReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
    {
        using (var frame = e.FrameReference.AcquireFrame())
        {
            if (frame != null)
            {

                var width = frame.FrameDescription.Width;
                var height = frame.FrameDescription.Height;
                var bitmap = frame.ToBitmap();
                var image = bitmap.ToOpenCVImage<Bgr, byte>().Mat;

                //do something here with the IImage       

                int frameSkip = 1;
                //every 10 frames

                if (++frameCount == frameSkip)
                {
                    frameCount = 0;
                    using (VectorOfInt ids = new VectorOfInt())
                    using (VectorOfVectorOfPointF corners = new VectorOfVectorOfPointF())
                    using (VectorOfVectorOfPointF rejected = new VectorOfVectorOfPointF())
                    {
                        ArucoInvoke.DetectMarkers(image, ArucoDictionary, corners, ids, _detectorParameters, rejected);

                        if (ids.Size > 0)
                        {
                            ArucoInvoke.RefineDetectedMarkers(image, ArucoBoard, corners, ids, rejected, null, null, 10, 3, true, null, _detectorParameters);

                            ArucoInvoke.DrawDetectedMarkers(image, corners, ids, new MCvScalar(0, 255, 0));

                            if (!_cameraMatrix.IsEmpty && !_distCoeffs.IsEmpty)
                            {
                                ArucoInvoke.EstimatePoseSingleMarkers(corners, markersLength, _cameraMatrix, _distCoeffs, rvecs, tvecs);
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

                                        ArucoInvoke.DrawAxis(image, _cameraMatrix, _distCoeffs, rvec, tvec,
                                            markersLength * 0.5f);
                                    }
                                }
                            }

                            if (calibrate)
                            {
                                _allCorners.Push(corners);
                                _allIds.Push(ids);
                                _markerCounterPerFrame.Push(new int[] { corners.Size });
                                _imageSize = image.Size;
                                calibrated += 1;

                                if (calibrated >= calibrationFreq)
                                {
                                    calibrate = false;
                                }
                            }

                            int totalPoints = _markerCounterPerFrame.ToArray().Sum();
                            if (calibrated >= calibrationFreq && totalPoints > 0)
                            {

                                ArucoInvoke.CalibrateCameraAruco(_allCorners, _allIds, _markerCounterPerFrame, ArucoBoard, _imageSize,
                                    _cameraMatrix, _distCoeffs, null, null, CalibType.Default, new MCvTermCriteria(30, double.Epsilon));

                                _allCorners.Clear();
                                _allIds.Clear();
                                _markerCounterPerFrame.Clear();
                                _imageSize = System.Drawing.Size.Empty;
                                calibrated = 0;
                                Console.WriteLine("Calibrated");
                            }
                        }
                    }
                }
                //end doing something

                this.background = image.Bitmap.XNATextureFromBitmap(background);
                bitmap.Dispose();
                image.Dispose();
            }
        }
    }

    public Texture2D getBackground()
    {
        return background;
    }

    public void closeCapture()
    {
        if (this._rgbReader != null)
        {
            // ColorFrameReder is IDisposable
            this._rgbReader.Dispose();
            this._rgbReader = null;
        }

        if (this._sensor != null)
        {
            this._sensor.Close();
            this._sensor = null;
        }
    }

    private void calibrateToolStripMenuItem_Click(object sender, EventArgs e)
    {
        calibrate = true;
    }
}

