using Microsoft.Azure.Cosmos;

namespace AzureCosmosDbRepositoryLib;

/// <summary>
/// Implements a repository pattern for a container in Azure Cosmos DB
/// </summary>
public class Repository : IRepository, IDisposable 
{

    private readonly string? _databaseName;
    private readonly string? _containerId;
    private CosmosClient _client;
    private readonly Database _database;
    private readonly Container _container;
    private readonly CosmosClientOptions? _cosmosClientOptions;
    private readonly string? _partitionKeyPath;
    private string? _connectionString; 

    
    /// <summary>
    /// Generic repository for Azure Cosmos DB. Requires some of the parameters set here to function
    /// </summary>
    /// <param name="databaseName">Required.</param>
    /// <param name="containerId">Required.</param>
    /// <param name="clientOptions"></param>
    /// <param name="partitionKeyPath"></param>
    /// <param name="throughputPropertiesForDatabase"></param>
    /// <param name="connectionString">required</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public Repository(string databaseName, string containerId,
        CosmosClientOptions? clientOptions = null,
        string partitionKeyPath = "/id",
        ThroughputProperties? throughputPropertiesForDatabase = null,
        string? connectionString = null,
        bool defaultToUsingGateway = true)
    {
        if (connectionString == null)
        {
            throw new ArgumentException($"The connection string inside {nameof(connectionString)} must be non-null when passed into this repository"); 
        }
        _connectionString = connectionString; 
        _databaseName = databaseName;
        _containerId = containerId;
        _partitionKeyPath = partitionKeyPath; 
        _cosmosClientOptions = clientOptions;

        if (string.IsNullOrWhiteSpace(_databaseName) || string.IsNullOrWhiteSpace(_containerId))
        {
            throw new ArgumentException($"Must have both {nameof(databaseName)} and {nameof(containerId)} set before running any operations against these!"); 
        }


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
        _container =  Task.Run(async () =>  await _database.CreateContainerIfNotExistsAsync(_containerId, _partitionKeyPath)).Result;

    }


    public async Task<ItemResponse<T>> Add<T>(T item, PartitionKey? partitionKey = null, object? id = null)
    {

        ItemResponse<T>? response = null; 
        if (partitionKey != null)
        {
            response = await _container.CreateItemAsync(item, partitionKey); 
        }
        if (id != null)
        {
            response = await _container.CreateItemAsync(item, new PartitionKey(id.ToString())); 
        }
        if (response == null)
        {
            throw new ArgumentException("Adding the item threw an exception. To properly identify a row, either give a non-null partition key or a non-null id value! This is required to be able to later delete or update a row.");
        }
        return response; 
    }

    public T AddOrUpdate<T>(T item, object partitionkey)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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

    public string? GetDatabaseName()
    {
        return _database?.Id;
    }

    public string? GetContainerId()
    {
        return _container?.Id;
    }
}
