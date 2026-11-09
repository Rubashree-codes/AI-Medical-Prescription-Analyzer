using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Reflection;

namespace PrescriptionAPI
{
    public class PrescriptionDAL
    {
        private readonly string _connectionString;

        public PrescriptionDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")??

                throw new Exception("DefaultConnection is missing in appsettings.json");
        }

        public async Task<Guid> SavePrescriptionAsync(
    int userId,
    string FileName,
    string patientName,
    string age,
    string gender,
    string doctorName,
    string diagnosis,
    string treatmentPlan)
        {
            Guid displayId = Guid.NewGuid();
            using SqlConnection conn = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand("sp_SavePrescription", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DisplayId", displayId);
            cmd.Parameters.AddWithValue("@FileName", FileName);
            cmd.Parameters.AddWithValue("@PatientName", patientName);
            cmd.Parameters.AddWithValue("@Age", age);
            cmd.Parameters.AddWithValue("@Gender", gender);
            cmd.Parameters.AddWithValue("@DoctorName", doctorName);
            cmd.Parameters.AddWithValue("@Diagnosis", diagnosis);
            cmd.Parameters.AddWithValue("@TreatmentPlan", treatmentPlan);

            await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            return displayId;
        }

        public async Task<string> RegisterUserAsync(string fullName, string email, string password)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand("sp_RegisterUser", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                var result = await cmd.ExecuteScalarAsync();

                return result?.ToString() ?? "";
            }
        }

        public async Task<(int?, string)> ValidateUserAsync(string email, string password)
        {
            using SqlConnection conn = new SqlConnection(_connectionString); 
            using SqlCommand cmd = new SqlCommand("sp_ValidateUser", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);

            await conn.OpenAsync();
            SqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int userId = Convert.ToInt32(reader["UserId"]);
                string fullName = reader["FullName"].ToString();

                return (userId, fullName);
            }

            return (null, null);
        }

    }
}


