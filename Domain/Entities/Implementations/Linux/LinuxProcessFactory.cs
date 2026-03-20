using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Linux
{
    public class LinuxProcessFactory : OSProcessFactory
    {
        public override OSProcess CreateProcess(string processId)
        {
            return new LinuxProcess(processId);
        }
    }
}
