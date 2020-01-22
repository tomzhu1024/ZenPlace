using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectHandTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //Serial fields
        private SerialPort serialPort;

        //Kinect fields
        KinectSensor kinectSensor;
        CoordinateMapper coordinateMapper;
        int displayWidth;
        int displayHeight;
        BodyFrameReader bodyFrameReader;
        Body[] bodies;
        ColorFrameReader colorFrameReader;
        WriteableBitmap colorBitmap;

        //Definition of bones
        List<Tuple<JointType, JointType>> bones = new List<Tuple<JointType, JointType>>();

        //Drawing fields for body image
        DrawingGroup bodyDrawingGroup;
        DrawingImage bodyDrawingImage;

        //Drawing properties for body image
        const double HandSize = 30;
        const double JointThickness = 3;
        const float InferredZPositionClamp = 0.1f;
        readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));
        readonly Brush handNotTrackedBrush = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
        readonly Brush handUnknownBrush = new SolidColorBrush(Color.FromArgb(128, 40, 40, 255));
        readonly Pen[] bodyPens = new Pen[] { new Pen(Brushes.Red, 6),new Pen(Brushes.Orange, 6),
            new Pen(Brushes.Green, 6),new Pen(Brushes.Cyan, 6),
            new Pen(Brushes.LightSkyBlue, 6),new Pen(Brushes.Violet, 6) };
        readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        readonly Brush inferredJointBrush = new SolidColorBrush(Color.FromArgb(255, 212, 113, 165));
        readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        //LED indicator color
        readonly Brush ledIdleBrush = Brushes.DarkGray;
        readonly Brush ledSerialBrush = Brushes.Green;
        readonly Brush ledKinectBrush = Brushes.DarkViolet;

        //Distance statistic fields
        float minZ = 0f;
        float maxZ = 0f;
        float sumZ = 0f;
        int countZ = 0;

        //DataGrid binding source
        ObservableCollection<BodyInfo> bodiesInfo = new ObservableCollection<BodyInfo>();

        //MeanFilter for hands position data
        MeanFilter meanFilterLeftX;
        MeanFilter meanFilterLeftY;
        MeanFilter meanFilterRightX;
        MeanFilter meanFilterRightY;

        //Buffer size for mean filters
        readonly int meanFilterBufferSize = 10;

        class BodyInfo : INotifyPropertyChanged
        {
            private string _ID;
            public string ID
            {
                get
                {
                    return _ID;
                }
                set
                {
                    _ID = value;
                    OnPropertyChanged("ID");
                }
            }

            private string _IsTracked;
            public string IsTracked
            {
                get
                {
                    return _IsTracked;
                }
                set
                {
                    _IsTracked = value;
                    OnPropertyChanged("IsTracked");
                }
            }

            private string _MinZ;
            public string MinZ
            {
                get
                {
                    return _MinZ;
                }
                set
                {
                    _MinZ = value;
                    OnPropertyChanged("MinZ");
                }
            }

            private string _AvgZ;
            public string AvgZ
            {
                get
                {
                    return _AvgZ;
                }
                set
                {
                    _AvgZ = value;
                    OnPropertyChanged("AvgZ");
                }
            }

            private string _MaxZ;
            public string MaxZ
            {
                get
                {
                    return _MaxZ;
                }
                set
                {
                    _MaxZ = value;
                    OnPropertyChanged("MaxZ");
                }
            }

            private string _HandLeftState;
            public string HandLeftState
            {
                get
                {
                    return _HandLeftState;
                }
                set
                {
                    _HandLeftState = value;
                    OnPropertyChanged("HandLeftState");
                }
            }

            private string _HandRightState;
            public string HandRightState
            {
                get
                {
                    return _HandRightState;
                }
                set
                {
                    _HandRightState = value;
                    OnPropertyChanged("HandRightState");
                }
            }

            private string _HandLeftX;
            public string HandLeftX
            {
                get
                {
                    return _HandLeftX;
                }
                set
                {
                    _HandLeftX = value;
                    OnPropertyChanged("HandLeftX");
                }
            }

            private string _HandLeftY;
            public string HandLeftY
            {
                get
                {
                    return _HandLeftY;
                }
                set
                {
                    _HandLeftY = value;
                    OnPropertyChanged("HandLeftY");
                }
            }

            private string _HandRightX;
            public string HandRightX
            {
                get
                {
                    return _HandRightX;
                }
                set
                {
                    _HandRightX = value;
                    OnPropertyChanged("HandRightX");
                }
            }

            private string _HandRightY;
            public string HandRightY
            {
                get
                {
                    return _HandRightY;
                }
                set
                {
                    _HandRightY = value;
                    OnPropertyChanged("HandRightY");
                }
            }

            private string _IsNearest;
            public string IsNearest
            {
                get
                {
                    return _IsNearest;
                }
                set
                {
                    _IsNearest = value;
                    OnPropertyChanged("IsNearest");
                }
            }

            public int HandLeftStateCode;
            public int HandRightStateCode;

            public BodyInfo()
            {
                ID = "-";
                IsTracked = "-";
                MinZ = "-";
                AvgZ = "-";
                MaxZ = "-";
                HandLeftState = "-";
                HandRightState = "-";
                HandLeftX = "-";
                HandLeftY = "-";
                HandRightX = "-";
                HandRightY = "-";
                IsNearest = "-";
                HandLeftStateCode = 1;
                HandRightStateCode = 1;
            }

            public BodyInfo(int ID, bool IsTracked, float minZ, float avgZ, float maxZ, HandState handLeftState, HandState handRightState, Point handLeftPosition, Point handRightPosition)
            {
                this.ID = Convert.ToString(ID);
                this.IsTracked = Convert.ToString(IsTracked);
                MinZ = Convert.ToString(minZ);
                AvgZ = Convert.ToString(avgZ);
                MaxZ = Convert.ToString(maxZ);
                HandLeftState = Convert.ToString(handLeftState);
                HandRightState = Convert.ToString(handRightState);
                HandLeftX = Convert.ToString(handLeftPosition.X);
                HandLeftY = Convert.ToString(handLeftPosition.Y);
                HandRightX = Convert.ToString(handRightPosition.X);
                HandRightY = Convert.ToString(handRightPosition.Y);
                IsNearest = "-";
                HandLeftStateCode = (int)handLeftState;
                HandRightStateCode = (int)handRightState;
            }

            public void Update(int ID)
            {
                this.ID = Convert.ToString(ID);
                IsTracked = "False";
                MinZ = "-";
                AvgZ = "-";
                MaxZ = "-";
                HandLeftState = "-";
                HandRightState = "-";
                HandLeftX = "-";
                HandLeftY = "-";
                HandRightX = "-";
                HandRightY = "-";
                IsNearest = "-";
                HandLeftStateCode = 1;
                HandRightStateCode = 1;
            }

            public void Update(int ID, bool IsTracked, float minZ, float avgZ, float maxZ, HandState handLeftState, HandState handRightState, Point handLeftPosition, Point handRightPosition)
            {
                this.ID = Convert.ToString(ID);
                this.IsTracked = Convert.ToString(IsTracked);
                MinZ = Convert.ToString(minZ);
                AvgZ = Convert.ToString(avgZ);
                MaxZ = Convert.ToString(maxZ);
                HandLeftState = Convert.ToString(handLeftState);
                HandRightState = Convert.ToString(handRightState);
                HandLeftX = Convert.ToString(handLeftPosition.X);
                HandLeftY = Convert.ToString(handLeftPosition.Y);
                HandRightX = Convert.ToString(handRightPosition.X);
                HandRightY = Convert.ToString(handRightPosition.Y);
                IsNearest = "-";
                HandLeftStateCode = (int)handLeftState;
                HandRightStateCode = (int)handRightState;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public MainWindow()
        {
            #region Define bones
            // Torso
            bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
            #endregion

            //Initialize mean filters
            meanFilterLeftX = new MeanFilter(meanFilterBufferSize);
            meanFilterLeftY = new MeanFilter(meanFilterBufferSize);
            meanFilterRightX = new MeanFilter(meanFilterBufferSize);
            meanFilterRightY = new MeanFilter(meanFilterBufferSize);

            //Initialize kinect
            kinectSensor = KinectSensor.GetDefault();
            //Body tracker reader
            coordinateMapper = kinectSensor.CoordinateMapper;
            FrameDescription depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            displayWidth = depthFrameDescription.Width;
            displayHeight = depthFrameDescription.Height;
            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;

            bodyDrawingGroup = new DrawingGroup();
            bodyDrawingImage = new DrawingImage(bodyDrawingGroup);

            //Color camera reader
            colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            FrameDescription colorFrameDescription = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            //Register events handler for availability change
            kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;
            kinectSensor.Open();

            //Initialize the components of the window
            InitializeComponent();

            //Specify data context
            DataContext = this;
            dataGrid.DataContext = bodiesInfo;

            //Update baudrate combobox
            CmbBoxBaudRate.Items.Add(9600);
            CmbBoxBaudRate.Items.Add(19200);
            CmbBoxBaudRate.Items.Add(38400);
            CmbBoxBaudRate.Items.Add(57600);
            CmbBoxBaudRate.Items.Add(115200);
            CmbBoxBaudRate.SelectedIndex = 0;

            //Update ports combobox
            UpdatePortsList();
        }

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            //Just update colorBitmap for display, no data processing required
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        colorBitmap.Lock();
                        if ((colorFrameDescription.Width == colorBitmap.PixelWidth) && (colorFrameDescription.Height == colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);
                            colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                        }
                        colorBitmap.Unlock();
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource BodyImage
        {
            get
            {
                return bodyDrawingImage;
            }
        }

        public ImageSource ColorImage
        {
            get
            {
                return colorBitmap;
            }
        }

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyCount];
                        for (int i = 0; i < bodyFrame.BodyCount; i++)
                        {
                            bodiesInfo.Add(new BodyInfo());
                        }
                    }
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    dataReceived = true;
                }
                //Draw bodies
                if (dataReceived)
                {
                    using (DrawingContext dc = bodyDrawingGroup.Open())
                    {
                        //Draw black background
                        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, displayWidth, displayHeight));
                        int bodyIndex = 0;
                        foreach (Body body in bodies)
                        {
                            minZ = float.MaxValue;
                            maxZ = float.MinValue;
                            sumZ = 0f;
                            countZ = 0;
                            if (body.IsTracked)
                            {
                                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                                foreach (JointType jointType in joints.Keys)
                                {
                                    CameraSpacePoint position = joints[jointType].Position;
                                    if (position.Z < 0)
                                    {
                                        position.Z = InferredZPositionClamp;
                                    }
                                    if (position.Z < minZ)
                                    {
                                        minZ = position.Z;
                                    }
                                    if (position.Z > maxZ)
                                    {
                                        maxZ = position.Z;
                                    }
                                    sumZ += position.Z;
                                    countZ++;
                                    DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
                                    jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                }
                                Pen drawPen = bodyPens[bodyIndex];
                                DrawBody(joints, jointPoints, dc, drawPen);
                                DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                                bodiesInfo[bodyIndex].Update(bodyIndex, true, minZ, sumZ / countZ, maxZ, body.HandLeftState, body.HandRightState, jointPoints[JointType.HandLeft], jointPoints[JointType.HandRight]);
                            }
                            else
                            {
                                bodiesInfo[bodyIndex].Update(bodyIndex);
                            }
                            bodyIndex++;
                        }
                        int nearestBodyIndex = -1;
                        float nearestDistance = float.MaxValue;
                        foreach (BodyInfo bi in bodiesInfo)
                        {
                            if (nearestBodyIndex == -1 && Convert.ToBoolean(bi.IsTracked))
                            {
                                nearestBodyIndex = Convert.ToInt32(bi.ID);
                                nearestDistance = Convert.ToSingle(bi.AvgZ);
                            }
                            else if (Convert.ToBoolean(bi.IsTracked) && Convert.ToSingle(bi.AvgZ) < nearestDistance)
                            {
                                nearestBodyIndex = Convert.ToInt32(bi.ID);
                                nearestDistance = Convert.ToSingle(bi.AvgZ);
                            }
                        }
                        if (nearestBodyIndex != -1)
                        {
                            bodiesInfo[nearestBodyIndex].IsNearest = "True";
                            if (serialPort != null && serialPort.IsOpen)
                            {
                                BodyInfo focusedBody = bodiesInfo[nearestBodyIndex];
                                meanFilterLeftX.Update(Convert.ToSingle(focusedBody.HandLeftX));
                                meanFilterLeftY.Update(Convert.ToSingle(focusedBody.HandLeftY));
                                meanFilterRightX.Update(Convert.ToSingle(focusedBody.HandRightX));
                                meanFilterRightY.Update(Convert.ToSingle(focusedBody.HandRightY));
                                serialPort.Write(focusedBody.HandLeftStateCode.ToString() + "," + focusedBody.HandRightStateCode.ToString() + "," +
                                    meanFilterLeftX.Value.ToString() + "," + meanFilterLeftY.Value.ToString() + "," +
                                    meanFilterRightX.Value.ToString() + "," + meanFilterRightY.Value.ToString() + serialPort.NewLine);
                            }
                        }
                        else
                        {
                            //No hands are being tracked now
                            if (serialPort != null && serialPort.IsOpen)
                            {
                                meanFilterLeftX.Clear();
                                meanFilterLeftY.Clear();
                                meanFilterRightX.Clear();
                                meanFilterRightY.Clear();
                                serialPort.Write("-1,-1,0,0,0,0" + serialPort.NewLine);
                            }
                        }
                        bodyDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
                    }
                }
            }
        }


        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in bones)
            {
                DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];
            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }
            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }
            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Open:
                    drawingContext.DrawEllipse(handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Lasso:
                    drawingContext.DrawEllipse(handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.NotTracked:
                    drawingContext.DrawEllipse(handNotTrackedBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Unknown:
                    drawingContext.DrawEllipse(handUnknownBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (e.IsAvailable)
            {
                ledKinect.Fill = ledKinectBrush;
            }
            else
            {
                ledKinect.Fill = ledIdleBrush;
            }
        }

        private void UpdatePortsList()
        {
            CmbBoxPorts.Items.Clear();
            foreach (string port in SerialPort.GetPortNames())
            {
                CmbBoxPorts.Items.Add(port);
            }
            CmbBoxPorts.SelectedIndex = 0;
        }

        private void BtnRefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            UpdatePortsList();
        }

        private void BtnStartOutput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                serialPort = new SerialPort((string)CmbBoxPorts.SelectedItem, (int)CmbBoxBaudRate.SelectedItem);
                serialPort.Open();
                BtnRefreshPorts.IsEnabled = false;
                BtnStartOutput.IsEnabled = false;
                BtnStopOutput.IsEnabled = true;
                CmbBoxPorts.IsEnabled = false;
                CmbBoxBaudRate.IsEnabled = false;
                ledSerial.Fill = ledSerialBrush;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open serial port: " + ex.Message);
            }
        }

        private void BtnStopOutput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                serialPort.Close();
                BtnRefreshPorts.IsEnabled = true;
                BtnStartOutput.IsEnabled = true;
                BtnStopOutput.IsEnabled = false;
                CmbBoxPorts.IsEnabled = true;
                CmbBoxBaudRate.IsEnabled = true;
                ledSerial.Fill = ledIdleBrush;
            }
            catch { }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
            if (bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }
            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }
    }
}
