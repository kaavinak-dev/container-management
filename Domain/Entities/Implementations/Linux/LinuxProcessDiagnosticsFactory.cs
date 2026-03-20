using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Linux
{
    public class LinuxProcessDiagnosticsFactory : OSProcessDiagnosticsFactory
    {
        public LinuxProcessDiagnosticsFactory(OSProcessFactory processFactory) : base(processFactory) { }

        public override OSProcessDiagnostics CreateProcessDiagnostics(string processId)
        {
            var process = OSProcessFactory.CreateProcess(processId);
            return new LinuxProcessDiagnostics(process);
        }

        public override OSProcessDiagnostics CreateProcessDiagnostics(string processId, Dictionary<string, Dictionary<string, string>> processDiagnosticInfo)
        {
            var diagnostics = new LinuxProcessDiagnostics(OSProcessFactory.CreateProcess(processId));
            if (processDiagnosticInfo.TryGetValue("process", out var processProps))
                diagnostics.ProcessProps = processProps;
            if (processDiagnosticInfo.TryGetValue("system", out var systemProps))
                diagnostics.SystemProps = systemProps;
            return diagnostics;
        }
    }
}
