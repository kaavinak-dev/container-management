using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

namespace Engines.FileStorageEngines.ContainerBuild
{
    public class ContainerContextAssembler
    {
        private const string EntrypointScript =
@"#!/bin/sh
$@ &
USER_PID=$!
echo $USER_PID > /tmp/user-process.pid
/sidecar/os-process-manager-service
kill $USER_PID";

        // Returns a TAR stream consumable by the Docker daemon's POST /build endpoint.
        // Layout inside the TAR:
        //   Dockerfile         ← rendered from recipe template
        //   entrypoint.sh      ← supervisor script (executable bit set)
        //   user-project/      ← repacked from the MinIO .tar.gz artifact
        //   sidecar/           ← entire dotnet publish output directory
        public async Task<Stream> BuildContextTar(
            Stream projectArtifactStream,
            string renderedDockerfile,
            string sidecarPublishDir)
        {
            var output = new MemoryStream();
            await using var tarWriter = new TarWriter(output, TarEntryFormat.Pax, leaveOpen: true);

            await AddStringEntry(tarWriter, "Dockerfile", renderedDockerfile);
            await AddStringEntry(tarWriter, "entrypoint.sh", EntrypointScript,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

            // Repack user project from MinIO .tar.gz into user-project/ prefix
            projectArtifactStream.Position = 0;
            await using var gzip = new GZipStream(projectArtifactStream, CompressionMode.Decompress, leaveOpen: true);
            using var innerTar = new TarReader(gzip, leaveOpen: true);
            TarEntry? entry;
            while ((entry = await innerTar.GetNextEntryAsync()) != null)
            {
                var destName = "user-project/" + entry.Name.TrimStart('/');
                if (entry.EntryType == TarEntryType.Directory)
                {
                    await tarWriter.WriteEntryAsync(new PaxTarEntry(TarEntryType.Directory, destName));
                }
                else if (entry.EntryType == TarEntryType.RegularFile && entry.DataStream != null)
                {
                    var fileEntry = new PaxTarEntry(TarEntryType.RegularFile, destName);
                    var ms = new MemoryStream();
                    await entry.DataStream.CopyToAsync(ms);
                    ms.Position = 0;
                    fileEntry.DataStream = ms;
                    await tarWriter.WriteEntryAsync(fileEntry);
                }
            }

            // Add entire sidecar publish directory — mirrors how the sidecar Dockerfile does COPY . .
            if (!Directory.Exists(sidecarPublishDir))
                throw new DirectoryNotFoundException(
                    $"Sidecar publish directory not found: '{sidecarPublishDir}'. " +
                    "Run 'dotnet publish' on the os-process-manager-service project first, " +
                    "then set SidecarPublishDir in appsettings.json to the publish output path.");

            foreach (var file in Directory.EnumerateFiles(sidecarPublishDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sidecarPublishDir, file).Replace('\\', '/');
                await tarWriter.WriteEntryAsync(file, "sidecar/" + relative);
            }

            output.Position = 0;
            return output;
        }

        private static async Task AddStringEntry(TarWriter writer, string name, string content, UnixFileMode mode =
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead)
        {
            var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
            {
                DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)),
                Mode = mode
            };
            await writer.WriteEntryAsync(entry);
        }
    }
}
