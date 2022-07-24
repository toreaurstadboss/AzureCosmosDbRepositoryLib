using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AzureCosmosDbRepositoryLib;

/// <summary>
/// Implements a repository pattern for a container in Azure Cosmos DB
/// </summary>
public class Repository<T> : BaseRepository<T>, IRepository<T>, IDisposable where T : IStorableEntity
{
    //TODO: use partitionkeypath to infer how to build up partition keys implicitly
    //TODO: change implementation into a generic implementation 
    //TODO: move out some logic into helper class to automatically create database and container if needed.. (single responsobility)

    private readonly string? _databaseName;
    private readonly string? _containerId;
    private CosmosClient _client = null!;
    private Database _database = null!;
    private Container _container = null!;
    private readonly CosmosClientOptions? _cosmosClientOptions;
    private readonly string? _partitionKeyPath;
    private string? _connectionString;
    //private readonly int _bugdetResourceUnitsPerSecond;


    /// <summary>
    /// Generic repository for Azure Cosmos DB. Requires some of the parameters set here to function
    /// </summary>
    /// <param name="databaseName">Required.</param>
    /// <param name="containerId">Required.</param>
    /// <param name="clientOptions"></param>
    /// <param name="partitionKeyPath"></param>
    /// <param name="throughputPropertiesForDatabase"></param>
    /// <param name="connectionString">required</param>
    /// <param name="defaultToUsingGateway">Default using Gateway to avoid intranet firewall issues</param>
    /// <param name="bugdetResourceUnitsPerSecond">Default using resource units per second budget of 400 RU/s. Adjust this if desired. Will affect queries spending too much RU/s.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public Repository(string databaseName, string containerId,
        CosmosClientOptions? clientOptions = null,
        string partitionKeyPath = "/id",
        ThroughputProperties? throughputPropertiesForDatabase = null,
        string? connectionString = null,
        bool defaultToUsingGateway = true)
    //int bugdetResourceUnitsPerSecond = 400)
    {
        if (connectionString == null)
        {
            throw new ArgumentException($"The connection string inside {nameof(connectionString)} must be non-null when passed into this repository");
        }
        _connectionString = connectionString;
        //_bugdetResourceUnitsPerSecond = bugdetResourceUnitsPerSecond;
        _databaseName = databaseName;
        _containerId = containerId;
        _partitionKeyPath = partitionKeyPath;
        _cosmosClientOptions = clientOptions;

        if (string.IsNullOrWhiteSpace(_databaseName) || string.IsNullOrWhiteSpace(_containerId))
        {
            throw new ArgumentException($"Must have both {nameof(databaseName)} and {nameof(containerId)} set before running any operations against these!");
        }

        InitializeDatabaseAndContainer(clientOptions, throughputPropertiesForDatabase, defaultToUsingGateway);
    }


    public async Task<ISingleResult<T>?> Add(T item)
    {
        item.LastUpdate = DateTime.UtcNow;
        ISingleResult<T>? response = await SafeCallSingleItem(_container.CreateItemAsync(item, item.PartitionKey));
        return response;
    }

    public async Task<ISingleResult<T>?> AddOrUpdate(T item)
    {
        item.LastUpdate = DateTime.UtcNow;
        ISingleResult<T>? response = await SafeCallSingleItem(_container.UpsertItemAsync(item, item.PartitionKey));
        return response;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<ICollectionResult<T>?> AddRange(IDictionary<PartitionKey, T> items)
    {
        if (items == null || !items.Any())
            return null;

        var responses = new List<ISingleResult<T>>();
        foreach (var item in items)
        {
            item.Value.LastUpdate = DateTime.UtcNow;
            var createdItem = await SafeCallSingleItem(_container.CreateItemAsync(item.Value, item.Key));
            responses.Add(createdItem);
        }
        return BuildSearchResultCollection(responses);
    }

    public async Task<ISingleResult<T>?> Remove(IdWithPartitionKey id)
    {
        if (id == null || id.Id == null)
            return null; 

        var partitionKeyResolved = id.PartitionKey ?? GetDefaultPartitionKeyFromId(id.Id);
        if (partitionKeyResolved == null)
            return null;

        if (partitionKeyResolved == null)
        {
            throw new ArgumentNullException(nameof(id));
        }
        var response = await SafeCallSingleItem(_container.DeleteItemAsync<T>(id!.Id!.ToString(), partitionKeyResolved.Value));
        return response;
    }


    public async Task<ICollectionResult<T>?> RemoveRange(List<IdWithPartitionKey> ids)
    {
        if (ids == null || !ids.Any())
        {
            return await Task.FromResult<ICollectionResult<T>?>(null);
        }

        var deletedItems = new CollectionResult<T>();

        foreach (var id in ids)
        {
            ISingleResult<T>? deletedItem = await Remove(id);
            if (deletedItem != null && deletedItem.Item != null)
            {
                deletedItems.Items.Add(deletedItem!.Item);
                if (!string.IsNullOrWhiteSpace(deletedItem.ErrorMessage))
                {
                    deletedItems.ErrorMessage += deletedItem.ErrorMessage;
                }
                deletedItems.StatusCodes.Add(deletedItem.StatusCode);
            }
        }
        return deletedItems;

    }

    public async Task<ISingleResult<T>?> Get(IdWithPartitionKey id)
    {
        if (id == null || id.Id == null)
        {
            return null; 
        }
        var partitionKeyResolved = id.PartitionKey ?? GetDefaultPartitionKeyFromId(id.Id);
        if (partitionKeyResolved == null)
            return null;

        var item = await SafeCallSingleItem(_container.ReadItemAsync<T>(id?.Id?.ToString(), partitionKeyResolved.Value));
        return item;
    }

    public async Task<IPaginatedResult<T>?> GetPaginatedResult(int pageSize, string? continuationToken = null, bool sortDescending = false)
    {
        var query = new QueryDefinition($"SELECT * FROM c ORDER BY c.LastUpdate {(sortDescending ? "DESC" : "ASC")}".Trim()); //default query - will filter to type T via 'ItemQueryIterator<T>' 
        var queryRequestOptions = new QueryRequestOptions
        {
            MaxItemCount = pageSize
        };
        var queryResultSetIterator = _container.GetItemQueryIterator<T>(query, requestOptions: queryRequestOptions,
            continuationToken: continuationToken);
        var result = queryResultSetIterator.HasMoreResults ? await queryResultSetIterator.ReadNextAsync() : null;
        if (result == null)
            return null!;

        var sourceContinuationToken = result.ContinuationToken;
        var paginatedResult = new PaginatedResult<T>(sourceContinuationToken, result.Resource);
        return paginatedResult;

    }

    public async Task<ICollectionResult<T>?> Find(ISearchRequest<T>? searchRequest)
    {
        if (searchRequest?.Filter == null)
            return await Task.FromResult<ICollectionResult<T>?>(null);
        var linqQueryable = _container.GetItemLinqQueryable<T>();
        var stopWatch = Stopwatch.StartNew();
        try
        {
            using var feedIterator = linqQueryable.Where(searchRequest.Filter).ToFeedIterator();
            while (feedIterator.HasMoreResults)
            {
                var items = await feedIterator.ReadNextAsync();
                var result = BuildSearchResultCollection(items.Resource);
                result.ExecutionTimeInMs = stopWatch.ElapsedMilliseconds;
                return result;
            }
        }
        catch (Exception err)
        {
            return await Task.FromResult(BuildSearchResultCollection(err));
        }
        return await Task.FromResult<ICollectionResult<T>?>(null);
    }

    public async Task<ISingleResult<T>?> FindOne(ISearchRequest<T> searchRequest)
    {
        if (searchRequest?.Filter == null)
            return await Task.FromResult<ISingleResult<T>?>(null);
        var linqQueryable = _container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true);
        var stopWatch = Stopwatch.StartNew();
        try
        {
            var item = linqQueryable.Where(searchRequest.Filter).AsEnumerable().FirstOrDefault();
            if (item == null)
            {
                return await Task.FromResult<ISingleResult<T>?>(null);
            }
            var result = BuildSearchResult(item);
            result.ExecutionTimeInMs = stopWatch.ElapsedMilliseconds;
            return result;

        }
        catch (Exception err)
        {
            return await Task.FromResult(BuildSearchResult(err));
        }
    }

    public string? GetDatabaseName()
    {
        return _database?.Id;
    }

    public string? GetContainerId()
    {
        return _container?.Id;
    }

    #region Initialization 

    private void InitializeDatabaseAndContainer(CosmosClientOptions? clientOptions, ThroughputProperties? throughputPropertiesForDatabase, bool defaultToUsingGateway)
    {
        _client = clientOptions == null ?
            defaultToUsingGateway ?
            new CosmosClient(_connectionString, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway //this is the connection mode that works best in intranet-environments and should be considered as best compatible approach to avoid firewall issues
            }) :
            new CosmosClient(_connectionString) :
            new CosmosClient(_connectionString, _cosmosClientOptions);

        //Run initialization 
        if (throughputPropertiesForDatabase == null)
        {
            _database = Task.Run(async () => await _client.CreateDatabaseIfNotExistsAsync(_databaseName)).Result; //create the database if not existing (will go for default options regarding scaling)
        }
        else
        {
            _database = Task.Run(async () => await _client.CreateDatabaseIfNotExistsAsync(_databaseName, throughputPropertiesForDatabase)).Result; //create the database if not existing - specify specific through put options
        }

        // The container we will create.  
        _container = Task.Run(async () => await _database.CreateContainerIfNotExistsAsync(_containerId, _partitionKeyPath)).Result;
    }

    #endregion

    private void Dispose(bool isdisposing)
    {
        if (isdisposing)
        {
            if (_client != null)
            {
                _client.Dispose();
            }
            _client = null!;
            _connectionString = null!;
        }
    }

}
