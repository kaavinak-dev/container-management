using DeploymentManager.Implementations;
using OperatingSystemHelpers.Implementations.Windows;

// AppContext.BaseDirectory = DeploymentManager/bin/Debug/net8.0/ — go up 4 levels to solution root
var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

if (args.Length == 0)
{
    Console.WriteLine("Usage: DeploymentManager <os>");
    Console.WriteLine("  1 = Windows (win-x64)");
    Console.WriteLine("  2 = Linux   (linux-x64)");
    return;
}

var communicator = new WindowsProcessCommunicator();

switch (args[0])
{
    case "1":
        Console.WriteLine("Building os-process-manager for Windows (win-x64)...");
        new WindowsNativeExecutableBuilder(communicator, solutionRoot).BuildExecutable();
        break;
    case "2":
        Console.WriteLine("Building os-process-manager for Linux (linux-x64)...");
        new LinuxNativeExecutableBuilder(communicator, solutionRoot).BuildExecutable();
        break;
    default:
        Console.WriteLine($"Unknown OS argument '{args[0]}'. Use 1 = Windows, 2 = Linux.");
        break;
}
