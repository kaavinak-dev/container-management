using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Domain.Entities.Implementations.Windows;
using OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure.NativeStructs;
using Domain.Ports.OSPorts;

namespace OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure
{

    public class WindowsJobQueryObject : OSJobQueryObject
    {
        internal readonly  string jobQueryObjectBufferStr;

        public WindowsJobQueryObject(nint queryBuffer)
        {
            jobQueryObjectBufferStr = queryBuffer.ToString();
        }

        public override List<long> GetAllProcessIdsInJob()
        {
            nint buffer = (nint)int.Parse(jobQueryObjectBufferStr);
            try
            {
                IntPtr pProcessIdList = IntPtr.Add(buffer, 8);
                List<long> processIds = new();
                for (int i = 0; i < numberOfProcessesInJob; i++)
                {
                    long processId = Marshal.ReadIntPtr(pProcessIdList, i * IntPtr.Size).ToInt64();
                    processIds.Add(processId);

                }
                return processIds;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

    }


    public class WindowsJobManager : OSJobManager
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll")]
        static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll")]
        static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryInformationJobObject(
            IntPtr hJob,
            int JobObjectInformationClass,
            IntPtr lpJobObjectInfo,
            int cbJobObjectInfoLength,
            out int lpReturnLength);


        private const int JobObjectBasicProcessIdList = 3;



        const int JobObjectExtendedLimitInformation = 9;
        const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        
        public WindowsJobManager(OSProcessFactory processFactory,OSJobFactory jobFactory) : base(processFactory, jobFactory) { }

        public override OSJob CreateJobInOS(string jobName)
        {
            var jobSecurity = JobNativeStructs.GetDefaultJobSecurityAttribute();
            IntPtr job = CreateJobObject(jobSecurity, "containerManagement-"+jobName);
            JobNativeStructs.JOBOBJECT_EXTENDED_LIMIT_INFORMATION info = new JobNativeStructs.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            int length = Marshal.SizeOf(typeof(JobNativeStructs.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr infoPtr = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(info, infoPtr, false);

            SetInformationJobObject(job, JobObjectExtendedLimitInformation, infoPtr, (uint)length);
            string jobHandleStr = job.ToString();
            OSJob jobCreated= jobFactory.CreateJob(jobHandleStr,jobName);
            OSProcess processCreated=AddProcessToJob(jobCreated, new WindowsProcessManager());
            return jobCreated;
        }

        public override OSProcess AddProcessToJob(OSJob job,OSProcessManager processManager)
        {

            WindowsProcess processCreated = (WindowsProcess)processManager.CreateProcessInOS();
            // 3. Assign process to job object
            if (!AssignProcessToJobObject((nint)int.Parse(job.JobHandle), (nint)int.Parse(processCreated.ProcessHandle)))
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"Failed to assign process to job object. Error: {error}");
                return new WindowsProcess()
                {
                    ProcessId="",
                    JobHandle=""
                };
            }

            Console.WriteLine("Process successfully assigned to job object.");

            return processCreated;       
                
        }

        public override List<OSProcess> GetProcessesInJob(OSJob job)
        {
            var jobQueryInfo = (WindowsJobQueryObject)QueryJobInformation(job);
            var processIds = jobQueryInfo.GetAllProcessIdsInJob();
            var processes = processIds.Select((processId) =>
            {
                var process = (processFactory.CreateProcess(processId.ToString())) as WindowsProcess;
                if (process == null) { return null; }
                process.JobHandle = job.JobHandle;
                return process;
            }).Where((process) =>
            {
                if (process != null) return true;
                return false;
            }).Select((process) => {
                OSProcess genericProcess = process;
                return genericProcess;
            
            }).ToList() ?? new();
            return processes;
        }

        public override OSJobQueryObject QueryJobInformation(OSJob job)
        {
            int lengthNeeded;
            int bufferLength = 1024; // start with 1 KB buffer
            IntPtr buffer = Marshal.AllocHGlobal(bufferLength);
            nint jobHandlePtr = (nint)int.Parse(job.JobHandle);
            try
            {
                if (!QueryInformationJobObject(jobHandlePtr, JobObjectBasicProcessIdList, buffer, bufferLength, out lengthNeeded))
                {
                    // If buffer is too small, reallocate and try again
                    if (Marshal.GetLastWin32Error() == 122) // ERROR_INSUFFICIENT_BUFFER
                    {
                        Marshal.FreeHGlobal(buffer);
                        buffer = Marshal.AllocHGlobal(lengthNeeded);

                        if (!QueryInformationJobObject(jobHandlePtr, JobObjectBasicProcessIdList, buffer, lengthNeeded, out lengthNeeded))
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                }
                WindowsJobQueryObject queryObject = new WindowsJobQueryObject(buffer);
                queryObject.Job = job;
                queryObject.numberOfProcessesInJob = Marshal.ReadInt32(buffer, 4);
                return queryObject;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            
        }
    }
}
