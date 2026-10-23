using ExamRFID.Models;

namespace ExamRFID.Data
{
    // Creates fake customers, orders and items.
    public static class SeedData
    {
        // Adds "customerCount" customers, each with 1-10 orders of 1-5 items. Random(12345) gives the same data every time.
        // Used by: Program.cs (100 customers on the first run) and PerformanceController.Generate.
        public static void AddTestData(AppDbContext db, int customerCount)
        {
            var random = new Random(12345);

            string[] products = { "Laptop", "Mouse", "Keyboard", "Monitor", "USB Cable",
                                  "Headphones", "Desk Lamp", "Office Chair", "Notebook", "Pen Set" };

            var firstDay = new DateTime(2025, 1, 1);

            for (int c = 1; c <= customerCount; c++)
            {
                var customer = new Customer
                {
                    Name = "Customer " + c,
                    Email = "customer" + c + "@example.com",
                    Phone = "555-" + c.ToString("0000")
                };

                int orderCount = random.Next(1, 11);

                for (int o = 0; o < orderCount; o++)
                {
                    var order = new Order { OrderDate = firstDay.AddDays(random.Next(0, 365)) };

                    int itemCount = random.Next(1, 6);

                    for (int i = 0; i < itemCount; i++)
                    {
                        order.Items.Add(new OrderItem
                        {
                            ProductName = products[random.Next(products.Length)],
                            Quantity = random.Next(1, 6),
                            UnitPrice = random.Next(100, 50000) / 100m
                        });
                    }

                    customer.Orders.Add(order);
                }

                db.Customers.Add(customer);
            }

            db.SaveChanges();
        }
    }
}
