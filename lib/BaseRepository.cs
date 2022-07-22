using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AzureCosmosDbRepositoryLib
{
    public class BaseRepository<T> 
    {

        public async Task<ISingleResult<T>> SafeCallSingleItem(Task<ItemResponse<T>> task)
        {
            var searchResult = new SingleResult<T>(); 
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await task;
                searchResult.Item = result.Resource;
                searchResult.ExecutionTimeInMs = stopwatch.ElapsedMilliseconds; 
                searchResult.StatusCode = result.StatusCode;
            }
            catch (CosmosException err)
            {
                searchResult.ErrorMessage = err.Message; 
            }
            return searchResult; 
        }

        public async Task<ICollectionResult<T>> SafeCallMultipleItems(Task<IList<ItemResponse<T>>> task)
        {
            var searchResult = new CollectionResult<T>();
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var results = await task;
                foreach (var item in results.Select(r => r))
                {
                    searchResult.Items.Add(item.Resource);
                }
                searchResult.ExecutionTimeInMs = stopwatch.ElapsedMilliseconds;
            }
            catch (CosmosException err)
            {
                searchResult.ErrorMessage = err.Message;
            }
            return searchResult;
        }

        public ICollectionResult<T> BuildSearchResultCollection(IList<ISingleResult<T>> searchResults)
        {
            var resultingResponse = new CollectionResult<T>();
            foreach (var item in searchResults)
            {
                if (item != null && item.Item != null)
                {
                    if (!string.IsNullOrEmpty(item.ErrorMessage))
                    {
                        resultingResponse.ErrorMessage += item.ErrorMessage; 
                    }
                    resultingResponse.Items.Add(item.Item);
                    resultingResponse.StatusCodes.Add(item.StatusCode); 
                }
            }
            resultingResponse.TotalCount = searchResults.Count;
            resultingResponse.PageIndex = 0;
            resultingResponse.PageSize = searchResults.Count; 
            //TODO: need to also supporting paging scenario for larger data sets 
            return resultingResponse;   
        }

        public ICollectionResult<T> BuildSearchResultCollection(IEnumerable<T> searchResults)
        {
            var resultingResponse = new CollectionResult<T>();
            foreach (var item in searchResults)
            {
                if (item != null && item != null)
                {                   
                    resultingResponse.Items.Add(item);
                }
            }
            resultingResponse.TotalCount = searchResults.Count();
            resultingResponse.PageIndex = 0;
            resultingResponse.PageSize = searchResults.Count();
            //TODO: need to also supporting paging scenario for larger data sets 
            return resultingResponse;
        }

        public ICollectionResult<T> BuildSearchResultCollection(Exception err)
        {
            var resultingResponse = new CollectionResult<T>();
            resultingResponse.ErrorMessage = err.ToString(); 
            return resultingResponse;
        }

        public ISingleResult<T> BuildSearchResult(T searchResult)
        {
            return new SingleResult<T>
            {
                Item = searchResult

            };
        }

        /// <summary>
        /// Returns default partition key for item. The type <typeparamref name="T"/> of item must have a property with JsonProperty attribute and set its property to 'id' to signal that property is the id of the item. Azure cosmos db requires identifiable objects 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public PartitionKey? GetDefaultPartitionKey(T item)
        {
            if (item == null)
                return null;
            var propWithJsonPropertyIdAttribute = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(p => p.GetCustomAttributes(typeof(JsonArrayAttribute), true).Any() && (p.GetCustomAttributes(typeof(JsonArrayAttribute), true).First() as JsonArrayAttribute)?.Id == "id");

            if (propWithJsonPropertyIdAttribute != null)
            {
                var idValue = propWithJsonPropertyIdAttribute.GetValue(item, null);
                if (idValue != null)
                {
                    return new PartitionKey(idValue.ToString()); 
                }
            }

            return null;
        }

        /// <summary>
        /// Returns default partition key for item. The type <typeparamref name="T"/> of item must have a property with JsonProperty attribute and set its property to 'id' to signal that property is the id of the item. Azure cosmos db requires identifiable objects 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public PartitionKey? GetDefaultPartitionKeyFromId(object? id)
        {
            if (id == null)
                return null;
            return new PartitionKey(id.ToString()); 
        }


    }
}
