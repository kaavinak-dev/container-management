using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using Newtonsoft;
using Newtonsoft.Json;
using Domain.Ports;


namespace Domain.Entities.Implementations.Windows
{
    public class WindowsProcessDiagnosticsFactory:OSProcessDiagnosticsFactory
    {
       
        public WindowsProcessDiagnosticsFactory(OSProcessFactory processFactory) : base(processFactory)
        {

        }

        public override OSProcessDiagnostics CreateProcessDiagnostics(string processId)
        {
            var process = OSProcessFactory.CreateProcess(processId);
            return   new WindowsProcessDiagnostics(process);
            
        }

        public override OSProcessDiagnostics CreateProcessDiagnostics(string processId,Dictionary<string,Dictionary<string,string>> processDiagnosticInfo)
        {
            var windowsDiagnostics = new WindowsProcessDiagnostics(OSProcessFactory.CreateProcess(processId));
            windowsDiagnostics.ProcessProps = processDiagnosticInfo["process"];
            windowsDiagnostics.SystemProps = processDiagnosticInfo["system"];
            return windowsDiagnostics;
        }


    }
}
