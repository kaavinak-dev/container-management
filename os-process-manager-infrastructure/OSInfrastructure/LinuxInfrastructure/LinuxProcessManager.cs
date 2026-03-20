using Domain.Entities.Abstractions;
using Domain.Entities.Implementations.Linux;
using Domain.Ports.OSPorts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSProcessManagerInfastructure.OSInfrastructure.LinuxInfrastructure
{
    public class LinuxProcessManager : OSProcessManager
    {
        public override OSProcess CreateProcessInOS()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            var process = Process.Start(startInfo);
            return new LinuxProcess(process.Id.ToString());
        }
    }
}
