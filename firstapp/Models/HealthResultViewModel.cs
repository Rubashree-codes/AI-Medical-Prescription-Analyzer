namespace Firstapp.Models
{
    public class HealthResultViewModel
    {
        public float Sleep { get; set; }
        public float Water { get; set; }
        public float Exercise { get; set; }
        public float Visits { get; set; }
        public float Fatigue { get; set; }

        public float? StressScore { get; set; }

        public string? Insight { get; set; }
    }
}