using ExamRFID.Data;
using ExamRFID.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ExamRFID.Controllers
{
    // All the order pages: list, details, create, edit and delete.
    public class OrdersController : Controller
    {
        private const int PageSize = 20;

        private readonly AppDbContext _db;

        private readonly ILogger<OrdersController> _logger;

        // ASP.NET Core gives us the DbContext and the logger (dependency injection).
        public OrdersController(AppDbContext db, ILogger<OrdersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /Orders?page=1 - shows 20 orders per page (newest first) with customer name, item count and total, in one query (projection).
        // Used by: "Orders" in the sidebar and the Previous/Next buttons.
        public async Task<IActionResult> Index(int page = 1)
        {
            int totalOrders = await _db.Orders.CountAsync();

            int totalPages = Math.Max(1, (int)Math.Ceiling(totalOrders / (double)PageSize));

            page = Math.Clamp(page, 1, totalPages);

            List<OrderListItem> orders = await _db.Orders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(o => new OrderListItem
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    CustomerName = o.Customer!.Name,
                    ItemCount = o.Items.Count,
                    Total = o.Items.Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }

        // GET /Orders/Details/5 - shows one order with its customer and items (Include).
        // Used by: the eye icon in the order list, and after every create/edit.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Order? order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET /Orders/Create or /Orders/Create?customerId=5 - shows a new order form with today's date.
        // Used by: "New order" and "New order for this customer".
        public async Task<IActionResult> Create(int? customerId)
        {
            var order = new Order { OrderDate = DateTime.Today, CustomerId = customerId ?? 0 };

            await FillCustomerListAsync();

            return View(order);
        }

        // POST /Orders/Create - checks and saves the new order, then opens it so items can be added.
        // Used by: the Save button on the New order form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderDate,CustomerId")] Order order)
        {
            await CheckOrderAsync(order);

            if (!ModelState.IsValid)
            {
                await FillCustomerListAsync();
                return View(order);
            }

            try
            {
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                TempData["Success"] = "The order was created. Now add some items to it.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not create an order.");

                ModelState.AddModelError("", "The order could not be saved. Please try again.");
                await FillCustomerListAsync();
                return View(order);
            }
        }

        // GET /Orders/Edit/5 - shows the form with the order's customer and date.
        // Used by: the pencil icon and the "Edit order" button.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Order? order = await _db.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            await FillCustomerListAsync();
            return View(order);
        }

        // POST /Orders/Edit/5 - checks and saves the changed customer and date.
        // Used by: the Save button on the Edit order form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderDate,CustomerId")] Order order)
        {
            Order? savedOrder = await _db.Orders.FindAsync(id);

            if (savedOrder == null)
            {
                return NotFound();
            }

            order.Id = id;

            await CheckOrderAsync(order);

            if (!ModelState.IsValid)
            {
                await FillCustomerListAsync();
                return View(order);
            }

            savedOrder.OrderDate = order.OrderDate;
            savedOrder.CustomerId = order.CustomerId;

            try
            {
                await _db.SaveChangesAsync();

                TempData["Success"] = "The order was updated.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not update order {OrderId}.", id);

                ModelState.AddModelError("", "The changes could not be saved. The order may have been deleted, or there was a database problem. Please try again.");
                await FillCustomerListAsync();
                return View(order);
            }
        }

        // GET /Orders/Delete/5 - shows what will be deleted (the order and its items).
        // Used by: the trash icon and the "Delete order" button.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Order? order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST /Orders/Delete/5 - deletes the order; the database deletes its items too (cascade).
        // Used by: the "Yes, delete" button.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Order? order = await _db.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            try
            {
                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Order #" + id + " was deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not delete order {OrderId}.", id);

                TempData["Error"] = "The order could not be deleted. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // Extra checks the attributes can't do: the date is not in the future and the customer exists.
        // Used by: Create (POST) and Edit (POST).
        private async Task CheckOrderAsync(Order order)
        {
            if (order.OrderDate.Date > DateTime.Today)
            {
                ModelState.AddModelError("OrderDate", "The order date cannot be in the future.");
            }

            if (order.CustomerId > 0)
            {
                bool customerExists = await _db.Customers.AnyAsync(c => c.Id == order.CustomerId);
                if (!customerExists)
                {
                    ModelState.AddModelError("CustomerId", "The chosen customer does not exist.");
                }
            }
        }

        // Puts the customers (Id and Name only) in ViewBag.Customers for the drop-down list.
        // Used by: every action that shows the order form.
        private async Task FillCustomerListAsync()
        {
            var customers = await _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.Customers = new SelectList(customers, "Id", "Name");
        }
    }
}
