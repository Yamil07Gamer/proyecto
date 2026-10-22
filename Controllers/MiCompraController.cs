using Microsoft.AspNetCore.Mvc;
using proyecto.Data;
using proyecto.Filters;
using proyecto.Models.ViewModels;

namespace proyecto.Controllers;

// Tienda:
//   /MiCompra                 catálogo (cualquier usuario)
//   /MiCompra/Confirmar       elegir cantidad y ver los cálculos (requiere sesión)
//   /MiCompra/Ticket/{folio}  resumen de una compra (sp_venta_general)
//   /MiCompra/Historial       compras del usuario de la sesión
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
        if (producto.Existencias <= 0)
        {
            TempData["Error"] = $"{producto.IdConsolaNavigation.Nombre} está agotada.";
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
            var folio = _db.VentaGeneral(IdSesion, vm.IdConsola, vm.Cantidad);

            TempData["Exito"] = "¡Compra realizada!";
            return RedirectToAction(nameof(Ticket), new { id = folio });
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Producto = _db.ObtenerStockPorConsola(vm.IdConsola);
            return vm.Producto is null ? RedirectToAction(nameof(Index)) : View(vm);
        }
    }

    // Solo se puede ver un ticket propio
    [RequiereSesion]
    public IActionResult Ticket(int id)
    {
        var venta = _db.ObtenerVenta(id, IdSesion);
        if (venta is null)
        {
            TempData["Error"] = "No se encontró esa compra.";
            return RedirectToAction(nameof(Historial));
        }

        return View(venta);
    }

    [RequiereSesion]
    public IActionResult Historial()
    {
        return View(_db.ListarComprasUsuario(IdSesion));
    }
}
