using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Abstractions
{
    public interface OSLakeOrchestrator
    {
        public Dictionary<string, object> GetOSLakesInfo(string osLakeType);
    }
}
