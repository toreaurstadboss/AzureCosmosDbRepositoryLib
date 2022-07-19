using AzureCosmosDbRepositoryLib;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AcceptanceTests;

public class CosmosDbTestsFixture : IClassFixture<CosmosDbTests>
{
#pragma warning disable IDE0060 // Remove unused parameter
    public CosmosDbTestsFixture(CosmosDbTests data)
#pragma warning restore IDE0060 // Remove unused parameter
    {

    }
}

public class CosmosDbTests : IDisposable
{
    //Guide : to run these tests, first run :
    //dotnet user-secrets init
    //dotnet set "ConnectionStrings:AzureCosmosDbConnString" "<connection-string-found-inside-keys-on-azure-portal-page-for-your-cosmosdb-with-container(s)>"
    //dotnet user-secrets list

    //Afterwards, you should be able to run these tests using YOUR user secrets ! (connection to your database and container(s) !
    //This makes it possible to publish and push the source code to Github without revealing user secrets 

    private IRepository? _repository;

    private readonly string? _connectionString;
    private readonly IConfiguration? _configuration;
    private readonly ITestOutputHelper _output; 
        

    public CosmosDbTests(ITestOutputHelper output)
    {
        if (_repository == null)
        {
            _configuration = new ConfigurationBuilder().AddUserSecrets<CosmosDbTests>().Build();
            _connectionString = _configuration.GetConnectionString("AzureCosmosDbConnString");
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new ArgumentException("Need to have a connection string. This should be done via dotnet user secret! Run dotnet user-secrets init, then set the connection string. Read the guide in comments of this unit test!");
            }
            _repository = new Repository("ToDoList", "Container1",
                connectionString: _connectionString);
        }

        _output = output; 

    }

    [Fact]
    public void AddAndRemoveItemFromContainer_Succeeds()
    {
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = $"Create an item at the following time: {DateTime.UtcNow}"
        };
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        ItemResponse<TodoListItem> response = Task.Run(async ()  =>  await _repository.Add(todoItem, id: todoItem.Id)).Result;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        string? resultText = $"Acceptance test passed. Could create a new item in remote Azure Cosmos DB container. DB: {_repository?.GetDatabaseName()} ContainerId: {_repository?.GetContainerId()}";

        _output.WriteLine(resultText);

        //TODO: implement clean up (deleting item) 
    }

    [Fact]
    public void AddItemsAndRemoveItemsFromContainer_Succeeds()
    {
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 110,
            Task = $"Create first item of batch at the following time: {DateTime.UtcNow}"
        };
        var anotherTodoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 120,
            Task = $"Create second item of batch at the following time: {DateTime.UtcNow}"
        };

        var todoList = new Dictionary<PartitionKey, TodoListItem> {
            { new PartitionKey(todoItem.Id), todoItem },
            { new PartitionKey(anotherTodoItem.Id), anotherTodoItem }
        };

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        IList<ItemResponse<TodoListItem>>? responses = Task.Run(async () => await _repository.AddRange(todoList)).Result;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        responses?.All(r => r.StatusCode == System.Net.HttpStatusCode.Created).Should().BeTrue();
        string? resultText = $"Acceptance test passed. Could create a set of two new items in remote Azure Cosmos DB container. DB: {_repository?.GetDatabaseName()} ContainerId: {_repository?.GetContainerId()} Partition keys used: {string.Join(",", todoList.Select(t => t.Value?.Id?.ToString()))}";
        _output.WriteLine(resultText);
        //todo implement clean up (deleting items)
    }

    public void Dispose()
    {
        if (_repository != null)
        {
            _repository.Dispose(); //will trigger disposing the repository and which will then release the client
            _repository = null; 
        }
        GC.SuppressFinalize(this);
    }
}