using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Abstractions;

namespace Domain.Entities.Implementations.Windows
{
    public static class WindowsDomainServiceInjector
    {
        public static void AddWindowsDomainServices(this IServiceCollection services)
        {
            services.AddTransient<OSProcessFactory, WindowsProcessFactory>();
            services.AddTransient<OSProcessDiagnosticsFactory, WindowsProcessDiagnosticsFactory>();
        }

    }
}
