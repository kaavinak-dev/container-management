using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Windows
{
    public  class WindowsProcessFactory: OSProcessFactory
    {
        public override OSProcess CreateProcess(string processId)
        {
            return new WindowsProcess(processId);   
        
        }
    }
}
