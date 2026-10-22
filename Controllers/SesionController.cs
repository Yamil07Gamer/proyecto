using Microsoft.AspNetCore.Mvc;
using proyecto.Data;
using proyecto.Filters;
using proyecto.Models.ViewModels;

namespace proyecto.Controllers;

// Rutas:
//   /Sesion/Login
//   /Sesion/MiUsuario
//   /Sesion/MiUsuario/Actualizar
//   /Sesion/MiUsuario/Secret
// Las vistas están en carpetas con el mismo nombre dentro de Views/Sesion/
public class SesionController : Controller
{
    private const string VistaLogin = "~/Views/Sesion/Login/Index.cshtml";
    private const string VistaMiUsuario = "~/Views/Sesion/MiUsuario/Index.cshtml";
    private const string VistaActualizar = "~/Views/Sesion/MiUsuario/Actualizar/Index.cshtml";
    private const string VistaSecret = "~/Views/Sesion/MiUsuario/Secret/Index.cshtml";

    private readonly VentaConsolasDb _db;

    public SesionController(VentaConsolasDb db)
    {
        _db = db;
    }

    // /Sesion  ->  a Mi usuario si ya hay sesión, si no al login
    [HttpGet("Sesion")]
    public IActionResult Index()
    {
        return SesionUsuario.HaySesion(HttpContext.Session)
            ? RedirectToAction(nameof(MiUsuario))
            : RedirectToAction(nameof(Login));
    }


    // =============================================================
    // LOGIN
    // =============================================================

    [HttpGet("Sesion/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (SesionUsuario.HaySesion(HttpContext.Session))
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(VistaLogin, new LoginViewModel());
    }

    [HttpPost("Sesion/Login")]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(VistaLogin, vm);

        try
        {
            var usuario = _db.Login(vm.Correo.Trim(), vm.Contrasenia);

            if (usuario is null)
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                vm.Contrasenia = "";
                return View(VistaLogin, vm);
            }

            SesionUsuario.Iniciar(HttpContext.Session, usuario);
            TempData["Exito"] = $"¡Bienvenido, {usuario.Nombre}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(VistaLogin, vm);
        }
    }

    [HttpPost("Sesion/CerrarSesion")]
    [ValidateAntiForgeryToken]
    public IActionResult CerrarSesion()
    {
        SesionUsuario.Cerrar(HttpContext.Session);
        TempData["Exito"] = "Cerraste sesión.";
        return RedirectToAction("Index", "Home");
    }


    // =============================================================
    // MI USUARIO
    // =============================================================

    [RequiereSesion]
    [HttpGet("Sesion/MiUsuario")]
    public IActionResult MiUsuario()
    {
        var usuario = _db.ObtenerUsuario(IdSesion);
        if (usuario is null) return RedirectToAction(nameof(Login));

        return View(VistaMiUsuario, usuario);
    }

    // Dar de baja la propia cuenta (sp_usuario_dar_baja), confirmando con la contraseña
    [RequiereSesion]
    [HttpPost("Sesion/MiUsuario/DarDeBaja")]
    [ValidateAntiForgeryToken]
    public IActionResult DarDeBaja(string? contrasenia)
    {
        try
        {
            if (string.IsNullOrEmpty(contrasenia) || !_db.VerificarContrasenia(IdSesion, contrasenia))
            {
                TempData["Error"] = "La contraseña es incorrecta. Tu cuenta no se dio de baja.";
                return RedirectToAction(nameof(MiUsuario));
            }

            _db.DarDeBaja(IdSesion);
            SesionUsuario.Cerrar(HttpContext.Session);
            TempData["Exito"] = "Tu cuenta se dio de baja.";
            return RedirectToAction("Index", "Home");
        }
        catch (ErrorNegocio ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(MiUsuario));
        }
    }


    // =============================================================
    // MI USUARIO / ACTUALIZAR   (sp_usuario_actualizar)
    // =============================================================

    [RequiereSesion]
    [HttpGet("Sesion/MiUsuario/Actualizar")]
    public IActionResult Actualizar()
    {
        var usuario = _db.ObtenerUsuario(IdSesion);
        if (usuario is null) return RedirectToAction(nameof(Login));

        var vm = new ActualizarUsuarioViewModel
        {
            Nombre = usuario.Nombre,
            Telefono = usuario.Telefono,
            Correo = usuario.Correo ?? ""
        };
        return View(VistaActualizar, vm);
    }

    [RequiereSesion]
    [HttpPost("Sesion/MiUsuario/Actualizar")]
    [ValidateAntiForgeryToken]
    public IActionResult Actualizar(ActualizarUsuarioViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(VistaActualizar, vm);

        try
        {
            // Siempre se actualiza al usuario de la sesión, nunca a otro
            _db.ActualizarUsuario(IdSesion, vm);

            var usuario = _db.ObtenerUsuario(IdSesion);
            if (usuario is not null) SesionUsuario.Iniciar(HttpContext.Session, usuario);

            TempData["Exito"] = "Tus datos se actualizaron.";
            return RedirectToAction(nameof(MiUsuario));
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(VistaActualizar, vm);
        }
    }


    // =============================================================
    // MI USUARIO / SECRET   (cambiar contraseña)
    // =============================================================

    [RequiereSesion]
    [HttpGet("Sesion/MiUsuario/Secret")]
    public IActionResult Secret()
    {
        return View(VistaSecret, new CambiarContraseniaViewModel());
    }

    [RequiereSesion]
    [HttpPost("Sesion/MiUsuario/Secret")]
    [ValidateAntiForgeryToken]
    public IActionResult Secret(CambiarContraseniaViewModel vm)
    {
        if (ModelState.IsValid && vm.ContraseniaActual == vm.ContraseniaNueva)
            ModelState.AddModelError(nameof(vm.ContraseniaNueva), "La nueva contraseña debe ser distinta a la actual.");

        if (!ModelState.IsValid)
            return View(VistaSecret, new CambiarContraseniaViewModel());

        try
        {
            _db.CambiarContrasenia(IdSesion, vm.ContraseniaActual, vm.ContraseniaNueva);
            TempData["Exito"] = "Tu contraseña se cambió.";
            return RedirectToAction(nameof(MiUsuario));
        }
        catch (ErrorNegocio ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(VistaSecret, new CambiarContraseniaViewModel());
        }
    }


    // Id del usuario de la sesión (los filtros [RequiereSesion] garantizan que existe)
    private int IdSesion => SesionUsuario.Id(HttpContext.Session)!.Value;
}
