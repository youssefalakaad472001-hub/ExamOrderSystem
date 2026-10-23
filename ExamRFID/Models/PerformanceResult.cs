namespace ExamRFID.Models
{
    // The result of one way on the Performance page: times, query count and SQL.
    // Used by: PerformanceController and Views/Performance/Index.
    public class PerformanceResult
    {
        public string Strategy { get; set; } = "";

        public double AverageMs { get; set; }

        public double FastestMs { get; set; }

        public double SlowestMs { get; set; }

        public int QueryCount { get; set; }

        public int OrdersLoaded { get; set; }

        public decimal GrandTotal { get; set; }

        public bool SameDataAsFirst { get; set; }

        public string Sql { get; set; } = "";
    }
}
