using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;


namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiUrl = "http://localhost:5093/api/greeting";

        public MainWindow()
        {
            InitializeComponent();
            LoadHistory();
        }

        private async void SendMessage(object sender, RoutedEventArgs e)
        {
            string userInput = UserInput.Text;

            if (string.IsNullOrWhiteSpace(userInput))
            {
                MessageBox.Show("請輸入內容！");
                return;
            }

            var json = JsonSerializer.Serialize(userInput);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            AiResponse.Text = "正在處理中...";

            try
            {
                var response = await _httpClient.PostAsync($"{_apiUrl}/ask-ai", content);

                if (!response.IsSuccessStatusCode)
                {
                    AiResponse.Text = $"發生錯誤: {response.StatusCode}";
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                AiResponse.Text = responseBody;
                LoadHistory(); // 更新歷史紀錄
            }
            catch (Exception ex)
            {
                MessageBox.Show("發生錯誤：" + ex.Message);
                AiResponse.Text = "無法獲得回應";
            }
        }

        private async void LoadHistory()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/history");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("無法讀取歷史紀錄");
                    return;
                }

                var history = await response.Content.ReadAsStringAsync();
                var historyList = JsonSerializer.Deserialize<List<HistoryItem>>(history, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                HistoryList.Items.Clear();
                foreach (var item in historyList)
                {
                    // 使用 DisplayText 屬性來顯示問題和回答
                    HistoryList.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("載入歷史紀錄失敗：" + ex.Message);
            }
        }

        private async void DeleteHistory(object sender, RoutedEventArgs e)
        {
            if (HistoryList.SelectedItem == null)
            {
                MessageBox.Show("請選擇要刪除的歷史紀錄！");
                return;
            }

            // 直接取得選中的 HistoryItem 物件
            HistoryItem selectedItem = (HistoryItem)HistoryList.SelectedItem;
            int id = selectedItem.Id;  // 直接取得 ID

            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiUrl}/history/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("刪除失敗！");
                    return;
                }

                LoadHistory(); // 重新載入歷史紀錄
            }
            catch (Exception ex)
            {
                MessageBox.Show("刪除失敗：" + ex.Message);
            }
        }
    }

    public class HistoryItem
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }

        // 顯示格式化的問題和回答
        public string DisplayText => $"{Question} -> {Answer}";
    }
}
