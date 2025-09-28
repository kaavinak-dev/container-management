using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Abstractions
{
    public abstract class BaseOSLake
    {
        public string OSLakeName;
        public string OSLakeIp;

        public BaseOSLake(string OSLakeName,string OSLakeIp)
        {
            this.OSLakeName = OSLakeName;
            this.OSLakeIp = OSLakeIp;
        }
    }
}
