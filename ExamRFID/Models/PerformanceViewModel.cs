using System.ComponentModel.DataAnnotations;

namespace ExamRFID.Models
{
    // Everything the Performance page shows: form values, row counts, results and saved history.
    // Used by: PerformanceController and Views/Performance/Index.
    public class PerformanceViewModel
    {
        [Range(1, 5000, ErrorMessage = "Orders to load must be between 1 and 5000.")]
        [Display(Name = "Orders to load")]
        public int OrdersToLoad { get; set; } = 200;

        [Range(1, 50, ErrorMessage = "Runs must be between 1 and 50.")]
        [Display(Name = "Measured runs per strategy")]
        public int Runs { get; set; } = 5;

        [Range(10, 5000, ErrorMessage = "Customers must be between 10 and 5000.")]
        [Display(Name = "Customers to generate")]
        public int CustomersToGenerate { get; set; } = 1000;

        public int CustomerCount { get; set; }

        public int OrderCount { get; set; }

        public int ItemCount { get; set; }

        public List<PerformanceResult> Results { get; set; } = new List<PerformanceResult>();

        public List<string[]> History { get; set; } = new List<string[]>();

        public string? Message { get; set; }
    }
}
