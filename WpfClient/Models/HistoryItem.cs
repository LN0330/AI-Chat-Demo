// WpfClient/Models/HistoryItem.cs
namespace WpfApp.Models
{
    public class HistoryItem
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        public string DisplayText => $"{Question} -> {Answer}";
    }
}