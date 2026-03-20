using Domain.Entities.Implementations.Linux;
using Domain.Ports.OSPorts;
using Grpc.Core;
using GrpcApplyServer;

namespace OSProcessManagerInfastructure.Grpc
{
    public class ProcessServiceImpl : ProcessService.ProcessServiceBase
    {
        private readonly OSProcessDiagnosticManager _diagnosticsManager;

        public ProcessServiceImpl(OSProcessDiagnosticManager diagnosticsManager)
        {
            _diagnosticsManager = diagnosticsManager;
        }

        public override Task<GetDiagnosticsResponse> GetProcessDiagnostics(
            GetDiagnosticsRequest request,
            ServerCallContext context)
        {
            var pid = ReadUserProcessPid();
            var diagnostics = _diagnosticsManager.GetProcessDiagnosticsByProcessId(pid);

            var response = new GetDiagnosticsResponse();
            foreach (var diag in diagnostics)
            {
                if (diag is LinuxProcessDiagnostics linuxDiag)
                {
                    foreach (var (key, value) in linuxDiag.ProcessProps)
                        response.Entries.Add(new ProcessDiagnosticEntry { Key = key, Value = value, Category = "process" });
                    foreach (var (key, value) in linuxDiag.SystemProps)
                        response.Entries.Add(new ProcessDiagnosticEntry { Key = key, Value = value, Category = "system" });
                }
            }

            return Task.FromResult(response);
        }

        public override async Task StreamProcessDiagnostics(
            GetDiagnosticsRequest request,
            IServerStreamWriter<DiagnosticsEvent> responseStream,
            ServerCallContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var pid = ReadUserProcessPid();
                var diagnostics = _diagnosticsManager.GetProcessDiagnosticsByProcessId(pid);

                foreach (var diag in diagnostics)
                {
                    if (diag is LinuxProcessDiagnostics linuxDiag)
                    {
                        foreach (var (key, value) in linuxDiag.ProcessProps)
                        {
                            await responseStream.WriteAsync(new DiagnosticsEvent
                            {
                                Entry = new ProcessDiagnosticEntry { Key = key, Value = value, Category = "process" },
                                Timestamp = DateTimeOffset.UtcNow.ToString("o")
                            });
                        }
                    }
                }

                await Task.Delay(1000, context.CancellationToken);
            }
        }

        private static int ReadUserProcessPid()
        {
            var pidFile = "/tmp/user-process.pid";
            if (!File.Exists(pidFile))
                throw new RpcException(new Status(StatusCode.Unavailable, "User process PID file not found"));
            return int.Parse(File.ReadAllText(pidFile).Trim());
        }
    }
}
