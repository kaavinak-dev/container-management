using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Windows
{
    public class WindowsJob: OSJob
    {
        public WindowsJob(string jobHandle):base(jobHandle) { 
        
        }
        public WindowsJob(string jobHandle, string jobName) : base(jobHandle, jobName) { 
        
        
        }

    }
}
