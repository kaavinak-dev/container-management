using Domain.Entities.Abstractions;
using Domain.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Windows
{
    public class WindowsProcessDiagnostics:OSProcessDiagnostics
    {

        public Dictionary<string, string> SystemProps { get; set; } = new();
        public Dictionary<string,string> ProcessProps { get; set; } = new();    
        public WindowsProcessDiagnostics(OSProcess process) : base(process)
        {

        }
        public WindowsProcessDiagnostics():base()
        {
            
        }

    }

}
