using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TrackpadCompanion
{
    // Talks to the AmtPtp driver (imbushuo's Magic Trackpad Precision Touchpad driver).
    public static class Driver
    {
        const string DevicePath = @"\\.\AmtPtpControlDeviceUm";
        const uint IOCTL_RELOAD = (0x22u << 16) | (0x800u << 2);
        const uint IOCTL_BATTERY = (0x22u << 16) | (0x801u << 2);
        static readonly Guid RestartGuid = new Guid("4a5064e5-7d39-41d1-a0e4-81097edce967");
        const string ParamsPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF\Services\AmtPtpDeviceUsbUm\Parameters";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DeviceIoControl(IntPtr h, uint code, IntPtr inBuf, int inLen, IntPtr outBuf, int outLen, out int ret, IntPtr ov);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

        public static int LastError;

        static bool Ioctl(uint code, bool wantOutput, out int value)
        {
            value = 0; LastError = 0;
            IntPtr h = CreateFile(DevicePath, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (h == IntPtr.Zero || h == new IntPtr(-1)) { LastError = Marshal.GetLastWin32Error(); return false; }
            IntPtr buf = IntPtr.Zero;
            try
            {
                int len = wantOutput ? 4 : 0;
                if (wantOutput) buf = Marshal.AllocHGlobal(4);
                int ret;
                bool ok = DeviceIoControl(h, code, IntPtr.Zero, 0, buf, len, out ret, IntPtr.Zero);
                if (!ok) { LastError = Marshal.GetLastWin32Error(); return false; }
                if (wantOutput) value = Marshal.ReadInt32(buf);
                return true;
            }
            finally { if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf); CloseHandle(h); }
        }

        // Returns 0..100, or -1 when unavailable (USB connection, trackpad off, driver missing).
        public static int ReadBattery()
        {
            int v;
            if (Ioctl(IOCTL_BATTERY, true, out v) && v >= 0 && v <= 100) return v;
            return -1;
        }

        public static bool CanOpen()
        {
            IntPtr h = CreateFile(DevicePath, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (h == IntPtr.Zero || h == new IntPtr(-1)) { LastError = Marshal.GetLastWin32Error(); return false; }
            CloseHandle(h);
            return true;
        }

        public static bool Reload() { int v; return Ioctl(IOCTL_RELOAD, false, out v); }

        // ---- registry ----
        public class Raw { public int ButtonDisabled, FeedbackClick = 394775, FeedbackRelease = 20, StopPressure, StopSize = -1, IgnoreButtonFinger = 1, IgnoreNearFingers = 1, PalmRejection = 1; }

        static int Get(RegistryKey k, string name, int def)
        {
            if (k == null) return def;
            object o = k.GetValue(name);
            return o is int ? (int)o : def;
        }

        public static Raw ReadRaw()
        {
            Raw r = new Raw();
            using (RegistryKey b = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey k = b.OpenSubKey(ParamsPath))
            {
                r.ButtonDisabled = Get(k, "ButtonDisabled", r.ButtonDisabled);
                r.FeedbackClick = Get(k, "FeedbackClick", r.FeedbackClick);
                r.FeedbackRelease = Get(k, "FeedbackRelease", r.FeedbackRelease);
                r.StopPressure = Get(k, "StopPressure", r.StopPressure);
                r.StopSize = Get(k, "StopSize", r.StopSize);
                r.IgnoreButtonFinger = Get(k, "IgnoreButtonFinger", r.IgnoreButtonFinger);
                r.IgnoreNearFingers = Get(k, "IgnoreNearFingers", r.IgnoreNearFingers);
                r.PalmRejection = Get(k, "PalmRejection", r.PalmRejection);
            }
            return r;
        }

        public static void WriteRaw(Raw r)
        {
            WriteTo(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF\Services", "AmtPtpDeviceUsbUm", r);
            WriteTo(@"SYSTEM\CurrentControlSet\Services", "AmtPtpHidFilter", r);
        }

        static void WriteTo(string parent, string svc, Raw r)
        {
            using (RegistryKey b = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey p = b.OpenSubKey(parent, true))
            using (RegistryKey s = p.CreateSubKey(svc))
            using (RegistryKey k = s.CreateSubKey("Parameters"))
            {
                k.SetValue("ButtonDisabled", r.ButtonDisabled, RegistryValueKind.DWord);
                k.SetValue("FeedbackClick", r.FeedbackClick, RegistryValueKind.DWord);
                k.SetValue("FeedbackRelease", r.FeedbackRelease, RegistryValueKind.DWord);
                k.SetValue("StopPressure", r.StopPressure, RegistryValueKind.DWord);
                k.SetValue("StopSize", r.StopSize, RegistryValueKind.DWord);
                k.SetValue("IgnoreButtonFinger", r.IgnoreButtonFinger, RegistryValueKind.DWord);
                k.SetValue("IgnoreNearFingers", r.IgnoreNearFingers, RegistryValueKind.DWord);
                k.SetValue("PalmRejection", r.PalmRejection, RegistryValueKind.DWord);
            }
        }

        // ---- device restart (same SetupAPI sequence the original panel uses) ----
        [StructLayout(LayoutKind.Sequential)] struct SP_DEVINFO_DATA { public int cbSize; public Guid ClassGuid; public int DevInst; public IntPtr Reserved; }
        [StructLayout(LayoutKind.Sequential)] struct SP_PROPCHANGE_PARAMS { public int cbSize; public int InstallFunction; public int StateChange; public int Scope; public int HwProfile; }
        [DllImport("setupapi.dll", SetLastError = true)] static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr h, int flags);
        [DllImport("setupapi.dll", SetLastError = true)] static extern bool SetupDiEnumDeviceInfo(IntPtr set, int idx, ref SP_DEVINFO_DATA d);
        [DllImport("setupapi.dll", SetLastError = true)] static extern bool SetupDiSetClassInstallParams(IntPtr set, ref SP_DEVINFO_DATA d, ref SP_PROPCHANGE_PARAMS p, int size);
        [DllImport("setupapi.dll", SetLastError = true)] static extern bool SetupDiCallClassInstaller(int fn, IntPtr set, ref SP_DEVINFO_DATA d);
        [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

        static void SetState(bool enable)
        {
            Guid g = RestartGuid;
            IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, 0x12);
            if (set == IntPtr.Zero || set == new IntPtr(-1)) return;
            try
            {
                for (int i = 0; ; i++)
                {
                    SP_DEVINFO_DATA d = new SP_DEVINFO_DATA();
                    d.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                    if (!SetupDiEnumDeviceInfo(set, i, ref d)) break;
                    SP_PROPCHANGE_PARAMS p = new SP_PROPCHANGE_PARAMS();
                    p.cbSize = 8; p.InstallFunction = 0x12; p.StateChange = enable ? 1 : 2; p.Scope = 1; p.HwProfile = 0;
                    if (SetupDiSetClassInstallParams(set, ref d, ref p, Marshal.SizeOf(typeof(SP_PROPCHANGE_PARAMS))))
                        SetupDiCallClassInstaller(0x12, set, ref d);
                }
            }
            finally { SetupDiDestroyDeviceInfoList(set); }
        }

        public static void RestartDevices()
        {
            SetState(false);
            System.Threading.Thread.Sleep(500);
            SetState(true);
            System.Threading.Thread.Sleep(1500);
        }
    }
}
