using AzureCosmosDbRepositoryLib;
using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace AcceptanceTests;

public class CosmosDbTestsFixture : IClassFixture<CosmosDbTests>
{

    public CosmosDbTestsFixture(CosmosDbTests data)
    {
        if (data != null)
        {

        }

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

    private IRepository<TodoListItem>? _repository;

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
            _repository = new Repository<TodoListItem>("ToDoList", "Container1",
                connectionString: _connectionString);
        }

        _output = output;

    }

    [Fact]
    public void AddThreeAndGetPagingatedResultsAndDelete_Succeeds()
    {
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = $"Create an item at the following time: {DateTime.UtcNow}",
            Timing = new TodoListItem.Schedule
            {
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(10)
            }
        };
        var anotherTodoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = $"Create another item at the following time: {DateTime.UtcNow}",
            Timing = new TodoListItem.Schedule
            {
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(10)
            }
        };
        var yetAnotherTodoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = $"Create yet another item at the following time: {DateTime.UtcNow}",
            Timing = new TodoListItem.Schedule
            {
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(10)
            }
        };
        ISingleResult<TodoListItem>? response = Task.Run(async () => await _repository!.Add(todoItem)).Result;
        response = Task.Run(async () => await _repository!.Add(anotherTodoItem)).Result;
        response = Task.Run(async () => await _repository!.Add(yetAnotherTodoItem)).Result;


        IPaginatedResult<TodoListItem>? paginatedResultFirstPage = Task.Run(async () => await _repository!.GetPaginatedResult(1, continuationToken: null, sortDescending: true)).Result;
        IPaginatedResult<TodoListItem>? paginatedResultSecondPage = Task.Run(async () => await _repository!.GetPaginatedResult(1, continuationToken: paginatedResultFirstPage!.ContinuationToken, sortDescending: true)).Result;
        IPaginatedResult<TodoListItem>? paginatedResultThirdPage = Task.Run(async () => await _repository!.GetPaginatedResult(1, continuationToken: paginatedResultSecondPage!.ContinuationToken, sortDescending: true)).Result;

        paginatedResultFirstPage!.Items.Should().NotBeEmpty();
        paginatedResultSecondPage!.Items.Should().NotBeEmpty();
        paginatedResultThirdPage!.Items.Should().NotBeEmpty(); //for now we just check if we got a non-empty page content here - which seems to work ok

    }

    [Fact]
    public void AddAndRemoveItemFromContainer_Succeeds()
    {
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = $"Create an item at the following time: {DateTime.UtcNow}",
            Timing = new TodoListItem.Schedule
            {
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(10)
            }
        };
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        ISingleResult<TodoListItem>? response = Task.Run(async () => await _repository.Add(todoItem)).Result;

#pragma warning restore CS8602 // Dereference of a possibly null reference.
        response?.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        string? resultText = $"Acceptance test passed. Could create a new item in remote Azure Cosmos DB container. DB: {_repository?.GetDatabaseName()} ContainerId: {_repository?.GetContainerId()}";

        _output.WriteLine(resultText);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var responseDeletion = Task.Run(async () => await _repository.Remove(todoItem.Id));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        responseDeletion!.Result!.StatusCode?.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public void AddGetDeleteItemFromContainer_Succeeds()
    {
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = "Create an item at the following time: " + $"{DateTime.UtcNow}"
        };


        ISingleResult<TodoListItem>? response = Task.Run(async () => await _repository!.Add(todoItem)).Result;

        response!.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        string? resultText = $"Acceptance test passed. Could create a new item in remote Azure Cosmos DB container. DB: {_repository?.GetDatabaseName()} ContainerId: {_repository?.GetContainerId()}";

        _output.WriteLine(resultText);

        //try getting the item too 

        var item = Task.Run(async () => await _repository!.Get(todoItem.Id)).Result;
        item!.Item!.Id.Should().Be(todoItem.Id);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var responseDeletion = Task.Run(async () => await _repository.Remove(todoItem.Id));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        responseDeletion!.Result!.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public void AddUpdateGetDeleteItemFromContainer_Succeeds()
    {
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 400,
            Task = "Create item at the following time: " + $"{DateTime.UtcNow}. Will be updated"
        };


        ISingleResult<TodoListItem>? response = Task.Run(async () => await _repository!.Add(todoItem)).Result;

        response!.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        string? resultText = $"Acceptance test passed. Could create a new item in remote Azure Cosmos DB container. DB: {_repository?.GetDatabaseName()} ContainerId: {_repository?.GetContainerId()}";

        _output.WriteLine(resultText);

        todoItem.Task = "Updated task contents: " + $"{DateTime.UtcNow}"; 
        ISingleResult<TodoListItem>? responseUpdate = Task.Run(async () => await _repository!.AddOrUpdate(todoItem)).Result;


        //try getting the item too after updating it 

        var item = Task.Run(async () => await _repository!.Get(todoItem.Id)).Result;
        item!.Item!.Id.Should().Be(todoItem.Id);

        item!.Item!.Task.Should().Be(todoItem.Task); 

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var responseDeletion = Task.Run(async () => await _repository.Remove(todoItem.Id));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        responseDeletion!.Result!.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public void AddFindDeleteFromContainer_Succeeds()
    {
        string pattern = "LOOK FOR THIS ITEM ###: "; 
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = pattern + $"{DateTime.UtcNow}"
        };

        ISingleResult<TodoListItem>? response = Task.Run(async () => await _repository!.Add(todoItem)).Result;

        var anotherTodoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = pattern + $"{DateTime.UtcNow}"
        };

        ISingleResult<TodoListItem>? responseSecond = Task.Run(async () => await _repository!.Add(anotherTodoItem)).Result;

        response!.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        responseSecond!.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        //try finding the items too 

        var searchRequest = new SearchRequest<TodoListItem>
        {
            Filter = f => f.Task != null && f.Task.Contains(pattern) == true
        };

        ICollectionResult<TodoListItem>? items = Task.Run(async () => await _repository!.Find(searchRequest)).Result;
        var itemsAllMatch = items!.Items.All(x => x.Task!.StartsWith(pattern));
        itemsAllMatch.Should().BeTrue(); 

        var responseDeletion = Task.Run(async () => await _repository!.Remove(todoItem.Id));
        responseDeletion!.Result!.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var responseDeletionSecond = Task.Run(async () => await _repository!.Remove(anotherTodoItem.Id));
        responseDeletionSecond!.Result!.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public void AddFindOneDeleteFromContainer_Succeeds()
    {
        string pattern = "LOOK FOR THIS ITEM ###: ";
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = pattern + $"{DateTime.UtcNow}"
        };

        ISingleResult<TodoListItem>? response = Task.Run(async () => await _repository!.Add(todoItem)).Result;
        response!.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);


        //try finding the items too 

        var searchRequest = new SearchRequest<TodoListItem>
        {
            Filter = f => f.Task != null && f.Task.Contains(pattern) == true
        };

        ISingleResult<TodoListItem>? item = Task.Run(async () => await _repository!.FindOne(searchRequest)).Result;
        var itemAllMatch = item!.Item!.Task!.StartsWith(pattern);
        itemAllMatch.Should().BeTrue();

        var responseDeletion = Task.Run(async () => await _repository!.Remove(todoItem.Id));
        responseDeletion!.Result!.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
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
        ICollectionResult<TodoListItem>? responses = Task.Run(async () => await _repository.AddRange(todoList)).Result;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        responses!.StatusCodes.All(r => r == System.Net.HttpStatusCode.Created).Should().BeTrue();
        string? resultText = $"Acceptance test passed. Could create a set of two new items in remote Azure Cosmos DB container. DB: {_repository?.GetDatabaseName()} ContainerId: {_repository?.GetContainerId()} Partition keys used: {string.Join(",", todoList.Select(t => t.Value?.Id?.ToString()))}";
        _output.WriteLine(resultText);
        //todo implement clean up (deleting items)


#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var responseDeletion = Task.Run(async () => await _repository.Remove(todoItem.Id));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        responseDeletion!.Result!.StatusCode!.Should().Be(System.Net.HttpStatusCode.NoContent);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        responseDeletion = Task.Run(async () => await _repository.Remove(anotherTodoItem.Id));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        responseDeletion!.Result!.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

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

