using Microsoft.EntityFrameworkCore;
using Paintings.Data;
using Paintings.Services;

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient<SupabaseStorageService>();
var connectionString = builder.Configuration.GetConnectionString("PaintingsDb")
    ?? throw new InvalidOperationException("Set ConnectionStrings__PaintingsDb to your Supabase PostgreSQL connection string.");
connectionString = PostgresConnectionString.Normalize(connectionString);

builder.Services.AddDbContext<PaintingContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthorization();

app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();
