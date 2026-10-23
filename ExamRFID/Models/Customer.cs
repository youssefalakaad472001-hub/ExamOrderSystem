using System.ComponentModel.DataAnnotations;

namespace ExamRFID.Models
{
    // A customer who places orders. The attributes check the forms; the database rules are in AppDbContext.
    // Used by: CustomersController, the Customers views and Order.Customer.
    public class Customer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the customer's name.")]
        [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Please enter an email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(200, ErrorMessage = "The email can be at most 200 characters.")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30, ErrorMessage = "The phone number can be at most 30 characters.")]
        [Display(Name = "Phone number")]
        public string? Phone { get; set; }

        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
