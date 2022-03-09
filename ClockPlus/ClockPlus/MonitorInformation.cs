using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClockPlus
{
    class MonitorInformation
    {
        int width = 0;
        int height = 0;
        int refreshRate = 0;



        //https://blog.getpaint.net/2017/08/12/win32-how-to-get-the-refresh-rate-for-a-window/
        //https://github.com/rickbrew/RefreshRateWpf/blob/a95c9f5e7f9b423ec90d9194408320c5cbe86eb7/RefreshRateWpfApp/MainWindow.xaml.cs#L110
        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);




        // GetMonitorInfo
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfoW(
            IntPtr hMonitor,
            ref MONITORINFOEXW lpmi);



        // MonitorFromWindow
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        public MonitorInformation(IntPtr hwnd)
        {
            bool error = false;
            // 2. Get a monitor handle ("HMONITOR") for the window. 
            IntPtr hmonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hmonitor == IntPtr.Zero)
            {
                error = true;
            }

            // 3. Get more information about the monitor.
            MONITORINFOEXW monitorInfo = new MONITORINFOEXW();
            monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

            bool bResult = GetMonitorInfoW(hmonitor, ref monitorInfo);
            if (!bResult)
            {
                error = true;
            }

            // 4. Get the current display settings for that monitor, which includes the resolution and refresh rate.
            DEVMODEW devMode = new DEVMODEW();
            devMode.dmSize = (ushort)Marshal.SizeOf<DEVMODEW>();

            bResult = EnumDisplaySettingsW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, out devMode);
            if (!bResult)
            {
                error = true;
            }
            if(!error)
            {
                width = Convert.ToInt32(devMode.dmPelsWidth);
                height = Convert.ToInt32(devMode.dmPelsHeight);
                refreshRate = Convert.ToInt32(devMode.dmDisplayFrequency);

            }

        }
        public int getWidth()
        {
            return width;
        }
        public int getHeight()
        {
            return height;
        }
        public int getRefreshRate()
        {
            return refreshRate;
        }

        // RECT
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        // MONITORINFOEX
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFOEXW
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }
        // EnumDisplaySettings
        private const uint ENUM_CURRENT_SETTINGS = unchecked((uint)-1);

        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplaySettingsW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszDeviceName,
            uint iModeNum,
            out DEVMODEW lpDevMode);    

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODEW
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;

            /*public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;*/
            // These next 4 int fields are a union with the above 8 shorts, but we don't need them right now
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;

            public uint dmNupOrDisplayFlags;
            public uint dmDisplayFrequency;

            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }
    }
}
