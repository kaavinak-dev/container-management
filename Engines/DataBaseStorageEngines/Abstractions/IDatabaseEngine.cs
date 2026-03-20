namespace Engines.DataBaseStorageEngines.Abstractions;

public interface IDatabaseEngine
{
    Task MigrateAsync();
    Task<bool> IsHealthyAsync();
}
