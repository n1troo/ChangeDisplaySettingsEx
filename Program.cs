using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace DisplaySwitch
{
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [Flags]
        private enum DisplayDeviceStateFlags : int
        {
            AttachedToDesktop = 0x1,
            PrimaryDevice = 0x4,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Checking display mode...");
            if (IsOnlyPrimaryMonitorEnabled())
            {
                Console.WriteLine("Only primary monitor is enabled. Switching to extend mode...");
                SetDisplayMode(DisplaySwitchMode.Extend);
            }
            else
            {
                Console.WriteLine("Multiple monitors detected or already in extend mode.");
                SetDisplayMode(DisplaySwitchMode.Internal);
            }

            DisplayMonitorNames();
        }

        private static bool IsOnlyPrimaryMonitorEnabled()
        {
            var displayDevice = new DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };
            int activeMonitorCount = 0;
            int primaryMonitorCount = 0;
            int deviceIndex = 0;

            while (EnumDisplayDevices(null, (uint)deviceIndex, ref displayDevice, 0))
            {
                if ((displayDevice.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
                {
                    activeMonitorCount++;
                    if ((displayDevice.StateFlags & DisplayDeviceStateFlags.PrimaryDevice) != 0)
                    {
                        primaryMonitorCount++;
                    }
                }
                deviceIndex++;
                displayDevice.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)); // Reset size for the next call
            }

            return activeMonitorCount == 1 && primaryMonitorCount == 1;
        }

        private static void SetDisplayMode(DisplaySwitchMode mode)
        {
            string argument = mode switch
            {
                DisplaySwitchMode.Extend => "/extend",
                DisplaySwitchMode.Internal => "/internal",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            Process.Start("displayswitch.exe", argument);
        }

        private static void DisplayMonitorNames()
        {
            var monitorDetails = GetMonitorDetails();

            foreach (var detail in monitorDetails)
            {
                Console.WriteLine($"Display Name: {detail.DisplayName}");
                Console.WriteLine($"Current Horizontal Resolution: {detail.CurrentHorizontalResolution}");
                Console.WriteLine($"Current Vertical Resolution: {detail.CurrentVerticalResolution}");
                Console.WriteLine($"Current Refresh Rate: {detail.CurrentRefreshRate}");
                Console.WriteLine();
            }
        }

        private enum DisplaySwitchMode
        {
            Extend,
            Internal
        }

        public static List<MonitorDetail> GetMonitorDetails()
        {
            var monitorDetailsList = new List<MonitorDetail>();

 

            // Query for display configuration
            var displaySearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (var display in displaySearcher.Get())
            {
                var displayDetail = new MonitorDetail
                {
                    DisplayName = display["Name"]?.ToString(),
                    CurrentHorizontalResolution = display["CurrentHorizontalResolution"]?.ToString(),
                    CurrentVerticalResolution = display["CurrentVerticalResolution"]?.ToString(),
                    CurrentRefreshRate = display["CurrentRefreshRate"]?.ToString()
                };

                monitorDetailsList.Add(displayDetail);
            }

            return monitorDetailsList;
        }
    }

    public class MonitorDetail
    {
        public string MonitorName { get; set; }
        public string ScreenWidth { get; set; }
        public string ScreenHeight { get; set; }
        public string Frequency { get; set; }
        public string DisplayName { get; set; }
        public string CurrentHorizontalResolution { get; set; }
        public string CurrentVerticalResolution { get; set; }
        public string CurrentRefreshRate { get; set; }
    }
}
