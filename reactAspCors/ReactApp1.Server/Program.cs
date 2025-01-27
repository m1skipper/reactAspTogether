var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// AddCors
builder.Services.AddCors(options =>
{
    options.AddPolicy("TestPolicy",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod().AllowAnyHeader();
        });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
