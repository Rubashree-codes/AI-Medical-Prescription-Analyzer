using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using firstapp.Models;

namespace firstapp.Controllers
{
    public class ViewController : Controller
    {
        private readonly IConfiguration _configuration;

        public ViewController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(string searchTerm, int page = 1)
        {
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Account");

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            int pageSize = 5;   
            int totalRecords = 0;

            List<Prescription> prescriptions = new List<Prescription>();

            using (SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();

                using (SqlCommand countCmd = new SqlCommand("sp_GetUserPrescriptionCount", con))
                {
                    countCmd.CommandType = CommandType.StoredProcedure;
                    countCmd.Parameters.AddWithValue("@UserId", userId);

                    if (string.IsNullOrEmpty(searchTerm))
                        countCmd.Parameters.AddWithValue("@SearchTerm", DBNull.Value);
                    else
                        countCmd.Parameters.AddWithValue("@SearchTerm", searchTerm);

                    totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                using (SqlCommand cmd = new SqlCommand("sp_GetUserPrescriptions", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@PageNumber", page);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    if (string.IsNullOrEmpty(searchTerm))
                        cmd.Parameters.AddWithValue("@SearchTerm", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        prescriptions.Add(new Prescription
                        {
                            PrescriptionID = Convert.ToInt32(reader["PrescriptionID"]),
                            DisplayId = Guid.Parse(reader["DisplayId"].ToString()),
                            PatientName = reader["PatientName"]?.ToString() ?? "",
                            FileName = reader["FileName"]?.ToString() ?? "",
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        });
                    }
                }

                con.Close();
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.SearchTerm = searchTerm;

            return View(prescriptions);
        }

        public IActionResult Details(Guid id)
        {
            Prescription? prescription = null;

            using (SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_GetPrescriptionByDisplayId", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@DisplayId", id);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    prescription = new Prescription
                    {
                        PrescriptionID = Convert.ToInt32(reader["PrescriptionID"]),
                        FileName = reader["FileName"]?.ToString() ?? "",
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        PatientName = reader["PatientName"]?.ToString() ?? "",
                        DoctorName = reader["DoctorName"]?.ToString() ?? "",
                        Diagnosis = reader["Diagnosis"]?.ToString() ?? "",
                        TreatmentPlan = reader["TreatmentPlan"]?.ToString() ?? ""
                    };
                }

                con.Close();
            }

            if (prescription == null)
                return NotFound();

            return View(prescription);
        }
    }
}