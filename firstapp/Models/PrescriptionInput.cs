namespace PrescriptionAPI
{
    public class PrescriptionInput
    {
        public string PatientName { get; set; } = "";
        public string Age { get; set; } = "";
        public string Gender { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string Diagnosis { get; set; } = "";
        public string TreatmentPlan { get; set; } = "";
    }
}
