using Domain.Ports.OSPorts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSProcessManagerInfastructure.OSInfrastructure.LinuxInfrastructure
{
    public static class LinuxInfrastructureServiceInjector
    {
        public static void AddLinuxInfrastructureServices(this IServiceCollection services)
        {
            services.AddTransient<OSProcessManagementObject, LinuxProcessManagement>();
            services.AddTransient<OSProcessDiagnosticManager, LinuxProcessDiagnosticsManager>();
        }
    }
}
