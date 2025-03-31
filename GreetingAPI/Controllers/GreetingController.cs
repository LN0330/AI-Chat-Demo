using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;

[Route("api/greeting")]
public class GreetingController : ControllerBase
{
    private static readonly string LlamaCliPath = Path.Combine(Directory.GetCurrentDirectory(), "llama_model\\llama.cpp\\build\\bin\\Release", "llama-cli.exe");
    private static readonly string ModelPath = Path.Combine(Directory.GetCurrentDirectory(), "llama_model", "Llama-3.2-3B-Instruct-Q6_K_L.gguf");
    private readonly string connectionString = "server=localhost;database=chat_history;user=root;password=!QAZ2wsx1234";

    [HttpPost("ask-ai")]
    public async Task<IActionResult> AskAI([FromBody] string request)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            return BadRequest("請求內容不可為空");
        }
        string arguments = $"-m {ModelPath} -no-cnv -n 200 -p \"{request}\"";
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
                using (var reader = process.StandardOutput)
                {
                    string responseText = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    responseText = responseText.Trim();
                    string firstParagraph = responseText.Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0];

                    using (var connection = new MySqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string query = "INSERT INTO history (question, answer) VALUES (@question, @answer)";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@question", request);
                            cmd.Parameters.AddWithValue("@answer", firstParagraph);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    Console.WriteLine("[Debug] 資料庫:新增操作, 輸入: " + request);
                    return Ok(firstParagraph);
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var historyList = new List<object>();
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            string query = "SELECT id, question, answer FROM history ORDER BY id ASC";
            using (var cmd = new MySqlCommand(query, connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    historyList.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Question = reader.GetString(1),
                        Answer = reader.GetString(2)
                    });
                }
            }
        }
        return Ok(historyList);
    }

    [HttpDelete("history/{id}")]
    public async Task<IActionResult> DeleteHistory(int id)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            string query = "DELETE FROM history WHERE id = @id";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        Console.WriteLine("[Debug] 資料庫:刪除操作, 刪除ID: " + id);
        return NoContent();
    }
}
