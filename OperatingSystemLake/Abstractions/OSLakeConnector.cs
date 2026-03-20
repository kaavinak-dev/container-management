using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Abstractions
{
    /// <summary>
    /// Abstract base for OS Lake connectors. An OS Lake is a host (VM, cloud instance, etc.)
    /// that runs a Docker daemon where user containers are created.
    /// Each connector implementation knows how to discover and communicate with a specific
    /// hosting technology (VirtualBox, Docker Machine, AWS, etc.).
    /// </summary>
    public abstract class OSLakeConnector
    {
        /// <summary>
        /// Returns all currently running/available OS lake instances discoverable by this connector.
        /// </summary>
        public abstract List<BaseOSLake> GetAvailableOSLakes();

        /// <summary>
        /// Returns the first available OS lake instance matching the requested OS type (Windows/Linux).
        /// </summary>
        public abstract BaseOSLake GetOSLakeByType(OSLakeTypes osType);

        /// <summary>
        /// Resolves the IP address of a named OS lake instance.
        /// </summary>
        public abstract string GetOSLakeIp(string lakeName);
    }
}
