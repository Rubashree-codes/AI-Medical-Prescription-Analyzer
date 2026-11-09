using Microsoft.AspNetCore.Mvc;
using Firstapp.Models;
using System.Text.Json;

namespace Firstapp.Controllers
{
    public class HealthAnalyticsController : Controller
    {
        public IActionResult Index()
        {
            HealthResultViewModel model = new HealthResultViewModel();

            if (TempData["HealthModel"] != null)
            {
                var json = TempData["HealthModel"] as string;

                if (!string.IsNullOrEmpty(json))
                {
                    model = JsonSerializer.Deserialize<HealthResultViewModel>(json)
                            ?? new HealthResultViewModel();
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Analyze(HealthResultViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var input = new MLModel.ModelInput()
            {
                SleepHours = model.Sleep,
                WaterIntake = model.Water,
                ExerciseMinutes = model.Exercise,
                DoctorVisits = model.Visits,
                FatigueLevel = model.Fatigue
            };

            var result = MLModel.Predict(input);

            model.StressScore = result.Score;

            if (result.Score < 30)
                model.Insight = "Low stress level. Keep maintaining healthy habits.";
            else if (result.Score < 60)
                model.Insight = "Moderate stress detected. Improve sleep & hydration.";
            else
                model.Insight = "High stress level. Immediate lifestyle improvement needed.";

            TempData["HealthModel"] = JsonSerializer.Serialize(model);

            return RedirectToAction("Index"); 
        }
    }
}