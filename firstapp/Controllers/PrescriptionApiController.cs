using Firstapp;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PrescriptionAPI

{
    [ApiController]
    [Route("api/prescription")]
    public class PrescriptionApiController : Controller
    {
        private readonly IConfiguration _config;
        private readonly PrescriptionDAL _dal;

        public PrescriptionApiController(IConfiguration config, PrescriptionDAL dal )
        {
            _config = config;
            _dal = dal;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(IFormFile file)
        {
            Console.WriteLine("Analyze API HIT");
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            var apiKey = _config["Affinda:ApiKey"];
            var workspaceId = _config["Affinda:WorkspaceId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(workspaceId))
                return BadRequest("Affinda API key or Workspace ID missing.");

            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(fileBytes), "file", file.FileName);
            form.Add(new StringContent("true"), "ocr");
            form.Add(new StringContent(workspaceId), "workspace");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.affinda.com/v3/documents", form);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Affinda API timed out. PDF may be too large or scanned badly.");
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("AFFINDA RESPONSE:");
            Console.WriteLine(json);
            if (!response.IsSuccessStatusCode)
                return BadRequest($"Affinda API failed: {json}");

            var root = JObject.Parse(json);

            string SafeField(string field)
            {
                var token = root["data"]?[field];

                if (token is JObject obj && obj["parsed"] != null)
                    return obj["parsed"]!.ToString();

                if (token is JValue value)
                    return value.ToString();

                return "";
            }


            string patientName = SafeField("patientName");
            string age = SafeField("age");
            string genderRaw = SafeField("gender");
            string gender = string.IsNullOrWhiteSpace(genderRaw)
                ? "Unknown"
                : (genderRaw.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "Male" : "Female");
            string doctorName = SafeField("doctorName");
            string diagnosis = SafeField("diagnosis");
            string treatmentPlan = SafeField("treatmentPlan");
            if (string.IsNullOrWhiteSpace(treatmentPlan) && root["data"]?["treatmentPlan"] is JArray tpArray)
            {
                treatmentPlan = string.Join("\n", tpArray.Select(x => x["raw"]?.ToString()));
            }

            float avgConfidence = 0;
            int confidenceCount = 0;

            if (root["data"] is JObject dataObj)
            {
                foreach (var property in dataObj.Properties())
                {
                    if (property.Value.Type != JTokenType.Object)
                        continue;

                    var confToken = property.Value["textExtractionConfidence"];

                    if (confToken != null && float.TryParse(confToken.ToString(), out float conf))
                    {
                        avgConfidence += conf;
                        confidenceCount++;
                    }
                }
            }

            if (confidenceCount > 0)
            {
                avgConfidence /= confidenceCount;
            }

            float fieldCount = 0;

            if (root["data"] is JObject dataObject)
            {
                fieldCount = dataObject.Properties().Count();
            }
            string fullText = root["data"]?["rawText"]?.ToString() ?? "";
            float textLength = fullText.Length;
            float imageSizeKB = file.Length / 1024f;

           
            var input = new PrescriptionReliabilityModel.ModelInput
            {
                AvgConfidence = avgConfidence,
                FieldCount = fieldCount,
                TextLength = textLength,
                ImageSizeKB = imageSizeKB
            };
            Debug.WriteLine($"Confidence: {avgConfidence}");
            Debug.WriteLine($"FieldCount: {fieldCount}");
            Debug.WriteLine($"TextLength: {textLength}");
            Debug.WriteLine($"ImageSizeKB: {imageSizeKB}");


            var prediction = PrescriptionReliabilityModel.Predict(input);

            float reliabilityScore = prediction.Score;
            reliabilityScore = (float)Math.Round(reliabilityScore);

            return Ok(new
            {
                patientName,
                age,
                gender,
                doctorName,
                diagnosis,
                treatmentPlan,
                reliabilityScore
            });
        }

        [HttpPost("/api/save")]
        public async Task<IActionResult> SavePrescription([FromBody] PrescriptionModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.PatientName))
                return BadRequest(new { message = "No data to save" });

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized(new { message = "User not logged in" });

            try
            {
                //var dal = new PrescriptionDAL(connectionString);

                Guid displayId = await _dal.SavePrescriptionAsync( 
    userId.Value,
    model.FileName ?? "",
    model.PatientName ?? "",
    model.Age ?? "",
    model.Gender ?? "",
    model.DoctorName ?? "",
    model.Diagnosis ?? "",
    model.TreatmentPlan ?? ""                                
);

                return Ok(new
                {
                    message = "Saved successfully",
                    displayId = displayId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class PrescriptionModel
    {
        public string? PatientName { get; set; }
        public string? Age { get; set; }
        public string? Gender { get; set; }
        public string? DoctorName { get; set; }
        public string? Diagnosis { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? FileName { get; set; }

    }
}
