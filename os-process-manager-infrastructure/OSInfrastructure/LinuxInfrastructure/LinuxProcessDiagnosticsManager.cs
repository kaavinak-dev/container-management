using Domain.Entities.Abstractions;
using Domain.Entities.Implementations.Linux;
using Domain.Ports.OSPorts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSProcessManagerInfastructure.OSInfrastructure.LinuxInfrastructure
{
    public class LinuxProcessManagement : OSProcessManagementObject
    {
        public Dictionary<string, Dictionary<string, string>> GetManagementInfo(Dictionary<string, object> managementInfo)
        {
            var extractedData = new Dictionary<string, Dictionary<string, string>>();
            if (managementInfo.ContainsKey("system"))
            {
                extractedData["system"] = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    JsonConvert.SerializeObject(managementInfo["system"])) ?? new();
            }
            if (managementInfo.ContainsKey("process"))
            {
                extractedData["process"] = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    JsonConvert.SerializeObject(managementInfo["process"])) ?? new();
            }
            return extractedData;
        }
    }

    public class LinuxProcessDiagnosticsManager : OSProcessDiagnosticManager
    {
        public LinuxProcessDiagnosticsManager(
            OSProcessDiagnosticsFactory processDiagnosticsFactory,
            OSProcessManagementObject managementObject)
            : base(processDiagnosticsFactory, managementObject) { }

        public override List<OSProcessDiagnostics> GetProcessDiagnosticsByProcessId(int pid)
        {
            var statusPath = $"/proc/{pid}/status";
            if (!File.Exists(statusPath))
            {
                return new List<OSProcessDiagnostics>();
            }

            // Parse /proc/{pid}/status into key-value pairs
            var statusLines = File.ReadAllLines(statusPath);
            var processProps = statusLines
                .Select(line => line.Split(':', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(
                    parts => parts[0].Trim(),
                    parts => parts[1].Trim());

            // Read /proc/{pid}/stat for additional system-level metrics
            var systemProps = new Dictionary<string, string>();
            var statPath = $"/proc/{pid}/stat";
            if (File.Exists(statPath))
            {
                var statContent = File.ReadAllText(statPath).Trim();
                systemProps["stat"] = statContent;
            }

            var managementInput = new Dictionary<string, object>
            {
                ["process"] = processProps,
                ["system"] = systemProps,
            };

            var diagInfo = processManagementObject.GetManagementInfo(managementInput);
            var diagnostics = processDiagnosticsFactory.CreateProcessDiagnostics(pid.ToString(), diagInfo);
            return new List<OSProcessDiagnostics> { diagnostics };
        }
    }
}
