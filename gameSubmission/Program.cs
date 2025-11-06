var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

var app = builder.Build();

// Enable static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

// Fallback to index.html for root
app.MapFallbackToFile("index.html");

app.Run();
