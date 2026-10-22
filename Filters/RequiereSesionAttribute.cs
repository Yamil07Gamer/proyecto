using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using proyecto.Data;

namespace proyecto.Filters;

// [RequiereSesion]: solo usuarios con sesión iniciada y activos.
// Si no hay sesión, manda al login y después regresa a la página que se pidió.
public class RequiereSesionAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        var id = SesionUsuario.Id(http.Session);

        if (id is null)
        {
            context.Result = IrAlLogin(http);
            return;
        }

        // Se vuelve a consultar al usuario por si lo dieron de baja o le cambiaron permisos
        var db = http.RequestServices.GetRequiredService<VentaConsolasDb>();
        var usuario = db.ObtenerUsuario(id.Value);

        if (usuario is null || !usuario.Activo)
        {
            SesionUsuario.Cerrar(http.Session);
            context.Result = IrAlLogin(http);
            return;
        }

        SesionUsuario.Iniciar(http.Session, usuario);
        base.OnActionExecuting(context);
    }

    protected static IActionResult IrAlLogin(HttpContext http)
    {
        var regreso = http.Request.Path + http.Request.QueryString;
        return new RedirectToActionResult("Login", "Sesion", new { returnUrl = regreso.ToString() });
    }
}

// [RequiereAdmin]: además de tener sesión, el usuario debe ser administrador.
public class RequiereAdminAttribute : RequiereSesionAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (context.Result is not null) return;   // ya se mandó al login

        if (!SesionUsuario.EsAdmin(context.HttpContext.Session))
        {
            var tempData = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>()
                .GetTempData(context.HttpContext);
            tempData["Error"] = "Solo un administrador puede entrar a esa sección.";

            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}
