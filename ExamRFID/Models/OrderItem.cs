using System.ComponentModel.DataAnnotations;

namespace ExamRFID.Models
{
    // One line of an order (product, quantity, unit price). The attributes check the forms.
    // Used by: OrderItemsController and the order details page.
    public class OrderItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the product name.")]
        [StringLength(100, ErrorMessage = "The product name can be at most 100 characters.")]
        [Display(Name = "Product")]
        public string ProductName { get; set; } = "";

        [Required(ErrorMessage = "Please enter the quantity.")]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Please enter the unit price.")]
        [Range(0.01, 100000, ErrorMessage = "Unit price must be between 0.01 and 100,000.")]
        [Display(Name = "Unit price")]
        public decimal UnitPrice { get; set; }

        public int OrderId { get; set; }

        public Order? Order { get; set; }
    }
}
