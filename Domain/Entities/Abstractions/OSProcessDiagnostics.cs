using Domain.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Abstractions
{
    public abstract class OSProcessDiagnostics
    {
        public OSProcess Process;
        public OSProcessDiagnostics(OSProcess _process)
        {
            Process= _process;
        }

        public OSProcessDiagnostics()
        {

        }

             
                 
    }

    
            
}
