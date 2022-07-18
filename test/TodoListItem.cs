using Newtonsoft.Json;

namespace AcceptanceTests; 

public class TodoListItem
{
  
    [JsonProperty("id")]
    public string? Id { get; set; }

    public string? Task { get; set; }

    public int Priority { get; set; }

    public override string ToString()
    {
        return $"Id: {Id} Task: {Task} Priority: {Priority}";
    }

}