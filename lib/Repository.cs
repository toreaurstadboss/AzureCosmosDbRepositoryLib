﻿using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;

namespace AzureCosmosDbRepositoryLib;

/// <summary>
/// Implements a repository pattern for a container in Azure Cosmos DB
/// </summary>
public class Repository<T> : BaseRepository<T>, IRepository<T>, IDisposable 
{
    //TODO: use partitionkeypath to infer how to build up partition keys implicitly
    //TODO: change implementation into a generic implementation 
    //TODO: move out some logic into helper class to automatically create database and container if needed.. (single responsobility)

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


    public async Task<ISingleResult<T>?> Add(T item, PartitionKey? partitionKey = null, object? id = null)
    {
        ISingleResult<T>? response = null; 
        if (partitionKey != null)
        {
            response = await SafeCallSingleItem(_container.CreateItemAsync(item, partitionKey)); 
        }
        if (id != null)
        {
            response = await SafeCallSingleItem(_container.CreateItemAsync(item, new PartitionKey(id.ToString()))); 
        }
        if (response == null)
        {
            throw new ArgumentException("Adding the item threw an exception. To properly identify a row, either give a non-null partition key or a non-null id value! This is required to be able to later delete or update a row.");
        }
        return response; 
    }

    public ISingleResult<T> AddOrUpdate(T item, object partitionkey)
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

    public async Task<ICollectionResult<T>?> AddRange(IDictionary<PartitionKey, T> items)
    {
        if (items == null || !items.Any())
            return null;

        var responses = new List<ISingleResult<T>>(); 
        foreach (var item in items)
        {
            var createdItem = await SafeCallSingleItem(_container.CreateItemAsync(item.Value, item.Key));
            responses.Add(createdItem); 
        }
        return BuildSearchResultCollection(responses); 
    }

    public async Task<ISingleResult<T>?> Remove(object? id = null, PartitionKey? partitionKey = null)
    {
        var partitionKeyResolved = partitionKey ?? GetDefaultPartitionKeyFromId(id);
        if (partitionKeyResolved == null)
            return null;
        
        if (partitionKeyResolved == null)
        {
            throw new ArgumentNullException(nameof(partitionKey)); 
        }
        var response = await SafeCallSingleItem(_container.DeleteItemAsync<T>(id!.ToString(), partitionKeyResolved.Value));
        return response;
    }

    public async Task<ISingleResult<T>?> Get(object? id = null, PartitionKey? partitionKey = null)
    {
        var partitionKeyResolved = partitionKey ?? GetDefaultPartitionKeyFromId(id);
        if (partitionKeyResolved == null)
            return null; 

        var item = await SafeCallSingleItem(_container.ReadItemAsync<T>(id?.ToString(), partitionKeyResolved.Value));
        return item;
    }

}
