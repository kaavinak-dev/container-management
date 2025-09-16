using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Ports;
using FluentResults;

namespace Domain.Entities.Abstractions
{
    public abstract class OSProcessDiagnosticsFactory
    {
        public OSProcessFactory OSProcessFactory { get; set; }

        public OSProcessDiagnosticsFactory(OSProcessFactory _OSProcessFactory)
        {
            OSProcessFactory=_OSProcessFactory;
        }

        public abstract OSProcessDiagnostics CreateProcessDiagnostics(string processId);

        public abstract OSProcessDiagnostics CreateProcessDiagnostics(string processId,Dictionary<string,Dictionary<string,string>> processDiagnosticInfo);
           
        
    }
}
