using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Ports.OSPorts;
namespace OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure
{
    public static class WindowsInfrastructureServiceInjector
    {
        public static void AddWindowsInfrastructureServices(this IServiceCollection services)
        {
            services.AddTransient<OSProcessManagementObject, WindowsProcessManagement>();
            services.AddTransient<OSProcessDiagnosticManager, WindowsProcessDiagnosticsManager>();
        }
    }
}
