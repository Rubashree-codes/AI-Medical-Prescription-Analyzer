using Microsoft.ML.Data;

public class HabitPrediction
{
    [ColumnName("Score")]
    public float PredictedStressScore { get; set; }
}