using Microsoft.Azure.Cosmos;

namespace AzureCosmosDbRepositoryLib.Contracts
{
    public class IdWithPartitionKey
    {
        public object? Id { get; set; }
        public PartitionKey? PartitionKey { get; set; }
    }
}
