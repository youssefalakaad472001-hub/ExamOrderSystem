using ExamRFID.Data;
using ExamRFID.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamRFID.Controllers
{
    // All the customer pages: list, details, create, edit and delete.
    public class CustomersController : Controller
    {
        private const int PageSize = 20;

        private readonly AppDbContext _db;

        private readonly ILogger<CustomersController> _logger;

        // ASP.NET Core gives us the DbContext and the logger (dependency injection).
        public CustomersController(AppDbContext db, ILogger<CustomersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /Customers?page=1 - shows 20 customers per page, sorted by name.
        // Used by: "Customers" in the sidebar and the Previous/Next buttons.
        public async Task<IActionResult> Index(int page = 1)
        {
            int totalCustomers = await _db.Customers.CountAsync();

            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCustomers / (double)PageSize));

            page = Math.Clamp(page, 1, totalPages);

            List<Customer> customers = await _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(customers);
        }

        // GET /Customers/Details/5 - shows one customer with their orders (newest first).
        // Used by: the eye icon in the customer list, and after create/edit.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Customer? customer = await _db.Customers
                .AsNoTracking()
                .Include(c => c.Orders.OrderByDescending(o => o.OrderDate))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET /Customers/Create - shows an empty customer form.
        // Used by: the "New customer" button.
        public IActionResult Create()
        {
            return View();
        }

        // POST /Customers/Create - checks the form (attributes + unique email) and saves the new customer.
        // Used by: the Save button on the New customer form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                bool emailTaken = await _db.Customers.AnyAsync(c => c.Email == customer.Email);

                if (emailTaken)
                {
                    ModelState.AddModelError("Email", "Another customer already uses this email.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();

                TempData["Success"] = "The customer was created.";

                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not create a customer.");

                ModelState.AddModelError("", "The customer could not be saved. Please try again.");
                return View(customer);
            }
        }

        // GET /Customers/Edit/5 - shows the form filled with the customer's data.
        // Used by: the pencil icon and the Edit button.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Customer? customer = await _db.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST /Customers/Edit/5 - checks the form and saves the changes (load, copy, save).
        // Used by: the Save button on the Edit customer form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Email,Phone")] Customer customer)
        {
            Customer? savedCustomer = await _db.Customers.FindAsync(id);

            if (savedCustomer == null)
            {
                return NotFound();
            }

            customer.Id = id;

            if (ModelState.IsValid)
            {
                bool emailTaken = await _db.Customers.AnyAsync(c => c.Email == customer.Email && c.Id != id);

                if (emailTaken)
                {
                    ModelState.AddModelError("Email", "Another customer already uses this email.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            savedCustomer.Name = customer.Name;
            savedCustomer.Email = customer.Email;
            savedCustomer.Phone = customer.Phone;

            try
            {
                await _db.SaveChangesAsync();

                TempData["Success"] = "The customer was updated.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not update customer {CustomerId}.", id);

                ModelState.AddModelError("", "The changes could not be saved. The customer may have been deleted, or there was a database problem. Please try again.");
                return View(customer);
            }
        }

        // GET /Customers/Delete/5 - asks "Are you sure?" and warns if the customer has orders.
        // Used by: the trash icon and the Delete button.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Customer? customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            ViewBag.OrderCount = await _db.Orders.CountAsync(o => o.CustomerId == id);

            return View(customer);
        }

        // POST /Customers/Delete/5 - deletes the customer, but only if they have no orders.
        // Used by: the "Yes, delete" button.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Customer? customer = await _db.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            bool hasOrders = await _db.Orders.AnyAsync(o => o.CustomerId == id);
            if (hasOrders)
            {
                TempData["Error"] = "This customer has orders, so it cannot be deleted. Delete the orders first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();

                TempData["Success"] = "The customer was deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not delete customer {CustomerId}.", id);

                TempData["Error"] = "The customer could not be deleted. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
