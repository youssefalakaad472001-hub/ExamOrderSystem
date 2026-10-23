using System.Diagnostics;
using ExamRFID.Data;
using ExamRFID.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamRFID.Controllers
{
    // The Performance page: compares 3 ways of loading orders with their customer and items.
    public class PerformanceController : Controller
    {
        private const string NPlusOne = "N+1 (load in a loop)";
        private const string IncludeWay = "Include (eager loading)";
        private const string SelectWay = "Select (projection)";

        private readonly AppDbContext _db;

        private readonly string _resultsFile;

        private readonly ILogger<PerformanceController> _logger;

        // ASP.NET Core gives us the DbContext, the app folder (for the CSV file) and the logger.
        public PerformanceController(AppDbContext db, IWebHostEnvironment environment, ILogger<PerformanceController> logger)
        {
            _db = db;
            _resultsFile = Path.Combine(environment.ContentRootPath, "perf-results.csv");
            _logger = logger;
        }

        // GET /Performance - shows the row counts, the two forms and the saved results.
        // Used by: "Performance" in the sidebar.
        public async Task<IActionResult> Index()
        {
            var model = new PerformanceViewModel();

            await CountRowsAsync(model);
            LoadHistory(model);

            return View(model);
        }

        // POST /Performance/Run - measures the 3 ways, checks they loaded the same data, and saves the results to the CSV file.
        // Used by: the "Run test" button.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(PerformanceViewModel model)
        {
            await CountRowsAsync(model);

            if (ModelState.IsValid)
            {
                model.Results.Add(await MeasureAsync(NPlusOne, model.OrdersToLoad, model.Runs));
                model.Results.Add(await MeasureAsync(IncludeWay, model.OrdersToLoad, model.Runs));
                model.Results.Add(await MeasureAsync(SelectWay, model.OrdersToLoad, model.Runs));

                foreach (PerformanceResult result in model.Results)
                {
                    result.SameDataAsFirst = result.OrdersLoaded == model.Results[0].OrdersLoaded
                        && result.GrandTotal == model.Results[0].GrandTotal;
                }

                SaveResults(model);
            }

            LoadHistory(model);
            return View("Index", model);
        }

        // POST /Performance/Generate - deletes all data and creates new test data with SeedData.
        // Used by: the "Delete all and generate" button.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(PerformanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CountRowsAsync(model);
                LoadHistory(model);
                return View("Index", model);
            }

            try
            {
                _db.OrderItems.RemoveRange(_db.OrderItems);
                _db.Orders.RemoveRange(_db.Orders);
                _db.Customers.RemoveRange(_db.Customers);
                await _db.SaveChangesAsync();

                SeedData.AddTestData(_db, model.CustomersToGenerate);

                TempData["Success"] = "New test data was created with " + model.CustomersToGenerate + " customers.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not generate test data.");

                TempData["Error"] = "The test data could not be created. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Runs one way (1 warm-up run + the measured runs) and measures the time with a Stopwatch.
        // Used by: Run.
        private async Task<PerformanceResult> MeasureAsync(string way, int ordersToLoad, int runs)
        {
            var result = new PerformanceResult { Strategy = way };

            var times = new List<double>();

            for (int run = 0; run <= runs; run++)
            {
                _db.ChangeTracker.Clear();

                var stopwatch = Stopwatch.StartNew();

                List<OrderListItem> rows;
                if (way == NPlusOne)
                {
                    rows = await LoadWithNPlusOneAsync(ordersToLoad, result);
                }
                else if (way == IncludeWay)
                {
                    rows = await LoadWithIncludeAsync(ordersToLoad, result);
                }
                else
                {
                    rows = await LoadWithSelectAsync(ordersToLoad, result);
                }

                stopwatch.Stop();

                if (run == 0)
                {
                    continue;
                }

                times.Add(stopwatch.Elapsed.TotalMilliseconds);
                result.OrdersLoaded = rows.Count;
                result.GrandTotal = rows.Sum(r => r.Total);
            }

            result.AverageMs = times.Average();
            result.FastestMs = times.Min();
            result.SlowestMs = times.Max();

            return result;
        }

        // Way 1 (N+1): loads the orders, then each order's customer and items one by one, and counts the queries.
        // Used by: MeasureAsync.
        private async Task<List<OrderListItem>> LoadWithNPlusOneAsync(int count, PerformanceResult result)
        {
            IQueryable<Order> query = _db.Orders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Take(count);

            if (result.Sql == "")
            {
                result.Sql = query.ToQueryString();
            }

            List<Order> orders = await query.ToListAsync();

            result.QueryCount = 1;

            foreach (Order order in orders)
            {
                if (!_db.Entry(order).Reference(o => o.Customer).IsLoaded)
                {
                    await _db.Entry(order).Reference(o => o.Customer).LoadAsync();
                    result.QueryCount++;
                }

                await _db.Entry(order).Collection(o => o.Items).LoadAsync();
                result.QueryCount++;
            }

            return ToListItems(orders);
        }

        // Way 2 (Include): loads the orders with their customer and items in one query (read only).
        // Used by: MeasureAsync.
        private async Task<List<OrderListItem>> LoadWithIncludeAsync(int count, PerformanceResult result)
        {
            IQueryable<Order> query = _db.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Take(count);

            if (result.Sql == "")
            {
                result.Sql = query.ToQueryString();
            }

            List<Order> orders = await query.ToListAsync();

            result.QueryCount = 1;

            return ToListItems(orders);
        }

        // Way 3 (Select): loads only the values the list needs; the database counts the items and adds up the totals.
        // Used by: MeasureAsync.
        private async Task<List<OrderListItem>> LoadWithSelectAsync(int count, PerformanceResult result)
        {
            IQueryable<OrderListItem> query = _db.Orders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Take(count)
                .Select(o => new OrderListItem
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    CustomerName = o.Customer!.Name,
                    ItemCount = o.Items.Count,
                    Total = o.Items.Sum(i => i.Quantity * i.UnitPrice)
                });

            if (result.Sql == "")
            {
                result.Sql = query.ToQueryString();
            }

            List<OrderListItem> rows = await query.ToListAsync();

            result.QueryCount = 1;

            return rows;
        }

        // Turns full Order objects into rows for the order list.
        // Used by: LoadWithNPlusOneAsync and LoadWithIncludeAsync.
        private static List<OrderListItem> ToListItems(List<Order> orders)
        {
            var rows = new List<OrderListItem>();

            foreach (Order order in orders)
            {
                rows.Add(new OrderListItem
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    CustomerName = order.Customer?.Name ?? "",
                    ItemCount = order.Items.Count,
                    Total = order.Items.Sum(i => i.Quantity * i.UnitPrice)
                });
            }

            return rows;
        }

        // Counts the rows in the 3 tables (shown on the page and saved with the results).
        // Used by: Index, Run and Generate.
        private async Task CountRowsAsync(PerformanceViewModel model)
        {
            model.CustomerCount = await _db.Customers.CountAsync();
            model.OrderCount = await _db.Orders.CountAsync();
            model.ItemCount = await _db.OrderItems.CountAsync();
        }

        // Adds one line per way to perf-results.csv.
        // Used by: Run.
        private void SaveResults(PerformanceViewModel model)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                if (!System.IO.File.Exists(_resultsFile))
                {
                    System.IO.File.WriteAllText(_resultsFile, "Date,Strategy,OrdersLoaded,Runs,Customers,Orders,Items,AverageMs,FastestMs,SlowestMs,QueriesPerRun" + Environment.NewLine);
                }

                foreach (PerformanceResult r in model.Results)
                {
                    string line = now + "," + r.Strategy + "," + r.OrdersLoaded + "," + model.Runs + ","
                        + model.CustomerCount + "," + model.OrderCount + "," + model.ItemCount + ","
                        + r.AverageMs.ToString("0.00") + "," + r.FastestMs.ToString("0.00") + ","
                        + r.SlowestMs.ToString("0.00") + "," + r.QueryCount;
                    System.IO.File.AppendAllText(_resultsFile, line + Environment.NewLine);
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Could not save the performance results.");
                model.Message = "The results could not be saved to perf-results.csv. Is the file open in another program?";
            }
        }

        // Reads the newest 30 lines of perf-results.csv.
        // Used by: Index, Run and Generate.
        private void LoadHistory(PerformanceViewModel model)
        {
            if (!System.IO.File.Exists(_resultsFile))
            {
                return;
            }

            try
            {
                string[] lines = System.IO.File.ReadAllLines(_resultsFile);

                for (int i = lines.Length - 1; i >= 1 && model.History.Count < 30; i--)
                {
                    model.History.Add(lines[i].Split(','));
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Could not read the performance results.");
                model.Message = "The saved results could not be read from perf-results.csv. Is the file open in another program?";
            }
        }
    }
}
