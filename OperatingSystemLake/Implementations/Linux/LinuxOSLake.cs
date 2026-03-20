using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Implementations.Linux
{
    public class LinuxOSLake : BaseOSLake
    {
        public LinuxOSLake(string osLakeName, string osLakeIp) : base(osLakeName, osLakeIp) { }
    }
}
