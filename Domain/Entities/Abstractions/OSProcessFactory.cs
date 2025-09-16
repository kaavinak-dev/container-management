using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Abstractions
{
    public abstract class OSProcessFactory
    {
        public OSProcessFactory() { 
        
        }
        public abstract OSProcess CreateProcess(string processId);
    }
}
