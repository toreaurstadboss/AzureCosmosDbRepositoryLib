using System.Linq.Expressions;

namespace AzureCosmosDbRepositoryLib.Contracts
{
 
    public interface ISearchRequest<T>
    {
        Expression<Func<T, bool>>? Filter { get; set; }
    }

    public class SearchRequest<T> : ISearchRequest<T>
    {
        public Expression<Func<T, bool>>? Filter { get; set; }
    }


}
