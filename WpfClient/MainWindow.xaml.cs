// WpfClient/MainWindow.xaml.cs
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using WpfApp.Models; // 引用資料模型

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiUrl = "http://localhost:5093/api/greeting";

        public MainWindow()
        {
            InitializeComponent();
            // 設定 HttpClient 的位址
            _httpClient.BaseAddress = new Uri(_apiUrl.Replace("/api/greeting", ""));
            LoadHistory();
        }

        private async void SendMessage(object sender, RoutedEventArgs e)
        {
            string userInput = UserInput.Text;

            if (string.IsNullOrWhiteSpace(userInput))
            {
                MessageBox.Show("請輸入內容！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 建立符合 API AskAIRequest 模型的請求物件
            var requestPayload = new { RequestText = userInput };
            var json = JsonSerializer.Serialize(requestPayload);
            // 設定 HTTP Content-Type 為 application/json
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            AiResponse.Text = "正在處理中...";
            // 處理期間禁用輸入
            UserInput.IsEnabled = false;
            // 禁用發送按鈕 (假設 x:Name="SendButton")
            SendButton.IsEnabled = false;

            try
            {
                // 向 ask-ai 端點發送 POST 請求
                var response = await _httpClient.PostAsync("api/greeting/ask-ai", content);

                if (!response.IsSuccessStatusCode)
                {
                    // 嘗試讀取並解析 API 回傳的錯誤訊息
                    string errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorObj.TryGetProperty("error", out var errorMessage))
                        {
                            AiResponse.Text = $"發生錯誤 ({response.StatusCode}): {errorMessage.GetString()}";
                        }
                        else if (errorObj.TryGetProperty("message", out var message))
                        {
                            AiResponse.Text = $"發生錯誤 ({response.StatusCode}): {message.GetString()}";
                        }
                        else
                        {
                            AiResponse.Text = $"發生錯誤: {response.StatusCode}\n{errorContent}";
                        }
                    }
                    catch
                    {
                        // 若錯誤訊息非 JSON 格式，直接顯示原始文字
                        AiResponse.Text = $"發生錯誤: {response.StatusCode}\n{errorContent}";
                    }
                    return;
                }

                // API 直接回傳處理後的字串
                string responseBody = await response.Content.ReadAsStringAsync();
                AiResponse.Text = responseBody;
                // 成功後清除輸入框
                UserInput.Clear();
                // 成功後重新載入歷史紀錄
                LoadHistory();
            }
            // 捕捉特定的網路連線錯誤
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show("無法連接到伺服器，請確認 API 是否正在運行。\n錯誤：" + httpEx.Message, "連線錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                AiResponse.Text = "無法獲得回應";
            }
            catch (Exception ex)
            {
                MessageBox.Show("處理請求時發生錯誤：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                AiResponse.Text = "無法獲得回應";
            }
            finally
            {
                // 處理完畢後重新啟用輸入
                UserInput.IsEnabled = true;
                SendButton.IsEnabled = true;
            }
        }

        private async void LoadHistory()
        {
            try
            {
                // 載入期間禁用列表
                HistoryList.IsEnabled = false;
                // 向 history 端點發送 GET 請求
                var response = await _httpClient.GetAsync("api/greeting/history");

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"無法讀取歷史紀錄 ({response.StatusCode}).\n{errorContent}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var historyJson = await response.Content.ReadAsStringAsync();
                // 將 JSON 反序列化為 List<HistoryItem> (使用 WpfApp.Models)
                var historyList = JsonSerializer.Deserialize<List<HistoryItem>>(historyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                HistoryList.Items.Clear();
                if (historyList != null)
                {
                    foreach (var item in historyList)
                    {
                        // 將 HistoryItem 物件加入列表
                        HistoryList.Items.Add(item);
                    }
                }
            }
            // 捕捉特定的網路連線錯誤
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show("無法連接到伺服器讀取歷史紀錄。\n錯誤：" + httpEx.Message, "連線錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // 捕捉 JSON 解析錯誤
            catch (JsonException jsonEx)
            {
                MessageBox.Show("解析歷史紀錄時發生錯誤：" + jsonEx.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("載入歷史紀錄失敗：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 處理完畢後重新啟用列表
                HistoryList.IsEnabled = true;
            }
        }

        private async void DeleteHistory(object sender, RoutedEventArgs e)
        {
            // 確認選取的項目是 HistoryItem 型別
            if (HistoryList.SelectedItem is HistoryItem selectedItem)
            {
                int idToDelete = selectedItem.Id;

                var confirmResult = MessageBox.Show($"確定要刪除紀錄 \"{selectedItem.Question}\" 嗎？", "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmResult == MessageBoxResult.No)
                {
                    return;
                }

                try
                {
                    // 禁用刪除按鈕 (假設 x:Name="DeleteButton")
                    DeleteButton.IsEnabled = false;
                    // 向 history/{id} 端點發送 DELETE 請求
                    var response = await _httpClient.DeleteAsync($"api/greeting/history/{idToDelete}");

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"刪除失敗 ({response.StatusCode}).\n{errorContent}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 成功刪除後重新載入歷史紀錄
                    LoadHistory();
                }
                // 捕捉特定的網路連線錯誤
                catch (HttpRequestException httpEx)
                {
                    MessageBox.Show("無法連接到伺服器刪除紀錄。\n錯誤：" + httpEx.Message, "連線錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("刪除過程中發生錯誤：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    DeleteButton.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("請先選擇一筆要刪除的歷史紀錄！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}