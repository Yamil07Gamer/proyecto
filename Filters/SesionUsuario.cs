using proyecto.Data.Entidades;

namespace proyecto.Filters;

// Lectura y escritura del usuario de la sesión.
// El id que se guarda aquí es el @id_sesion que reciben los procedures.
public static class SesionUsuario
{
    private const string ClaveId = "IdUsuario";
    private const string ClaveNombre = "NombreUsuario";
    private const string ClaveAdmin = "EsAdmin";

    public static void Iniciar(ISession sesion, Usuario usuario)
    {
        sesion.SetInt32(ClaveId, usuario.Id);
        sesion.SetString(ClaveNombre, usuario.Nombre);
        sesion.SetInt32(ClaveAdmin, usuario.Esadmin == true ? 1 : 0);
    }

    public static void Cerrar(ISession sesion) => sesion.Clear();

    public static int? Id(ISession sesion) => sesion.GetInt32(ClaveId);

    public static bool HaySesion(ISession sesion) => Id(sesion).HasValue;

    public static string Nombre(ISession sesion) => sesion.GetString(ClaveNombre) ?? "";

    public static bool EsAdmin(ISession sesion) => sesion.GetInt32(ClaveAdmin) == 1;

    // Primer nombre para el menú de perfil
    public static string NombreCorto(ISession sesion)
    {
        var nombre = Nombre(sesion).Trim();
        var espacio = nombre.IndexOf(' ');
        return espacio > 0 ? nombre[..espacio] : nombre;
    }
}
