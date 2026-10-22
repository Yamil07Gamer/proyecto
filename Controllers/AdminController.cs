using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using proyecto.Data;
using proyecto.Filters;
using proyecto.Models;
using proyecto.Models.ViewModels;

namespace proyecto.Controllers;

// Panel de administrador: todas las acciones exigen sesión de un admin activo.
// Los procedures también validan @id_sesion, así que la regla se cumple en los dos lados.
[RequiereAdmin]
public class AdminController : Controller
{
    private readonly VentaConsolasDb _db;

    public AdminController(VentaConsolasDb db)
    {
        _db = db;
    }

    private int IdSesion => SesionUsuario.Id(HttpContext.Session)!.Value;

    // =============================================================
    // INICIO
    // =============================================================

    public IActionResult Index()
    {
        var stock = _db.ListarStock();
        var usuarios = _db.ListarUsuarios(IdSesion);

        var vm = new AdminInicioViewModel
        {
            TotalConsolas = _db.ListarConsolas().Count,
            TotalUsuarios = usuarios.Count(u => u.Activo),
            TotalAdmins = usuarios.Count(u => u.Activo && u.EsAdmin),
            UnidadesEnStock = stock.Sum(s => s.Existencias),
            StockBajo = stock.Where(s => s.Existencias <= 5).OrderBy(s => s.Existencias).ToList()
        };
        return View(vm);
    }


    // =============================================================
    // CONSOLAS   (sp_listar_consolas, sp_consola_crear, sp_consola_actualizar)
    // =============================================================

    public IActionResult Consolas()
    {
        return View(_db.ListarConsolas());
    }

    [HttpGet]
    public IActionResult CrearConsola()
    {
        ViewData["Title"] = "Nueva consola";
        return View("ConsolaForm", new Consola());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearConsola(Consola consola)
    {
        ViewData["Title"] = "Nueva consola";
        if (!ModelState.IsValid) return View("ConsolaForm", consola);

        try
        {
            var id = _db.CrearConsola(IdSesion, consola);
            TempData["Exito"] = $"Consola \"{consola.Nombre}\" creada. Ahora puedes darle stock.";
            return RedirectToAction(nameof(CrearStock), new { idConsola = id });
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("ConsolaForm", consola);
        }
    }

    [HttpGet]
    public IActionResult EditarConsola(int id)
    {
        var consola = _db.ObtenerConsola(id);
        if (consola is null)
        {
            TempData["Error"] = "La consola no existe.";
            return RedirectToAction(nameof(Consolas));
        }

        ViewData["Title"] = "Editar consola";
        return View("ConsolaForm", consola);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarConsola(Consola consola)
    {
        ViewData["Title"] = "Editar consola";
        if (!ModelState.IsValid) return View("ConsolaForm", consola);

        try
        {
            _db.ActualizarConsola(IdSesion, consola);
            TempData["Exito"] = $"Consola \"{consola.Nombre}\" actualizada.";
            return RedirectToAction(nameof(Consolas));
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("ConsolaForm", consola);
        }
    }


    // =============================================================
    // STOCK   (sp_listar_stock, sp_stock_crear, sp_stock_actualizar)
    // =============================================================

    public IActionResult Stock()
    {
        var stock = _db.ListarStock();
        var conStock = stock.Select(s => s.IdConsola).ToHashSet();
        ViewData["ConsolasSinStock"] = _db.ListarConsolas().Count(c => !conStock.Contains(c.Id));
        return View(stock);
    }

    [HttpGet]
    public IActionResult CrearStock(int? idConsola)
    {
        var vm = new StockFormViewModel { IdConsola = idConsola ?? 0, Existencias = 0 };
        LlenarConsolas(vm, soloSinStock: true);

        if (vm.Consolas.Count == 0)
        {
            TempData["Error"] = "Todas las consolas ya tienen stock. Edita su registro o crea una consola nueva.";
            return RedirectToAction(nameof(Stock));
        }

        ViewData["Title"] = "Nuevo stock";
        return View("StockForm", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearStock(StockFormViewModel vm)
    {
        ViewData["Title"] = "Nuevo stock";
        if (!ModelState.IsValid)
        {
            LlenarConsolas(vm, soloSinStock: true);
            return View("StockForm", vm);
        }

        try
        {
            _db.CrearStock(IdSesion, vm.IdConsola, vm.Existencias!.Value, vm.PrecioUnitario!.Value);
            TempData["Exito"] = "Stock registrado.";
            return RedirectToAction(nameof(Stock));
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            LlenarConsolas(vm, soloSinStock: true);
            return View("StockForm", vm);
        }
    }

    [HttpGet]
    public IActionResult EditarStock(int id)
    {
        var s = _db.ObtenerStock(id);
        if (s is null)
        {
            TempData["Error"] = "El registro de stock no existe.";
            return RedirectToAction(nameof(Stock));
        }

        var vm = new StockFormViewModel
        {
            Id = s.Id,
            IdConsola = s.IdConsola,
            Existencias = s.Existencias,
            PrecioUnitario = s.PrecioUnitario
        };
        LlenarConsolas(vm, soloSinStock: false);

        ViewData["Title"] = "Editar stock";
        return View("StockForm", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarStock(StockFormViewModel vm)
    {
        ViewData["Title"] = "Editar stock";
        if (!ModelState.IsValid)
        {
            LlenarConsolas(vm, soloSinStock: false);
            return View("StockForm", vm);
        }

        try
        {
            _db.ActualizarStock(IdSesion, vm.Id, vm.IdConsola, vm.Existencias!.Value, vm.PrecioUnitario!.Value);
            TempData["Exito"] = "Stock actualizado.";
            return RedirectToAction(nameof(Stock));
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            LlenarConsolas(vm, soloSinStock: false);
            return View("StockForm", vm);
        }
    }

    // Opciones del <select>. Al crear solo salen consolas sin stock (hay un registro por consola).
    private void LlenarConsolas(StockFormViewModel vm, bool soloSinStock)
    {
        var conStock = _db.ListarStock()
                          .Where(s => s.Id != vm.Id)
                          .Select(s => s.IdConsola)
                          .ToHashSet();

        vm.Consolas = _db.ListarConsolas()
            .Where(c => !conStock.Contains(c.Id) || (!soloSinStock && c.Id == vm.IdConsola))
            .Select(c => new SelectListItem($"{c.Nombre} ({c.Modelo})", c.Id.ToString(), c.Id == vm.IdConsola))
            .ToList();
    }


    // =============================================================
    // USUARIOS   (sp_listar_usuarios, sp_usuario_ascender_admin, sp_usuario_quitar_admin)
    // =============================================================

    public IActionResult Usuarios()
    {
        ViewData["IdSesion"] = IdSesion;
        return View(_db.ListarUsuarios(IdSesion));
    }

    // Ventana de confirmación: pide la contraseña del admin que hace el cambio
    [HttpGet]
    public IActionResult CambiarPermiso(int id, bool ascender)
    {
        var usuario = _db.ObtenerUsuario(id);
        if (usuario is null)
        {
            TempData["Error"] = "El usuario no existe.";
            return RedirectToAction(nameof(Usuarios));
        }

        if (usuario.EsAdmin == ascender)
        {
            TempData["Error"] = ascender
                ? $"{usuario.Nombre} ya es administrador."
                : $"{usuario.Nombre} no es administrador.";
            return RedirectToAction(nameof(Usuarios));
        }

        return View(new CambiarPermisoViewModel
        {
            IdUsuario = usuario.Id,
            NombreUsuario = usuario.Nombre,
            CorreoUsuario = usuario.Correo,
            Ascender = ascender
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarPermiso(CambiarPermisoViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (!_db.VerificarContrasenia(IdSesion, vm.ContraseniaAdmin))
        {
            ModelState.AddModelError(nameof(vm.ContraseniaAdmin), "La contraseña es incorrecta.");
            return View(vm);
        }

        try
        {
            if (vm.Ascender)
            {
                _db.AscenderAdmin(IdSesion, vm.IdUsuario);
                TempData["Exito"] = $"{vm.NombreUsuario} ahora es administrador.";
            }
            else
            {
                _db.QuitarAdmin(IdSesion, vm.IdUsuario);
                TempData["Exito"] = $"Se quitaron los permisos de administrador a {vm.NombreUsuario}.";

                // Si el admin se quitó los permisos a sí mismo, ya no puede seguir en el panel
                if (vm.IdUsuario == IdSesion)
                {
                    var yo = _db.ObtenerUsuario(IdSesion);
                    if (yo is not null) SesionUsuario.Iniciar(HttpContext.Session, yo);
                    return RedirectToAction("Index", "Home");
                }
            }
            return RedirectToAction(nameof(Usuarios));
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }
}
