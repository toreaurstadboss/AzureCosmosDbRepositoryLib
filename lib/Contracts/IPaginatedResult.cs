namespace AzureCosmosDbRepositoryLib.Contracts
{

    public interface IPaginatedResult<T>
    {
        public IList<T> Items { get; }
        public string? ContinuationToken { get; set; }
    }

    public class PaginatedResult<T> : IPaginatedResult<T>
    {
        public PaginatedResult(string continuationToken, IEnumerable<T> items)
        {
            Items = new List<T>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            ContinuationToken = continuationToken;
        }
        public IList<T> Items { get; private set; }
        public string? ContinuationToken { get; set; }
    }
}
