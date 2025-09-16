using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Implementations.Windows
{
    public class VirtualBoxOSLakeOrchestrator : OSLakeOrchestrator
    {
        public Dictionary<string, object> GetOSLakesInfo(string osLakeType)
        {
           
            var processStartUp = new ProcessStartInfo()
            {
                FileName = "VBoxManage.exe",
                Arguments="list runningvms",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            var process = Process.Start(processStartUp);
            bool lakeLocated = false;
            string lakeDataString = null;
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null && !lakeLocated)
                {
                    var data = args.Data;
                    if (data.ToLower().Contains("windows") && osLakeType.ToLower().Contains("windows"))
                    {
                        lakeLocated = true;
                        lakeDataString += data;
                    }
                }
            };
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            if (lakeLocated)
            {
                process.Kill();
            }
            process.WaitForExit();
            return new Dictionary<string, object>();

        }
    }
}
