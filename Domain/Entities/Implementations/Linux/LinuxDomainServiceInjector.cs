using Domain.Entities.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Linux
{
    public static class LinuxDomainServiceInjector
    {
        public static void AddLinuxDomainServices(this IServiceCollection services)
        {
            services.AddTransient<OSProcessFactory, LinuxProcessFactory>();
            services.AddTransient<OSProcessDiagnosticsFactory, LinuxProcessDiagnosticsFactory>();
        }
    }
}
