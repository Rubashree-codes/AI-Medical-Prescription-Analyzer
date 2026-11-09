using System;

namespace firstapp.Models
{
    public class Prescription
    {
        public int PrescriptionID { get; set; }
        public Guid DisplayId { get; set; }
        public string? PatientName { get; set; }
        public string? Age { get; set; }
        public string? Gender { get; set; }
        public string? DoctorName { get; set; }
        public string? Diagnosis { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? FileName { get; set; }

        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
    }
}
