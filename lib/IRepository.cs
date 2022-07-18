using Microsoft.Azure.Cosmos;

namespace AzureCosmosDbRepositoryLib;
public interface IRepository
{

    Task<ItemResponse<T>> Add<T>(T item, PartitionKey? partitionKey = null, object? id = null);

    T AddOrUpdate<T>(T item, object partitionkey);

    void Dispose();

    string? GetDatabaseName();

    string? GetContainerId(); 

}
