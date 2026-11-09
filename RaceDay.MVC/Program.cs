using RaceDay.MVC.Services;


namespace RaceDay.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register MVC
            builder.Services.AddControllersWithViews();

            // Enable session storage
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.Name = "RaceDay.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy =
                    CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // Allow access to the current HTTP context
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<RaceDayApiService>();

            // Register the RaceDay API connection
            builder.Services.AddHttpClient("RaceDayApi", client =>
            {
                var apiUrl = builder.Configuration[
                    "ApiSettings:BaseUrl"
                ] ?? "https://localhost:7038/";

                client.BaseAddress = new Uri(apiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Enable session before processing MVC requests
            app.UseSession();

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