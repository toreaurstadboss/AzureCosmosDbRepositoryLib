using System.Net;

namespace AzureCosmosDbRepositoryLib.Contracts
{
   
    public interface ISingleResult<T> : IResult<T>
    {
        T? Item { get; set; }
        HttpStatusCode? StatusCode { get; set; }
    }

    public class SingleResult<T> : ISingleResult<T>
    {
        public T? Item { get; set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeInMs { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public double RequestCharge { get; set; }
    }

}
