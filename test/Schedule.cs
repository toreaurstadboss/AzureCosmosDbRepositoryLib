namespace AcceptanceTests;

public partial class TodoListItem
{
    public class Schedule
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime != null && StartTime != null
            ? EndTime.Value.Subtract(StartTime.Value) : null; 
    }

}