using System.Net;

namespace AzureCosmosDbRepositoryLib.Contracts
{
    public interface IResult<T>
    {
        string? ErrorMessage { get; set; }
        long ExecutionTimeInMs { get; set; }
    }
}
