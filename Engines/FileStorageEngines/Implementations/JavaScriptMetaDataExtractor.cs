using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Engines.FileStorageEngines.Abstractions;
using PeNet;
using PeNet.Header.Pe;

namespace Engines.FileStorageEngines.Implementations
{


    public class ArchitectureInfo
    {
        public string Architecture { get; set; }
        //     public bool Is64Bit { get; set; }
        public string Subsystem { get; set; }
        public string MinimumOS { get; set; }
        //public bool HasASLR { get; set; }
        //public bool HasDEP { get; set; }
        //public bool IsLargeAddressAware { get; set; }
    }


    public class JavaScriptFileMetaDataExtractor : FileMetaDataExtractor
    {
        public JavaScriptFileMetaDataExtractor()
        {


        }



        public ArchitectureInfo ExtractArchitectureInfo(string filePath)
        {
            var peFile = new PeFile(filePath);
            var info = new ArchitectureInfo();

            // 1. Machine Type (Primary)
            var machine = peFile.ImageNtHeaders.FileHeader.Machine;
            info.Architecture = machine switch
            {
                MachineType.I386 => "x86 (32-bit)",
                MachineType.Amd64 => "x64 (64-bit)",
                MachineType.Arm => "ARM (32-bit)",
                MachineType.Arm64 => "ARM64 (64-bit)",
                MachineType.Ia64 => "IA64 (Itanium)",
                _ => "Unknown"
            };

            // 2. Is 64-bit
            //info.Is64Bit = peFile.Is64Bit;

            // 3. Subsystem
            var subsystem = peFile.ImageNtHeaders.OptionalHeader.Subsystem;
            info.Subsystem = subsystem switch
            {
                SubsystemType.WindowsGui => "GUI Application",
                SubsystemType.WindowsCui => "Console Application",
                SubsystemType.Native => "Native Driver",
                _ => "Other"
            };

            // 4. Minimum OS
            var majorOS = peFile.ImageNtHeaders.OptionalHeader.MajorOperatingSystemVersion;
            var minorOS = peFile.ImageNtHeaders.OptionalHeader.MinorOperatingSystemVersion;
            info.MinimumOS = $"{majorOS}.{minorOS}";

            // 5. Security Features

            return info;
        }

    }


}
