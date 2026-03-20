using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Linux
{
    public class LinuxProcessDiagnostics : OSProcessDiagnostics
    {
        public Dictionary<string, string> SystemProps { get; set; } = new();
        public Dictionary<string, string> ProcessProps { get; set; } = new();

        public LinuxProcessDiagnostics(OSProcess process) : base(process) { }

        public LinuxProcessDiagnostics() : base() { }
    }
}
