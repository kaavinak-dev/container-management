namespace Engines.FileStorageEngines.Resources;

public enum ResourceType
{
    MySQL,
    Redis,
    MongoDB,
    PostgreSQL,
}

public record ResourceDefinition(
    ResourceType Type,
    string DisplayName,
    string Image,
    string DefaultAlias,
    int DefaultPort,
    string EnvVarPrefix,
    Dictionary<string, string> ContainerEnvVars,
    string? HelperSdkFileName);

public static class ResourceCatalog
{
    public static readonly IReadOnlyList<ResourceDefinition> SupportedResources = new List<ResourceDefinition>
    {
        new(
            Type: ResourceType.MySQL,
            DisplayName: "MySQL 8.0",
            Image: "mysql:8.0",
            DefaultAlias: "db",
            DefaultPort: 3306,
            EnvVarPrefix: "DB",
            ContainerEnvVars: new Dictionary<string, string>
            {
                ["MYSQL_ROOT_PASSWORD"] = "dev-secret",
                ["MYSQL_DATABASE"] = "app",
            },
            HelperSdkFileName: "mysqldb.js"),

        new(
            Type: ResourceType.Redis,
            DisplayName: "Redis 7",
            Image: "redis:7-alpine",
            DefaultAlias: "cache",
            DefaultPort: 6379,
            EnvVarPrefix: "REDIS",
            ContainerEnvVars: new Dictionary<string, string>(),
            HelperSdkFileName: "redis-helper.js"),

        new(
            Type: ResourceType.MongoDB,
            DisplayName: "MongoDB 7",
            Image: "mongo:7",
            DefaultAlias: "mongo",
            DefaultPort: 27017,
            EnvVarPrefix: "MONGO",
            ContainerEnvVars: new Dictionary<string, string>(),
            HelperSdkFileName: "mongodb-helper.js"),

        new(
            Type: ResourceType.PostgreSQL,
            DisplayName: "PostgreSQL 16",
            Image: "postgres:16-alpine",
            DefaultAlias: "pg",
            DefaultPort: 5432,
            EnvVarPrefix: "PG",
            ContainerEnvVars: new Dictionary<string, string>
            {
                ["POSTGRES_PASSWORD"] = "dev-secret",
                ["POSTGRES_DB"] = "app",
            },
            HelperSdkFileName: "pg-helper.js"),
    };

    public static ResourceDefinition GetDefinition(ResourceType type) =>
        SupportedResources.First(r => r.Type == type);
}
