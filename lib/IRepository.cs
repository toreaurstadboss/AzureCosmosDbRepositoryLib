using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;

namespace AzureCosmosDbRepositoryLib;


/// <summary>
/// Repository pattern for Azure Cosmos DB
/// </summary>
public interface IRepository<T> where T : IStorableEntity
{

    /// <summary>
    /// Adds an item to container in DB. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <returns></returns>
    Task<ISingleResult<T>?> Add(T item);

    /// <summary>
    /// Retrieves an item to container in DB. Param <paramref name="partitionKey"/> and param <paramref name="id"/> should be provided.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="partitionKey"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ISingleResult<T>?> Get(object? id = null, PartitionKey? partitionKey = null);

    /// <summary>
    /// Searches for a matching items by predicate (where condition) given in <paramref name="searchRequest"/>.
    /// </summary>
    /// <param name="searchRequest"></param>
    /// <returns></returns>
    Task<ICollectionResult<T>?> Find(ISearchRequest<T> searchRequest);

    /// <summary>
    /// Searches for a matching items by predicate (where condition) given in <paramref name="searchRequest"/>.
    /// </summary>
    /// <param name="searchRequest"></param>
    /// <returns></returns>
    Task<ISingleResult<T>?> FindOne(ISearchRequest<T> searchRequest);

    /// <summary>
    /// Removes an item from container in DB. Param <paramref name="partitionKey"/> and param <paramref name="id"/> should be provided.
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

    /// <summary>
    /// Adds or updates an item via 'Upsert' method in container in DB. 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    Task<ISingleResult<T>?> AddOrUpdate(T item);

    /// <summary>
    /// Retrieves results paginated of page size. Looks at all items of type <typeparamref name="T"/> in the container. Send in a null value for continuationToken in first request and then use subsequent returned continuation tokens to 'sweep through' the paged data divided by <paramref name="pageSize"/>.
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="continuationToken"></param>
    /// <param name="sortDescending">If true, sorting descending (sorting via LastUpdate property so newest items shows first)</param>
    /// <returns></returns>
    Task<IPaginatedResult<T>?> GetPaginatedResult(int pageSize, string? continuationToken = null, bool sortDescending = false);

    /// <summary>
    /// On demand method exposed from exposing this respository on demands. Frees up resources such as CosmosClient object inside.
    /// </summary>
    void Dispose();

    /// <summary>
    /// Returns name of database in Azure Cosmos DB
    /// </summary>
    /// <returns></returns>
    string? GetDatabaseName();

    /// <summary>
    /// Returns Container id inside database in Azure Cosmos DB
    /// </summary>
    /// <returns></returns>
    string? GetContainerId(); 

}
