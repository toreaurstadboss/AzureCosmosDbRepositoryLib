using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;

namespace AzureCosmosDbRepositoryLib;


/// <summary>
/// Repository pattern for Azure Cosmos DB
/// </summary>
public interface IRepository<T>
{

    /// <summary>
    /// Adds an item to container in DB. Param <paramref name="partitionKey"/> or param <paramref name="id"/> must be provided.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <param name="partitionKey"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ISingleResult<T>?> Add(T item);

    /// <summary>
    /// Adds an item to container in DB. Param <paramref name="partitionKey"/> or param <paramref name="id"/> must be provided.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <param name="partitionKey"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ISingleResult<T>?> Get(object? id = null, PartitionKey? partitionKey = null);

    /// <summary>
    /// Searches for a given item by given <paramref name="searchRequest"/>.
    /// </summary>
    /// <param name="searchRequest"></param>
    /// <returns></returns>
    Task<ICollectionResult<T>?> Find(ISearchRequest<T> searchRequest); 

    /// <summary>
    /// Removes an item from container in DB. Param <paramref name="partitionKey"/> and param <paramref name="id"/> must be provided.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <param name="partitionKey"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ISingleResult<T>?> Remove(object? id = null, PartitionKey? partitionKey = null);

    /// <summary>
    /// Adds a set of items to container in DB. A shared partitionkey is used and the items are added inside a transaction as a single operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="partitionKey"></param>
    /// <returns></returns>
    Task<ICollectionResult<T>?> AddRange(IDictionary<PartitionKey, T> items);

    Task<ISingleResult<T>?> AddOrUpdate(T item);

    void Dispose();

    string? GetDatabaseName();

    string? GetContainerId(); 

}
