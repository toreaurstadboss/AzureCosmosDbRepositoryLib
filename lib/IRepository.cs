using Microsoft.Azure.Cosmos;

namespace AzureCosmosDbRepositoryLib;


/// <summary>
/// Repository pattern for Azure Cosmos DB
/// </summary>
public interface IRepository
{

    /// <summary>
    /// Adds an item to container in DB. Param <paramref name="partitionKey"/> or param <paramref name="id"/> must be provided.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <param name="partitionKey"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ItemResponse<T>> Add<T>(T item, PartitionKey? partitionKey = null, object? id = null);

    /// <summary>
    /// Adds a set of items to container in DB. A shared partitionkey is used and the items are added inside a transaction as a single operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="partitionKey"></param>
    /// <returns></returns>
    Task<IList<ItemResponse<T>>?> AddRange<T>(IDictionary<PartitionKey, T> items);

    T AddOrUpdate<T>(T item, object partitionkey);

    void Dispose();

    string? GetDatabaseName();

    string? GetContainerId(); 

}
