using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configurar la conexi�n a SQL Server
builder.Services.AddTransient<SqlConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapFallbackToPage("/CreateDatabase"); // P�gina por defecto

app.MapRazorPages();
app.MapGet("/", context =>
{
    // Redirigir a /CreateDatabase cuando se accede a la ra�z (/)
    context.Response.Redirect("/CreateDatabase");
    return Task.CompletedTask;
});
app.Run();