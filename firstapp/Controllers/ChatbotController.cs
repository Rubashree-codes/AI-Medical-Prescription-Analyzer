using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class ChatbotController : Controller
{
    private readonly HttpClient _httpClient;

    public ChatbotController(IConfiguration configuration)
    {
        _httpClient = new HttpClient();

        string? apiKey = configuration["ChatPdf:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("ChatPDF API Key is missing in appsettings.json");

        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadPrescription(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
            return BadRequest("No file uploaded");

        using var formData = new MultipartFormDataContent();
        using var stream = pdfFile.OpenReadStream();

        formData.Add(new StreamContent(stream), "file", pdfFile.FileName);

        var response = await _httpClient.PostAsync(
            "https://api.chatpdf.com/v1/sources/add-file",
            formData
        );


        if (!response.IsSuccessStatusCode)
            return BadRequest("ChatPDF upload failed");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        string? sourceId = doc.RootElement
            .GetProperty("sourceId")
            .GetString();

        if (string.IsNullOrEmpty(sourceId))
            return BadRequest("Invalid document ID");

        return Json(new { documentId = sourceId });
    }

    [HttpPost]
    public async Task<IActionResult> AskQuestion([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.DocumentId) ||
            string.IsNullOrEmpty(request.Question))
        {
            return BadRequest("Invalid request");
        }

        var payload = new
        {
            sourceId = request.DocumentId,
            messages = new[]
            {
                new { role = "user", content = request.Question }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            "https://api.chatpdf.com/v1/chats/message",
            content
        );

        if (!response.IsSuccessStatusCode)
            return BadRequest("Failed to get response from ChatPDF");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        string answer = doc.RootElement
            .GetProperty("content")
            .GetString() ?? "No response received";

        return Json(new { answer });
    }
}

public class ChatRequest
{
    public string? DocumentId { get; set; }
    public string? Question { get; set; }
}
