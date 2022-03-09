using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ClockPlus
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Graphics meter;
        Bitmap bm;
        Thread mainThread;


        int size = ConfigManager.Instance.getSize();
        int numberOfSlots = ConfigManager.Instance.getNumberOfSlots();
        int refreshRate;
        int lowRefreshRate = ConfigManager.Instance.getLowRefreshRate();
        int lowRefreshUpdateAfter;
        double dpiScale;
        int finalSize;

        Color defaultColorMaster1 = ConfigManager.Instance.getDefaultColorMaster1();
        Color defaultColorMaster2 = ConfigManager.Instance.getDefaultColorMaster2();


        Dictionary<string, double> previousValues = new Dictionary<string, double>();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        private void Form1_Load(object sender, EventArgs e)
        {
            //console on
            //AllocConsole();

            //transparency
            this.AllowTransparency = true;
            this.TransparencyKey = this.BackColor;

            //always on top
            this.TopMost = true;
            this.Focus();
            this.TopMost = true;

            //adjust size (DPI aware)
            dpiScale = getScaling();
            finalSize = Convert.ToInt32(size * dpiScale);
            this.Size = new Size(finalSize, finalSize);
            mainPictureBox.Size = this.Size;

            //adjust location
            this.Location = ConfigManager.Instance.getLocation();

            //adjust refresh rate
            MonitorInformation monitorInformation = new MonitorInformation(this.Handle);
            refreshRate = monitorInformation.getRefreshRate();
            lowRefreshUpdateAfter = refreshRate / lowRefreshRate;

            //START
            mainThread = new Thread(mainFunctionOuter);
            mainThread.IsBackground = true;
            mainThread.Start();
        }
        public void mainFunctionOuter()
        {
            Stopwatch sw = new Stopwatch();
            long microSecondsElapsed;
            long targetUpdateTimeMS = Convert.ToInt32(1000.0 / (refreshRate));
            long remainingTimeUS, remainingTimeMS;
            DateTime previous = DateTime.UtcNow;
            DateTime now  = DateTime.UtcNow;
            TimeSpan timeDiff;
            while (true)
            {
                previous = DateTime.UtcNow;

                mainFunction();
                now = DateTime.UtcNow;
                timeDiff = now - previous;

                remainingTimeMS = targetUpdateTimeMS - Convert.ToInt32(timeDiff.TotalMilliseconds);
                //Console.WriteLine("prev: " + previous.Millisecond + "next: " + now.Millisecond + "elapsed: " +timeDiff.TotalMilliseconds + " remaining "+ remainingTimeMS);
                Thread.Sleep(Math.Max(0,Convert.ToInt32(remainingTimeMS)));
            }
        }
        int lowRefreshCounter = 0;
        public void mainFunction()
        {
            bm = new Bitmap(mainPictureBox.Width, mainPictureBox.Height);
            meter = Graphics.FromImage(bm);
            //Console.WriteLine(string.Join<string>("\n", ConfigManager.Instance.getAllEnabled()));
            //previousValues = new Dictionary<string, double>();
            lowRefreshCounter++;
            //if (lowRefreshCounter % lowRefreshUpdateAfter == 0)
            //{
            //    Console.WriteLine("low refresh rate" + ComputerInformation.Instance.getValue("clockSecondsInMillisecond"));
            //}
            //else
            //{
            //    Console.WriteLine("count" + ComputerInformation.Instance.getValue("clockSecondsInMillisecond"));
            //}
            foreach (int id in ConfigManager.Instance.getAllEnabled())
            {
                bool highRefresh = ConfigManager.Instance.getHighRefresh(id);
                string label = ConfigManager.Instance.getLabel(id);
                string bar = ConfigManager.Instance.getBar(id);
                if (highRefresh || (!highRefresh && lowRefreshCounter % lowRefreshUpdateAfter == 0))
                {
                    if (!previousValues.ContainsKey(bar))
                    {
                        previousValues.Add(bar, 0);
                    }
                    previousValues[bar] = ComputerInformation.Instance.getValue(bar);
                    if (!previousValues.ContainsKey(label))
                    {
                        previousValues.Add(label, 0);
                    }
                    previousValues[label] = ComputerInformation.Instance.getValue(label);
                }
                double value;
                try
                {
                    value = previousValues[bar];
                }
                catch
                {
                    value = 0;
                }
                int order = ConfigManager.Instance.getOrder(id);
                Rectangle rectangle = getSlotRectangle(order, numberOfSlots, bm.Size);
                int penWidth = getPenWidth(numberOfSlots, finalSize);
                string breatheType = ConfigManager.Instance.getBreatheType(id);
                int breatheInterval = ConfigManager.Instance.getBreatheInterval(id);
                double breatheKey = getBreatheKey(breatheType, ComputerInformation.Instance.getValue("clockSecondsInMillisecond"), breatheInterval);
                Color defaultColor1 = colorSlider(defaultColorMaster1, defaultColorMaster2, order * 1.0 / numberOfSlots);
                Color defaultColor2 = colorSlider(defaultColorMaster1, defaultColorMaster2, order * 1.0 / numberOfSlots);
                Color color1 = ConfigManager.Instance.getColor1(id, defaultColor1);
                Color color2 = ConfigManager.Instance.getColor2(id, defaultColor2);
                Color color = colorSlider(color1, color2, value);
                Color breatheColor = ConfigManager.Instance.getBreatheColor(id, color);
                color = colorSlider(color, breatheColor, breatheKey);
                
                Pen pen = new Pen(color, penWidth);
                int fontSize = finalSize / 50;
                Font font = new Font(new FontFamily("Arial"), fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                int startAngle = ConfigManager.Instance.getStartAngle(id);
                int bufferAngle = ConfigManager.Instance.getBufferAngle(id);
                int sweepAngle = ConfigManager.Instance.getSweepAngle(id);
                int labelAngle = startAngle + bufferAngle / 2;
                int midPoint = finalSize / 2;
                int labelLocationX = Convert.ToInt32(midPoint + (rectangle.Width) /2 * Math.Cos(DegreeToRadian(labelAngle)));
                int labelLocationY = Convert.ToInt32(midPoint + (rectangle.Height) / 2 * Math.Sin(DegreeToRadian(labelAngle)))-fontSize/2;
                Point labelLocation = new Point(labelLocationX, labelLocationY);
                string labelString;
                try
                {
                   labelString = Math.Round(previousValues[label],2).ToString();
                }
                catch
                {
                    labelString = "";
                }

                Brush brush = new SolidBrush(darkOrLight(color, Color.Black, Color.White));
                //Console.Write(previousValues[bar] + ", ");
                string triggerParameter = ConfigManager.Instance.getTriggerParameter(id);
                double triggerValue = ComputerInformation.Instance.getValue(triggerParameter);
                bool triggered = ConfigManager.Instance.triggered(id, triggerValue);
                if(triggered)
                {
                    meter.DrawArc(pen, rectangle, startAngle, bufferAngle + Convert.ToSingle(value) * sweepAngle);
                    if (label.Length > 0)
                    {
                        int rotateAngle = labelAngle - 90;
                        if (labelAngle <= 0 || labelAngle >= 180)
                        {
                            rotateAngle = rotateAngle + 180;
                        }
                        meter.TranslateTransform(labelLocation.X, labelLocation.Y);
                        meter.RotateTransform(rotateAngle);
                        meter.DrawString(labelString, font, brush, new PointF(0, 0));
                        meter.RotateTransform(-rotateAngle);
                        meter.TranslateTransform(-labelLocation.X, -labelLocation.Y);
                    }
                }
                
                
            }

            mainPictureBox.Image = bm;
        }
        public double getBreatheKey(string type, double driver, double interval)
        {
            switch (type)
            {
                case "sudden": 
                    return (driver % interval) / 1000.0;
                case "smooth":
                    return 0.5+0.5*Math.Sin((driver % interval) *2*Math.PI/interval);
                default: return 1;
            }
        }
        public double DegreeToRadian(double angle)
        {
            return (Math.PI / 180) * angle;
        }
        public int getPenWidth(int numberOfSlots, int size)
        {
            return (size-50) / numberOfSlots / 2;
        }
        public Rectangle getSlotRectangle(int slot, int numberOfSlots, Size bitmapSize)
        {
            int penWidth = getPenWidth(numberOfSlots, bitmapSize.Width);
            int left = penWidth * slot;
            int top = penWidth * slot;
            int right = bitmapSize.Width - left;
            int bottom = bitmapSize.Height - top;
            int width = right - left;
            int height = bottom - top;
            return new Rectangle(left, top, width, height);
        }
        public Color darkOrLight(Color color, Color dark, Color light)
        {
            //https://stackoverflow.com/questions/3942878/how-to-decide-font-color-in-white-or-black-depending-on-background-color
            //if (red*0.299 + green*0.587 + blue*0.114) > 186 use #000000 else use #ffffff
            if(color.R*0.299+color.G*0.587+color.B*0.114 > 186)
            {
                return dark;
            }
            else
            {
                return light;
            }
        }
        private double getScaling()
        {
            float dpiX, dpiY;
            Graphics graphics = this.CreateGraphics();
            dpiX = graphics.DpiX;
            dpiY = graphics.DpiY;
            return dpiX / 96.0;
        }
        Color colorSlider(Color a, Color b, double percent)
        {
            double al = Math.Abs(a.A - percent * (a.A - b.A));
            double r = Math.Abs(a.R - percent * (a.R - b.R));
            double g = Math.Abs(a.G - percent * (a.G - b.G));
            double bl = Math.Abs(a.B - percent * (a.B - b.B));
            if (al > 255) al = 255;
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (bl > 255) bl = 255;
            if (al < 0) al = 0;
            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (bl < 0) bl = 0;
            return Color.FromArgb(
            Convert.ToInt32(al),
            Convert.ToInt32(r),
            Convert.ToInt32(g),
            Convert.ToInt32(bl));
        }


        //Color colorSlider(Color x, Color y, double percent)
        //{
        //    float h = Convert.ToSingle(Math.Abs(x.GetHue() - percent * (x.GetHue() - y.GetHue())));
        //    float s = Convert.ToSingle(Math.Abs(x.GetSaturation() - percent * (x.GetSaturation() - y.GetSaturation())));
        //    float b = Convert.ToSingle(Math.Abs(x.GetBrightness() - percent * (x.GetBrightness() - y.GetBrightness())));
        //    int a = Convert.ToInt32(Math.Abs(x.A - percent * (x.A - y.A)));
        //    if (a > 255) a = 255;
        //    if (h > 360) h = 360;
        //    if (s > 1) s = 1;
        //    if (b > 1) b = 1;
        //    if (a < 0) a = 0;
        //    if (h < 0) h = 0;
        //    if (s < 0) s = 0;
        //    if (b < 0) b = 0;
        //    return FromAhsb(a, h, s, b);
        //}


        ///https://stackoverflow.com/questions/4106363/converting-rgb-to-hsb-colors
        /// <summary>
        /// Creates a Color from alpha, hue, saturation and brightness.
        /// </summary>
        /// <param name="alpha">The alpha channel value.</param>
        /// <param name="hue">The hue value.</param>
        /// <param name="saturation">The saturation value.</param>
        /// <param name="brightness">The brightness value.</param>
        /// <returns>A Color with the given values.</returns>
        public static Color FromAhsb(int alpha, float hue, float saturation, float brightness)
        {
            if (0 > alpha
                || 255 < alpha)
            {
                throw new ArgumentOutOfRangeException(
                    "alpha",
                    alpha,
                    "Value must be within a range of 0 - 255.");
            }

            if (0f > hue
                || 360f < hue)
            {
                throw new ArgumentOutOfRangeException(
                    "hue",
                    hue,
                    "Value must be within a range of 0 - 360.");
            }

            if (0f > saturation
                || 1f < saturation)
            {
                throw new ArgumentOutOfRangeException(
                    "saturation",
                    saturation,
                    "Value must be within a range of 0 - 1.");
            }

            if (0f > brightness
                || 1f < brightness)
            {
                throw new ArgumentOutOfRangeException(
                    "brightness",
                    brightness,
                    "Value must be within a range of 0 - 1.");
            }

            if (0 == saturation)
            {
                return Color.FromArgb(
                                    alpha,
                                    Convert.ToInt32(brightness * 255),
                                    Convert.ToInt32(brightness * 255),
                                    Convert.ToInt32(brightness * 255));
            }

            float fMax, fMid, fMin;
            int iSextant, iMax, iMid, iMin;

            if (0.5 < brightness)
            {
                fMax = brightness - (brightness * saturation) + saturation;
                fMin = brightness + (brightness * saturation) - saturation;
            }
            else
            {
                fMax = brightness + (brightness * saturation);
                fMin = brightness - (brightness * saturation);
            }

            iSextant = (int)Math.Floor(hue / 60f);
            if (300f <= hue)
            {
                hue -= 360f;
            }

            hue /= 60f;
            hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (0 == iSextant % 2)
            {
                fMid = (hue * (fMax - fMin)) + fMin;
            }
            else
            {
                fMid = fMin - (hue * (fMax - fMin));
            }

            iMax = Convert.ToInt32(fMax * 255);
            iMid = Convert.ToInt32(fMid * 255);
            iMin = Convert.ToInt32(fMin * 255);

            switch (iSextant)
            {
                case 1:
                    return Color.FromArgb(alpha, iMid, iMax, iMin);
                case 2:
                    return Color.FromArgb(alpha, iMin, iMax, iMid);
                case 3:
                    return Color.FromArgb(alpha, iMin, iMid, iMax);
                case 4:
                    return Color.FromArgb(alpha, iMid, iMin, iMax);
                case 5:
                    return Color.FromArgb(alpha, iMax, iMin, iMid);
                default:
                    return Color.FromArgb(alpha, iMax, iMid, iMin);
            }
        }
        private void mainPictureBox_Click(object sender, EventArgs e)
        {

        }
    }
}
