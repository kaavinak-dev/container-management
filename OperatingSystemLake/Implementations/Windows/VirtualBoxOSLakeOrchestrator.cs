using OperatingSystemHelpers.Abstractions;
using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace OperatingSystemLake.Implementations.Windows
{
    public class VirtualBoxOSLakeOrchestrator : OSLakeOrchestrator
    {
        private static ProcessCommunicator virtualBoxProcess;
        public const string IPQuery = "/VirtualBox/GuestInfo/Net/0/V4/IP";

        public VirtualBoxOSLakeOrchestrator(ProcessCommunicator processCommunicator) {
            virtualBoxProcess = processCommunicator;
            virtualBoxProcess.StartProcess();
        }

        private string? processOSLakeName(string osLakeNameString)
        {
            var match = Regex.Match(osLakeNameString, "\"(.*?)\"");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        public BaseOSLake GetRunningOSLakeForOrchestratorType(OSLakeTypes osLakeType)
        {
            virtualBoxProcess.StartTransaction();
            string? lakeName = null;
            virtualBoxProcess.ExecuteCommand("VBoxManage.exe list runningvms", (err, outputLogs) =>
            {
                if (outputLogs.Data != null)
                {
                    if (outputLogs.Data.ToLower().Contains(osLakeType.ToString().ToLower()) && lakeName == null)
                    {
                        lakeName=processOSLakeName((string)outputLogs.Data);

                    }
                }   
            }, (err, errorLogs) =>
            {
                if (errorLogs.Data != null) {
                    Console.WriteLine(errorLogs.Data);
                }
            });
            virtualBoxProcess.EndTransaction();
            var IpAddress = GetOSLakeIp(lakeName);
            return new WindowsOSLake(lakeName, IpAddress);
        }

        public string GetOSLakeIp(string activeOSLakeName)
        {
            virtualBoxProcess.StartTransaction();
            string? ipAddress = null;
            virtualBoxProcess.ExecuteCommand($"VBoxManage.exe guestproperty enumerate \"{activeOSLakeName}\"", (err, outputLogs) =>
            {
                if (outputLogs.Data != null)
                {
                    if (outputLogs.Data.Contains(IPQuery) && ipAddress==null)
                    {
                        var match = Regex.Match(outputLogs.Data, @"'([\d\.]+)'");
                        if (match.Success)
                        {
                            string ip = match.Groups[1].Value;
                            ipAddress = ip; // Output: 192.168.1.8
                        }
                    }
                }

            }, (err, errorLogs) =>
            {
                if (errorLogs.Data != null) {

                    Console.WriteLine(errorLogs.Data);
                }
            });
            virtualBoxProcess.EndTransaction();
            virtualBoxProcess.EndProcess();
            return ipAddress;
        }

        public Dictionary<string, object> GetOSLakesInfo1(string osLakeType)
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
