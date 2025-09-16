using Domain.Entities.Abstractions;
using Domain.Ports.OSPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure.NativeStructs;
using System.Runtime.InteropServices;
using Domain.Entities.Implementations.Windows;
using System.Security.Cryptography;

namespace OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure
{
    public class WindowsProcessManager : OSProcessManager
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref ProcessNativeStructs.STARTUPINFO lpStartupInfo,
            out ProcessNativeStructs.PROCESS_INFORMATION lpProcessInformation
        );

        public const uint CREATE_NEW_CONSOLE = 0x00000010;
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;


        public override OSProcess CreateProcessInOS()
        {
            ProcessNativeStructs.STARTUPINFO si = new ProcessNativeStructs.STARTUPINFO();
            si.cb = (uint)Marshal.SizeOf(typeof(ProcessNativeStructs.STARTUPINFO));
            ProcessNativeStructs.PROCESS_INFORMATION pi = new ProcessNativeStructs.PROCESS_INFORMATION();
            CreateProcess(
                null,
                "cmd.exe",
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT,
                IntPtr.Zero,
                null,
                ref si,
                out pi
            );
            return new WindowsProcess(pi.dwProcessId.ToString())
            {
                ProcessHandle = pi.hProcess.ToString()
            };
          
        }
    }
}
