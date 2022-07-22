using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace AzureCosmosDbRepositoryLib.Contracts
{
    public interface IStorableEntity
    {
        [JsonProperty("id")]
        string Id { get; set; }

        PartitionKey? PartitionKey { get; }
    }
}
