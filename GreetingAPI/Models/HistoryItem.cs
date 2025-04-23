// GreetingAPI/Models/HistoryItem.cs
namespace GreetingAPI.Models
{
    public class HistoryItem
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}