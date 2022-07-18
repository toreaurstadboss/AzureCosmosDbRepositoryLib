## AzureCosmosDbRepositoryLib

This lib is a helper lib for Azure Cosmos DB. It has been
tested against Microsoft Azure Cosmos DB version 3.29.

The lib implements a repository pattern to work against a 'container'
inside a DB. You can pass in class instances and perform different CRUD
operations. 

Please note, for this repository to work, you must provide the connection string
and database name and container id. It is also suggested to provide the 
correct partition key. It will default to '/id'. Also note that operations against a 
container which adds, updates and deletes item(s) requires either a partition key or 
object identifier to be passed in to correctly identify the row in the Azure Cosmos DB. 


## Examples 

### Adding a new item


```csharp

//we instantiate the repo like this: 

 _repository = new Repository("ToDoList", "Container1",
                connectionString: _connectionString);

//example of a test 
   [Fact]
    public void AddAndRemoveItemFromContainer_Succeeds()
    {
        var todoItem = new TodoListItem
        {
            Id = Guid.NewGuid().ToString(),
            Priority = 100,
            Task = $"Create an item at the following time: {DateTime.UtcNow}"
        };

        ItemResponse<TodoListItem> response = Task.Run(async ()  =>  await _repository.Add(todoItem, id: todoItem.Id)).Result;
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        string? resultText = $"Acceptance test passed. Could create a new item in remote Azure Cosmos DB container. DB: {_repository?.GetDatabaseName()} ContainerId: {_repository?.GetContainerId()}";

        _output.WriteLine(resultText);
    }

```

Methods in this lib are async, but can be run synchronously as shown in this example if this is required.
The connection string should be saved in a application settings json file for example. In
DEV environments, save the connection string to Azure Cosmos DB as a dotnet user secret for example. 


<hr />

Road map: 

Implement the methods shown in this repo for Entity Framework Core to have an equivalent repository pattern ! 

https://toreaurstad.blogspot.com/2022/06/repository-pattern-in-entity-framework.html

<hr />



Last update : 
19.07.2022 


Tore Aurstad 
