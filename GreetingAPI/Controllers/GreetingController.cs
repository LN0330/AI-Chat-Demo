// GreetingAPI/Controllers/GreetingController.cs
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using GreetingAPI.Models;        // 引用資料模型


[Route("api/greeting")]
public class GreetingController : ControllerBase
{
    // 模型執行檔路徑與資料庫連線字串
    private static readonly string LlamaCliPath = Path.Combine(Directory.GetCurrentDirectory(), "llama_model\\llama.cpp\\build\\bin\\Release", "llama-cli.exe");
    private static readonly string ModelPath = Path.Combine(Directory.GetCurrentDirectory(), "llama_model", "Llama-3.2-3B-Instruct-Q6_K_L.gguf");
    private readonly string connectionString = "server=localhost;database=chat_history;user=root;password=!QAZ2wsx1234";

    [HttpPost("ask-ai")]
    // 使用 AskAIRequest 模型接收請求，回傳處理後的字串
    public async Task<ActionResult<string>> AskAI([FromBody] AskAIRequest requestModel)
    {
        // 驗證請求內容
        if (requestModel == null || string.IsNullOrWhiteSpace(requestModel.RequestText))
        {
            return BadRequest("請求內容不可為空");
        }

        string userPrompt = requestModel.RequestText;

        // 設定並執行 Llama C++ CLI 程序
        string arguments = $"-m {ModelPath} -no-cnv -n 200 -p \"{userPrompt}\"";
        var processStartInfo = new ProcessStartInfo
        {
            FileName = LlamaCliPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using (var process = Process.Start(processStartInfo))
            {
                // 處理 AI 程序啟動失敗的情況
                if (process == null)
                {
                    return StatusCode(500, new { error = "無法啟動 AI 模型處理程序。" });
                }

                string responseText = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                responseText = responseText.Trim();
                string firstParagraph = responseText.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

                // 將問答紀錄存入資料庫
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO history (question, answer) VALUES (@question, @answer)";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@question", userPrompt);
                        cmd.Parameters.AddWithValue("@answer", firstParagraph);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Console.WriteLine("[Debug] 資料庫:新增操作, 輸入: " + userPrompt);
                // 回傳 AI 回應的第一段文字
                return Ok(firstParagraph);
            }
        }
        catch (Exception ex)
        {
            // 記錄詳細錯誤資訊
            Console.WriteLine($"[Error] AskAI failed: {ex.ToString()}");
            return StatusCode(500, new { error = "處理請求時發生內部錯誤：" + ex.Message });
        }
    }

    [HttpGet("history")]
    // 使用 ActionResult<T> 以增強型別安全與 API 文件說明
    public async Task<ActionResult<IEnumerable<HistoryItem>>> GetHistory()
    {
        var historyList = new List<HistoryItem>();
        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // 只選取需要的欄位
                string query = "SELECT id, question, answer FROM history ORDER BY id ASC";
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // 建立並填入 HistoryItem 模型物件
                        historyList.Add(new HistoryItem
                        {
                            Id = reader.GetInt32(0),
                            Question = reader.GetString(1),
                            Answer = reader.GetString(2)
                        });
                    }
                }
            }
            // 回傳 HistoryItem 物件列表
            return Ok(historyList);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] GetHistory failed: {ex.ToString()}");
            return StatusCode(500, new { error = "讀取歷史紀錄時發生錯誤。" });
        }
    }

    [HttpDelete("history/{id}")]
    public async Task<IActionResult> DeleteHistory(int id)
    {
        // ID 驗證
        if (id <= 0)
        {
            return BadRequest("無效的 ID。");
        }
        try
        {
            int rowsAffected = 0;
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM history WHERE id = @id";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            if (rowsAffected > 0)
            {
                Console.WriteLine("[Debug] 資料庫:刪除操作, 刪除ID: " + id);
                // 成功刪除時回傳標準的 NoContent (204)
                return NoContent();
            }
            else
            {
                Console.WriteLine("[Debug] 資料庫:刪除操作失敗, ID 不存在: " + id);
                // 若找不到指定 ID，回傳 Not Found (404)
                return NotFound(new { message = $"找不到 ID 為 {id} 的歷史紀錄。" });
            }
        }
        catch (Exception ex)
        {

            Console.WriteLine($"[Error] DeleteHistory failed for ID {id}: {ex.ToString()}");
            return StatusCode(500, new { error = "刪除歷史紀錄時發生錯誤。" });
        }
    }
}