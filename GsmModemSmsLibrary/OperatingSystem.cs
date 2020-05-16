using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GsmModemSmsLibrary
{
    public static class OperatingSystem
    {
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}
