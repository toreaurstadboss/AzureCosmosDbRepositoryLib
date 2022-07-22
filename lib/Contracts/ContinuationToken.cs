using Newtonsoft.Json;

namespace AzureCosmosDbRepositoryLib.Contracts
{
    public class ContinuationToken
    {
        [JsonProperty("Version")]
        public string Version { get; set; } = null!;

        [JsonProperty("QueryPlan")]
        public string QueryPlan { get; set; } = null!;

        [JsonProperty("SourceContinuationToken")]
        public string SourceContinuationToken { get; set; } = null!;
    }


}
