using CSharpFunctionalExtensions;
using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Ports.OSPorts
{

    public interface OSProcessManagementObject
    {
        public Dictionary<string, Dictionary<string, string>> GetManagementInfo(Dictionary<string, object> osProcessInfo);
    }

    public abstract class OSProcessDiagnosticManager
    {
        protected OSProcessDiagnosticsFactory processDiagnosticsFactory;
        protected OSProcessManagementObject processManagementObject;
        public OSProcessDiagnosticManager(OSProcessDiagnosticsFactory _processDiagnosticsFactory, OSProcessManagementObject _processManagementObject)
        {
            processDiagnosticsFactory = _processDiagnosticsFactory;
            processManagementObject = _processManagementObject;
        }
        public abstract List<OSProcessDiagnostics> GetProcessDiagnosticsByProcessId(int pid);

    }
}
