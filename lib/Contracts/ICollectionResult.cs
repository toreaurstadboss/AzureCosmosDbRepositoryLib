using System.Net;

namespace AzureCosmosDbRepositoryLib.Contracts
{
    public interface ICollectionResult<T> : IResult<T>
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int TotalCount { get; set; }

        IList<T> Items { get; }
        IList<HttpStatusCode?> StatusCodes { get; }
    }

    public class CollectionResult<T> : ICollectionResult<T>
    {
        public CollectionResult()
        {
            Items = new List<T>();
            StatusCodes = new List<HttpStatusCode?>(); 
            ErrorMessage = ""; 
        }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public IList<T> Items { get; private set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeInMs { get; set; }

        public IList<HttpStatusCode?> StatusCodes { get; private set; }
    }
}
