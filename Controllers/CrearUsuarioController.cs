using Microsoft.AspNetCore.Mvc;
using proyecto.Data;
using proyecto.Filters;
using proyecto.Models.ViewModels;

namespace proyecto.Controllers;

// /CrearUsuario  (se llega desde el botón "Crear usuario" del login)
public class CrearUsuarioController : Controller
{
    private readonly VentaConsolasDb _db;

    public CrearUsuarioController(VentaConsolasDb db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (SesionUsuario.HaySesion(HttpContext.Session))
            return RedirectToAction("MiUsuario", "Sesion");

        return View(new CrearUsuarioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(CrearUsuarioViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            // sp_usuario_crear con @esadmin = 0 (nunca se toma del formulario)
            _db.CrearUsuario(vm);

            TempData["Exito"] = "Tu cuenta se creó. Ya puedes iniciar sesión.";
            return RedirectToAction("Login", "Sesion");
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }
}
