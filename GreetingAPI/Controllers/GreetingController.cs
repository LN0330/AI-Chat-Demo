using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

[Route("api/greeting")]
public class GreetingController : ControllerBase
{
    // 請自行更換模型路徑
    private static readonly string LlamaCliPath = Path.Combine(Directory.GetCurrentDirectory(), "llama_model\\llama.cpp\\build\\bin\\Release", "llama-cli.exe");
    private static readonly string ModelPath = Path.Combine(Directory.GetCurrentDirectory(), "llama_model", "Llama-3.2-3B-Instruct-Q6_K_L.gguf");

    [HttpPost("ask-ai")]
    public async Task<IActionResult> AskAI([FromBody] string request)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            return BadRequest("請求內容不可為空");
        }

        // -no-cnv 關閉交談模式 -n 限制輸出字數
        string arguments = $"-m {ModelPath} -no-cnv -n 200 -p \"{request}\"";

        // 建立 ProcessStartInfo 執行 llama-cli.exe、傳入 arguments
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

                    if (!string.IsNullOrEmpty(responseText))
                    {
                        // 內容通常過多，只取第一段Demo
                        string firstParagraph = responseText.Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)[1];
                        ;
                        Console.WriteLine($"[Debug] First Paragraph: {firstParagraph}");

                        return Ok(firstParagraph);
                    }
                    return BadRequest("回傳失敗");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

}