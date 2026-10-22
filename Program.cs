using proyecto.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Sesión: aquí se guarda el usuario que inició sesión (id, nombre, esadmin)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Capa de datos.
// Por ahora funciona con datos de prueba en memoria (Singleton para que no se pierdan entre peticiones).
// Cuando se conecte la base de datos, la cadena de conexión se toma de appsettings.json
// ("ConnectionStrings:VentaConsolas") y se puede cambiar a AddScoped.
builder.Services.AddSingleton<VentaConsolasDb>();

// Contexto generado con el scaffold (Data/InventarioContext.cs). Desactivado por ahora.
// Como se generó con --no-onconfiguring, la conexión se configura aquí:
// builder.Services.AddDbContext<proyecto.Data.InventarioContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("VentaConsolas")));
// (requiere: using Microsoft.EntityFrameworkCore;)

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
