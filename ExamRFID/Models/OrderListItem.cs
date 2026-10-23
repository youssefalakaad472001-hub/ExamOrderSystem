namespace ExamRFID.Models
{
    // One row of the order list (not a table): number, date, customer name, item count and total.
    // Used by: OrdersController.Index and PerformanceController.
    public class OrderListItem
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }

        public string CustomerName { get; set; } = "";

        public int ItemCount { get; set; }

        public decimal Total { get; set; }
    }
}
