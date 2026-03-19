using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// SQLite via EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                     ?? "Data Source=users.db"));

// Register application services
builder.Services.AddScoped<IUserService, UserService>();

// CORS – allow any origin for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate + seed on startup ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Health check endpoint ─────────────────────────────────────────────────────
app.MapGet("/", () => new
{
    api = "User Management API",
    version = "1.0",
    status = "Running",
    timestamp = DateTime.UtcNow
});

app.Run();
