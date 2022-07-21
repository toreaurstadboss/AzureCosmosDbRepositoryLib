using AzureCosmosDbRepositoryLib.Contracts;
using Microsoft.Azure.Cosmos;
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


    }
}
