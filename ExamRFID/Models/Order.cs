using System.ComponentModel.DataAnnotations;

namespace ExamRFID.Models
{
    // A sales order of one customer, with many items. The attributes check the forms; the database rules are in AppDbContext.
    // Used by: OrdersController, the Orders views and the Performance page.
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the order date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Order date")]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Range(1, int.MaxValue, ErrorMessage = "Please choose a customer.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
