using ExamRFID.Data;
using ExamRFID.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamRFID.Controllers
{
    // Adds, edits and deletes the items of an order. The items are listed on the order details page.
    public class OrderItemsController : Controller
    {
        private readonly AppDbContext _db;

        private readonly ILogger<OrderItemsController> _logger;

        // ASP.NET Core gives us the DbContext and the logger (dependency injection).
        public OrderItemsController(AppDbContext db, ILogger<OrderItemsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /OrderItems/Create?orderId=5 - shows an empty item form for that order.
        // Used by: the "Add item" button on the order details page.
        public async Task<IActionResult> Create(int? orderId)
        {
            if (orderId == null)
            {
                return NotFound();
            }

            bool orderExists = await _db.Orders.AnyAsync(o => o.Id == orderId);
            if (!orderExists)
            {
                return NotFound();
            }

            return View(new OrderItem { OrderId = orderId.Value });
        }

        // POST /OrderItems/Create - checks and saves the new item, then goes back to the order.
        // Used by: the Save button on the Add item form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,ProductName,Quantity,UnitPrice")] OrderItem item)
        {
            bool orderExists = await _db.Orders.AnyAsync(o => o.Id == item.OrderId);
            if (!orderExists)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            try
            {
                _db.OrderItems.Add(item);
                await _db.SaveChangesAsync();

                TempData["Success"] = "The item was added.";
                return RedirectToAction("Details", "Orders", new { id = item.OrderId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not add an item to order {OrderId}.", item.OrderId);

                ModelState.AddModelError("", "The item could not be saved. Please try again.");
                return View(item);
            }
        }

        // GET /OrderItems/Edit/5 - shows the form filled with the item's data.
        // Used by: the pencil icon next to an item.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            OrderItem? item = await _db.OrderItems.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST /OrderItems/Edit/5 - saves the changes; the order id always comes from the database, not the form.
        // Used by: the Save button on the Edit item form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductName,Quantity,UnitPrice")] OrderItem item)
        {
            OrderItem? savedItem = await _db.OrderItems.FindAsync(id);

            if (savedItem == null)
            {
                return NotFound();
            }

            item.Id = id;
            item.OrderId = savedItem.OrderId;

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            savedItem.ProductName = item.ProductName;
            savedItem.Quantity = item.Quantity;
            savedItem.UnitPrice = item.UnitPrice;

            try
            {
                await _db.SaveChangesAsync();

                TempData["Success"] = "The item was updated.";
                return RedirectToAction("Details", "Orders", new { id = savedItem.OrderId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not update item {ItemId}.", id);

                ModelState.AddModelError("", "The changes could not be saved. The item may have been deleted, or there was a database problem. Please try again.");
                return View(item);
            }
        }

        // GET /OrderItems/Delete/5 - asks "Are you sure?" before deleting the item.
        // Used by: the trash icon next to an item.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            OrderItem? item = await _db.OrderItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST /OrderItems/Delete/5 - deletes the item and goes back to the order.
        // Used by: the "Yes, delete" button.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            OrderItem? item = await _db.OrderItems.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            int orderId = item.OrderId;

            try
            {
                _db.OrderItems.Remove(item);
                await _db.SaveChangesAsync();

                TempData["Success"] = "The item was deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not delete item {ItemId}.", id);

                TempData["Error"] = "The item could not be deleted. Please try again.";
            }

            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
    }
}
