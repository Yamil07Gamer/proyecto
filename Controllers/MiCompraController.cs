using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using proyecto.Data;
using proyecto.Filters;
using proyecto.Models;
using proyecto.Models.ViewModels;

namespace proyecto.Controllers;

// Tienda:
//   /MiCompra             catálogo (cualquier usuario, sp_listar_stock)
//   /MiCompra/Confirmar   elegir cantidad y ver los cálculos (requiere sesión)
//   /MiCompra/Ticket      resumen de la compra (sp_venta_general)
//   /MiCompra/Historial   compras del usuario de la sesión
public class MiCompraController : Controller
{
    private readonly VentaConsolasDb _db;

    public MiCompraController(VentaConsolasDb db)
    {
        _db = db;
    }

    private int IdSesion => SesionUsuario.Id(HttpContext.Session)!.Value;

    // Catálogo: lo puede ver cualquiera, con o sin sesión
    public IActionResult Index()
    {
        return View(_db.ListarStock());
    }

    [RequiereSesion]
    [HttpGet]
    public IActionResult Confirmar(int idConsola)
    {
        var producto = _db.ObtenerStockPorConsola(idConsola);
        if (producto is null)
        {
            TempData["Error"] = "Esa consola no está disponible.";
            return RedirectToAction(nameof(Index));
        }
        if (!producto.Disponible)
        {
            TempData["Error"] = $"{producto.Consola} está agotada.";
            return RedirectToAction(nameof(Index));
        }

        return View(new CompraViewModel
        {
            IdConsola = idConsola,
            Cantidad = 1,
            Producto = producto,
            TasaIva = VentaConsolasDb.TasaIva
        });
    }

    [RequiereSesion]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirmar(CompraViewModel vm)
    {
        // El precio y las existencias siempre se vuelven a leer; no se usan los del formulario
        vm.Producto = _db.ObtenerStockPorConsola(vm.IdConsola);
        vm.TasaIva = VentaConsolasDb.TasaIva;

        if (vm.Producto is null)
        {
            TempData["Error"] = "Esa consola no está disponible.";
            return RedirectToAction(nameof(Index));
        }

        if (ModelState.IsValid && vm.Cantidad > vm.Producto.Existencias)
            ModelState.AddModelError(nameof(vm.Cantidad), $"Solo hay {vm.Producto.Existencias} unidad(es) disponibles.");

        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            // sp_venta_general: descuenta stock, crea la venta y su detalle y calcula IVA y total
            var resumen = _db.VentaGeneral(IdSesion, vm.IdConsola, vm.Cantidad);

            TempData["Ticket"] = JsonSerializer.Serialize(resumen);
            return RedirectToAction(nameof(Ticket));
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Producto = _db.ObtenerStockPorConsola(vm.IdConsola);
            return View(vm);
        }
    }

    [RequiereSesion]
    public IActionResult Ticket()
    {
        if (TempData["Ticket"] is not string json)
            return RedirectToAction(nameof(Historial));

        var resumen = JsonSerializer.Deserialize<ResumenVenta>(json);
        if (resumen is null)
            return RedirectToAction(nameof(Historial));

        return View(resumen);
    }

    [RequiereSesion]
    public IActionResult Historial()
    {
        return View(_db.ListarComprasUsuario(IdSesion));
    }
}
