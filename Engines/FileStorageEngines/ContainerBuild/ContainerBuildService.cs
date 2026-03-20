using Docker.DotNet;
using Docker.DotNet.Models;
using Engines.FileStorageEngines.Recipes;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;

namespace Engines.FileStorageEngines.ContainerBuild
{
    public class ContainerBuildService
    {
        private readonly IDockerClientFactory _dockerClientFactory;
        private readonly string _sidecarPublishDir;

        public ContainerBuildService(IDockerClientFactory dockerClientFactory, string sidecarPublishDir)
        {
            _dockerClientFactory = dockerClientFactory;
            _sidecarPublishDir = sidecarPublishDir;
        }

        // Builds a project-specific Docker image (with sidecar) and starts a container from it.
        // Returns the image name that was built.
        public async Task<string> BuildAndStartProjectContainer(
            Stream projectArtifactStream,
            IProjectContainerRecipe recipe,
            string projectId,
            OSLakeTechTypes techType = OSLakeTechTypes.DockerMachine,
            OSLakeTypes osType = OSLakeTypes.Linux)
        {
            var dockerClient = _dockerClientFactory.CreateForLake(techType, osType);

            var dockerfile = DockerfileTemplateRenderer.Render(recipe);
            var assembler = new ContainerContextAssembler();
            projectArtifactStream.Position = 0;
            var contextTar = await assembler.BuildContextTar(projectArtifactStream, dockerfile, _sidecarPublishDir);

            var imageName = $"project-{projectId}:latest";
            var buildParams = new ImageBuildParameters
            {
                Tags = new List<string> { imageName }
            };

            // POST /build to Docker daemon — equivalent to ArtifactDeploymentThroughIP's curl approach
            using var buildResponse = await dockerClient.Images.BuildImageFromDockerfileAsync(
                contextTar, buildParams, CancellationToken.None);
            // Drain the response stream so the build completes before we proceed
            using var reader = new StreamReader(buildResponse);
            while (!reader.EndOfStream)
                await reader.ReadLineAsync();

            var createResponse = await dockerClient.Containers.CreateContainerAsync(
                new CreateContainerParameters
                {
                    Image = imageName,
                    Name = $"project-{projectId}"
                });

            var started = await dockerClient.Containers.StartContainerAsync(
                createResponse.ID, new ContainerStartParameters());

            if (!started)
                throw new Exception($"Container for project '{projectId}' failed to start");

            return imageName;
        }
    }
}
