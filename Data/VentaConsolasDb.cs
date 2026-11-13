using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using proyecto.Models;
using proyecto.Models.ViewModels;

namespace proyecto.Data;

/*
 * ================================================================
 *  Capa de datos de VentaConsolas (SQL Server)
 * ================================================================
 *  - Lecturas: LINQ sobre InventarioContext (las clases del scaffold).
 *  - Altas, cambios, login y ventas: los procedures de
 *    procedures_VentaConsolas.sql, porque ahí están las validaciones
 *    (sesión, permisos de admin, stock suficiente, IVA...).
 *
 *  Los THROW 50000-59999 de los procedures llegan como ErrorNegocio
 *  y los controllers muestran el mensaje en la vista.
 * ================================================================
 */
public class VentaConsolasDb
{
    public const decimal TasaIva = 0.16m;   // IVA México, igual que sp_venta_general

    private readonly InventarioContext _ctx;

    public VentaConsolasDb(InventarioContext ctx)
    {
        _ctx = ctx;
    }


    // =============================================================
    // USUARIO
    // =============================================================

    // sp_usuario_login: null si el correo o la contraseña no coinciden
    public Usuario? Login(string correo, string contrasenia)
    {
        var id = Salida("@id", SqlDbType.Int);
        Ejecutar("sp_usuario_login",
            P("@correo", correo),
            P("@contrasenia", contrasenia),
            id);

        return id.Value is int idUsuario ? ObtenerUsuario(idUsuario) : null;
    }

    public Usuario? ObtenerUsuario(int id) =>
        _ctx.Set<Usuario>().AsNoTracking().FirstOrDefault(u => u.Id == id);

    // sp_usuario_crear  (esadmin siempre en 0)
    public int CrearUsuario(CrearUsuarioViewModel vm)
    {
        var id = Salida("@id", SqlDbType.Int);
        Ejecutar("sp_usuario_crear",
            P("@nombre", vm.Nombre.Trim()),
            P("@telefono", vm.Telefono),
            P("@correo", vm.Correo.Trim()),
            P("@contrasenia", vm.Contrasenia),
            P("@esadmin", false),
            id);
        return (int)id.Value;
    }

    // sp_usuario_actualizar: solo el propio usuario de la sesión
    public void ActualizarUsuario(int idSesion, ActualizarUsuarioViewModel vm) =>
        Ejecutar("sp_usuario_actualizar",
            P("@id_sesion", idSesion),
            P("@nombre", vm.Nombre.Trim()),
            P("@telefono", vm.Telefono),
            P("@correo", vm.Correo.Trim()));

    // sp_usuario_cambiar_contrasenia: pide la contraseña actual
    public void CambiarContrasenia(int idSesion, string actual, string nueva) =>
        Ejecutar("sp_usuario_cambiar_contrasenia",
            P("@id_sesion", idSesion),
            P("@contrasenia_actual", actual),
            P("@contrasenia_nueva", nueva));

    // sp_usuario_dar_baja: solo el propio usuario de la sesión
    public void DarDeBaja(int idSesion) =>
        Ejecutar("sp_usuario_dar_baja", P("@id_sesion", idSesion));

    // sp_usuario_verificar_contrasenia: confirma la identidad del usuario de la sesión
    public bool VerificarContrasenia(int idSesion, string contrasenia)
    {
        var valido = Salida("@valido", SqlDbType.Bit);
        Ejecutar("sp_usuario_verificar_contrasenia",
            P("@id_sesion", idSesion),
            P("@contrasenia", contrasenia),
            valido);
        return valido.Value is bool b && b;
    }

    // Lista de usuarios para la sección de permisos ([RequiereAdmin] ya validó al admin)
    public List<Usuario> ListarUsuarios() =>
        _ctx.Set<Usuario>().AsNoTracking().OrderBy(u => u.Nombre).ToList();

    // sp_usuario_ascender_admin (solo admin)
    public void AscenderAdmin(int idSesion, int id) =>
        Ejecutar("sp_usuario_ascender_admin", P("@id_sesion", idSesion), P("@id", id));

    // sp_usuario_quitar_admin (solo admin)
    public void QuitarAdmin(int idSesion, int id) =>
        Ejecutar("sp_usuario_quitar_admin", P("@id_sesion", idSesion), P("@id", id));


    // =============================================================
    // CONSOLAS
    // =============================================================

    public List<Consola> ListarConsolas() =>
        _ctx.Set<Consola>().AsNoTracking().OrderBy(c => c.Nombre).ToList();

    public Consola? ObtenerConsola(int id) =>
        _ctx.Set<Consola>().AsNoTracking().FirstOrDefault(c => c.Id == id);

    // sp_consola_crear (solo admin)
    public int CrearConsola(int idSesion, Consola c)
    {
        var id = Salida("@id", SqlDbType.Int);
        Ejecutar("sp_consola_crear",
            P("@id_sesion", idSesion),
            P("@nombre", c.Nombre.Trim()),
            P("@anio", c.Anio),
            P("@modelo", c.Modelo),
            P("@generacion", c.Generacion),
            P("@region", c.Region),
            P("@imagen", c.Imagen),
            id);
        return (int)id.Value;
    }

    // sp_consola_actualizar (solo admin)
    public void ActualizarConsola(int idSesion, Consola c) =>
        Ejecutar("sp_consola_actualizar",
            P("@id_sesion", idSesion),
            P("@id", c.Id),
            P("@nombre", c.Nombre.Trim()),
            P("@anio", c.Anio),
            P("@modelo", c.Modelo),
            P("@generacion", c.Generacion),
            P("@region", c.Region),
            P("@imagen", c.Imagen));


    // =============================================================
    // STOCK  (siempre con su consola: IdConsolaNavigation)
    // =============================================================

    private IQueryable<Stock> StockConConsola() =>
        _ctx.Set<Stock>().AsNoTracking().Include(s => s.IdConsolaNavigation);

    public List<Stock> ListarStock() =>
        StockConConsola().OrderBy(s => s.IdConsolaNavigation.Nombre).ToList();

    public Stock? ObtenerStock(int id) =>
        StockConConsola().FirstOrDefault(s => s.Id == id);

    public Stock? ObtenerStockPorConsola(int idConsola) =>
        StockConConsola().FirstOrDefault(s => s.IdConsola == idConsola);

    // sp_stock_crear (solo admin)
    public int CrearStock(int idSesion, int idConsola, int existencias, decimal precioUnitario)
    {
        var id = Salida("@id", SqlDbType.Int);
        Ejecutar("sp_stock_crear",
            P("@id_sesion", idSesion),
            P("@id_consola", idConsola),
            P("@existencias", existencias),
            P("@precio_unitario", precioUnitario),
            id);
        return (int)id.Value;
    }

    // sp_stock_actualizar (solo admin)
    public void ActualizarStock(int idSesion, int id, int idConsola, int existencias, decimal precioUnitario) =>
        Ejecutar("sp_stock_actualizar",
            P("@id_sesion", idSesion),
            P("@id", id),
            P("@id_consola", idConsola),
            P("@existencias", existencias),
            P("@precio_unitario", precioUnitario));


    // =============================================================
    // VENTA
    // =============================================================

    // sp_venta_general: descuenta stock, crea venta + detalle y calcula subtotal, IVA y total.
    // Regresa el folio de la venta nueva.
    public int VentaGeneral(int idUsuario, int idConsola, int cantidad)
    {
        var folio = new SqlParameter("@folio", SqlDbType.Int)
        {
            Direction = ParameterDirection.InputOutput,
            Value = DBNull.Value      // venta nueva
        };

        Ejecutar("sp_venta_general",
            P("@id_usuario", idUsuario),
            P("@id_consola", idConsola),
            P("@cantidad", cantidad),
            P("@tasa_iva", TasaIva),
            folio);

        return (int)folio.Value;
    }

    // Una venta con su detalle, solo si es del usuario indicado
    public Ventum? ObtenerVenta(int folio, int idUsuario) =>
        VentasConDetalle().FirstOrDefault(v => v.Folio == folio && v.IdUsuario == idUsuario);

    // Historial del usuario de la sesión
    public List<Ventum> ListarComprasUsuario(int idUsuario) =>
        VentasConDetalle()
            .Where(v => v.IdUsuario == idUsuario)
            .OrderByDescending(v => v.Folio)
            .ToList();

    private IQueryable<Ventum> VentasConDetalle() =>
        _ctx.Set<Ventum>().AsNoTracking()
            .Include(v => v.IdUsuarioNavigation)
            .Include(v => v.VentaConsolas)
                .ThenInclude(vc => vc.IdConsolaNavigation);


    // =============================================================
    // Helpers para llamar procedures
    // =============================================================

    // Arma "EXEC sp @a = @a, @b = @b OUTPUT" y lo ejecuta.
    // Los THROW 50000-59999 se convierten en ErrorNegocio.
    private void Ejecutar(string procedimiento, params SqlParameter[] parametros)
    {
        var argumentos = string.Join(", ", parametros.Select(p =>
            $"{p.ParameterName} = {p.ParameterName}" +
            (p.Direction == ParameterDirection.Input ? "" : " OUTPUT")));

        try
        {
            _ctx.Database.ExecuteSqlRaw($"EXEC {procedimiento} {argumentos}", parametros);
        }
        catch (SqlException ex) when (ex.Number >= 50000 && ex.Number < 60000)
        {
            throw new ErrorNegocio(ex.Message);
        }
    }

    // Parámetro de entrada; null o texto vacío se mandan como NULL
    private static SqlParameter P(string nombre, object? valor)
    {
        if (valor is string texto && string.IsNullOrWhiteSpace(texto)) valor = null;
        return new SqlParameter(nombre, valor ?? DBNull.Value);
    }

    private static SqlParameter Salida(string nombre, SqlDbType tipo) =>
        new(nombre, tipo) { Direction = ParameterDirection.Output };
}
