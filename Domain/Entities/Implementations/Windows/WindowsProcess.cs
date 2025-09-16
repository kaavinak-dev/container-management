using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Windows
{
    public class WindowsProcess : OSProcess
    {

        public string? JobHandle { get; set; }
        public string? ProcessHandle { get; set; }

        public WindowsProcess():base() { }

        public WindowsProcess(string processId):base(processId)
        {

        }

        
        

    }
}
