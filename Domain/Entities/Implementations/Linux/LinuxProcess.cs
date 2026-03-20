using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Linux
{
    public class LinuxProcess : OSProcess
    {
        public LinuxProcess() : base() { }

        public LinuxProcess(string processId) : base(processId) { }
    }
}
