using OperatingSystemHelpers.Abstractions;
using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OperatingSystemLake.Implementations.Windows
{
    public class VirtualBoxOSLakeConnector : OSLakeConnector
    {
        private readonly ProcessCommunicator _processCommunicator;
        public const string IPQuery = "/VirtualBox/GuestInfo/Net/0/V4/IP";

        public VirtualBoxOSLakeConnector(ProcessCommunicator processCommunicator)
        {
            _processCommunicator = processCommunicator;
            _processCommunicator.StartProcess();
        }

        public override List<BaseOSLake> GetAvailableOSLakes()
        {
            var lakes = new List<BaseOSLake>();
            var vmNames = new List<string>();

            _processCommunicator.StartTransaction();
            _processCommunicator.ExecuteCommand("VBoxManage.exe list runningvms",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null)
                    {
                        var name = ParseVmName(outputLogs.Data);
                        if (name != null) vmNames.Add(name);
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null) Console.WriteLine(errorLogs.Data);
                });
            _processCommunicator.EndTransaction();

            foreach (var name in vmNames)
            {
                var ip = GetOSLakeIp(name);
                if (ip != null) lakes.Add(new WindowsOSLake(name, ip));
            }

            return lakes;
        }

        public override BaseOSLake GetOSLakeByType(OSLakeTypes osType)
        {
            string? lakeName = null;

            _processCommunicator.StartTransaction();
            _processCommunicator.ExecuteCommand("VBoxManage.exe list runningvms",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null && lakeName == null)
                    {
                        if (outputLogs.Data.ToLower().Contains(osType.ToString().ToLower()))
                        {
                            lakeName = ParseVmName(outputLogs.Data);
                        }
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null) Console.WriteLine(errorLogs.Data);
                });
            _processCommunicator.EndTransaction();

            var ip = GetOSLakeIp(lakeName);
            return osType == OSLakeTypes.Windows
                ? new WindowsOSLake(lakeName, ip)
                : throw new NotSupportedException($"VirtualBox connector does not support OS type: {osType}");
        }

        public override string GetOSLakeIp(string lakeName)
        {
            string? ipAddress = null;

            _processCommunicator.StartTransaction();
            _processCommunicator.ExecuteCommand($"VBoxManage.exe guestproperty enumerate \"{lakeName}\"",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null && ipAddress == null)
                    {
                        if (outputLogs.Data.Contains(IPQuery))
                        {
                            var match = Regex.Match(outputLogs.Data, @"'([\d\.]+)'");
                            if (match.Success) ipAddress = match.Groups[1].Value;
                        }
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null) Console.WriteLine(errorLogs.Data);
                });
            _processCommunicator.EndTransaction();
            _processCommunicator.EndProcess();

            return ipAddress;
        }

        private string? ParseVmName(string vmListLine)
        {
            var match = Regex.Match(vmListLine, "\"(.*?)\"");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
