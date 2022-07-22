using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace AcceptanceTests; 

public class TodoListItem : IStorableEntity
{

    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    public string? Task { get; set; }

    public int Priority { get; set; }

    public PartitionKey? PartitionKey => new PartitionKey(Id); 

    public override string ToString()
    {
        return $"Id: {Id} Task: {Task} Priority: {Priority}";
    }

}