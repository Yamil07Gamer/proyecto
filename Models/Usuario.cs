namespace proyecto.Models;

// Tabla: usuario
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    // Solo se usa internamente (login / cambio de contraseña); nunca se manda a una vista
    public string Contrasenia { get; set; } = "";

    public bool EsAdmin { get; set; }
    public bool Activo { get; set; } = true;
}
