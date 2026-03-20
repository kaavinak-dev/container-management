using Engines.FileStorageEngines.Recipes;

namespace Engines.FileStorageEngines.ContainerBuild
{
    public static class DockerfileTemplateRenderer
    {
        private const string Template =
@"FROM {BaseImage}

# === User project ===
WORKDIR /project
COPY ./user-project .
{BuildStep}

# === Sidecar ===
WORKDIR /sidecar
COPY ./sidecar .

# === Supervisor ===
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT [""/entrypoint.sh"", ""{StartCommand}""]";

        public static string Render(IProjectContainerRecipe recipe) =>
            Template
                .Replace("{BaseImage}", recipe.BaseImage)
                .Replace("{BuildStep}", string.IsNullOrEmpty(recipe.BuildStep) ? "" : recipe.BuildStep)
                .Replace("{StartCommand}", recipe.StartCommand);
    }
}
