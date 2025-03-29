using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SendMessage(object sender, RoutedEventArgs e)
        {
            string userInput = UserInput.Text;

            if (string.IsNullOrWhiteSpace(userInput))
            {
                MessageBox.Show("請輸入內容！");
                return;
            }

            // 回傳 JSON 格式
            var json = JsonSerializer.Serialize(userInput);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            AiResponse.Text = "正在處理中...";

            try
            {
                var response = await _httpClient.PostAsync("http://localhost:5093/api/greeting/ask-ai", content);

                if (!response.IsSuccessStatusCode)
                {
                    AiResponse.Text = $"發生錯誤: {response.StatusCode}";
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                AiResponse.Text = responseBody;
            }
            catch (Exception ex)
            {
                MessageBox.Show("發生錯誤：" + ex.Message);
                AiResponse.Text = "無法獲得回應";
            }
        }
    }
}
