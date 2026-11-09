using PrescriptionAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();


builder.Services.AddScoped<PrescriptionDAL>();
builder.Services.AddHttpClient();
builder.Services.AddSession();

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.UseRouting();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}");

app.Run();
