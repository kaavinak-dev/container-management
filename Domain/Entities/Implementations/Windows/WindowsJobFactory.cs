using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Windows
{
    public class WindowsJobFactory:OSJobFactory
    {
        public override OSJob CreateJob(string jobHandle)
        {
            return new WindowsJob(jobHandle);
        }

        public override OSJob CreateJob(string jobHandle, string jobName)
        {
            return new WindowsJob(jobHandle, jobName);
        }
    }
}
