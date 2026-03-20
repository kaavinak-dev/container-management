using Domain.Entities.Abstractions;
using Domain.Entities.Implementations.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using Domain.Ports.OSPorts;

namespace OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure
{

    public class WindowsProcessManagement : OSProcessManagementObject
    {
        
        public Dictionary<string,Dictionary<string, string>> GetManagementInfo(Dictionary<string,object> managementInfo)
        {
            var extractedData  = new Dictionary<string, Dictionary<string, string>>();
            if (managementInfo.ContainsKey("system"))
            {
                extractedData["system"] = JsonConvert.DeserializeObject<Dictionary<string,string>>(JsonConvert.SerializeObject(managementInfo["system"])) ?? new();
            }
            if (managementInfo.ContainsKey("process"))
            {
                extractedData["process"] = JsonConvert.DeserializeObject<Dictionary<string,string>>(JsonConvert.SerializeObject(managementInfo["process"])) ?? new();
            }
            return extractedData;
        }
    }

    public class WindowsProcessDiagnosticsManager : OSProcessDiagnosticManager
    {

        public WindowsProcessDiagnosticsManager(OSProcessDiagnosticsFactory processDiagnosticsFactory,OSProcessManagementObject managementObject) : base(processDiagnosticsFactory,managementObject) { }
        public override List<OSProcessDiagnostics> GetProcessDiagnosticsByProcessId(int pid)
        {
         
            string query = $"SELECT * FROM Win32_Process WHERE ProcessId = {pid}";
            //            Dictionary<Process_Diagnostic_types, Dictionary<string, string>> processDiagnostics = new Dictionary<string, Dictionary<string, string>>() { };
            //Dictionary<string, Dictionary<Process_Diagnostic_types, Dictionary<string, string>>> processCalcs = new();
            List<OSProcessDiagnostics> processDiagnostics = new List<OSProcessDiagnostics>();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach(ManagementObject proc in results)
                {
                    Dictionary<string, object> systemProps = new Dictionary<string, object>() { };
                    Dictionary<string, object> processProps = new Dictionary<string, object>() { };
                    var processId=proc["ProcessId"].ToString();
                    foreach(var sysproc in proc.SystemProperties)
                    {
                        if (systemProps.ContainsKey(sysproc.Name)) { continue; }
                        systemProps[sysproc.Name] = sysproc.Value;  
                    }
                    foreach(var processProp in proc.Properties)
                    {
                        if(processProps.ContainsKey(processProp.Name)) { continue; }
                        processProps[processProp.Name] = processProp.Value; 
                    }
                    Dictionary<string, object> processDetail = new();
                    processDetail["system"] = systemProps;
                    processDetail["process"] = processProps;
                    var processDiagnosis=this.processDiagnosticsFactory.CreateProcessDiagnostics(processId,this.processManagementObject.GetManagementInfo(processDetail));
                    processDiagnostics.Add(processDiagnosis);

                              
                }
            }
            return processDiagnostics;
   
        }
    }
}
