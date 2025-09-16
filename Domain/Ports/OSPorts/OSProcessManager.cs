using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Ports.OSPorts
{
    public abstract class OSProcessManager
    {
        public abstract OSProcess CreateProcessInOS();
    }
}
