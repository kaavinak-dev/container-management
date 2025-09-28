using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Abstractions
{

    public enum OSLakeTypes
    {
        Windows
    }

    public interface OSLakeOrchestrator
    {
        public BaseOSLake GetRunningOSLakeForOrchestratorType(OSLakeTypes lakeType);
    }
}
