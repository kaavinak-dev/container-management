using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure.NativeStructs
{
    public class JobNativeStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WindowsJobSecurityAttributes
        {
            public int nLength;
            public IntPtr securityDescriptor;
            public bool inheritHandle;
        }

        public static IntPtr GetDefaultJobSecurityAttribute()
        {
            WindowsJobSecurityAttributes windowsJobSecurityAttributes = new WindowsJobSecurityAttributes();
            windowsJobSecurityAttributes.inheritHandle = false;
            windowsJobSecurityAttributes.securityDescriptor = IntPtr.Zero;
            windowsJobSecurityAttributes.nLength = Marshal.SizeOf(typeof(WindowsJobSecurityAttributes));
            IntPtr securityPtr = Marshal.AllocHGlobal(Marshal.SizeOf(windowsJobSecurityAttributes));
            Marshal.StructureToPtr(windowsJobSecurityAttributes, securityPtr, false);
            return securityPtr;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public nuint MinimumWorkingSetSize;
            public nuint MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public long Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public nuint ProcessMemoryLimit;
            public nuint JobMemoryLimit;
            public nuint PeakProcessMemoryUsed;
            public nuint PeakJobMemoryUsed;
        }



    }

}
