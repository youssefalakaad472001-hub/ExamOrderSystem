using ExamRFID.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamRFID
{
    public class Program
    {
        // Starts the app: adds MVC and the database, adds sample data on the first run, and builds the request pipeline.
        // Run by .NET when the app starts.
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (!db.Customers.Any())
                {
                    SeedData.AddTestData(db, 100);
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");

                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/HttpError", "?code={0}");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
