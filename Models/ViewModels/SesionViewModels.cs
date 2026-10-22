using System.ComponentModel.DataAnnotations;

namespace proyecto.Models.ViewModels;

// Sesion/Login
public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasenia { get; set; } = "";
}

// CrearUsuario  (esadmin no viene del formulario: siempre se crea en 0)
public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [Display(Name = "Teléfono")]
    [Phone(ErrorMessage = "El teléfono no es válido.")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(100)]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(150, MinimumLength = 4, ErrorMessage = "La contraseña debe tener al menos 4 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasenia { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Contrasenia), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarContrasenia { get; set; } = "";
}

// Sesion/MiUsuario/Actualizar  (mismos datos que sp_usuario_actualizar)
public class ActualizarUsuarioViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [Display(Name = "Teléfono")]
    [Phone(ErrorMessage = "El teléfono no es válido.")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(100)]
    public string Correo { get; set; } = "";
}

// Sesion/MiUsuario/Secret
public class CambiarContraseniaViewModel
{
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string ContraseniaActual { get; set; } = "";

    [Required(ErrorMessage = "Escribe la nueva contraseña.")]
    [StringLength(150, MinimumLength = 4, ErrorMessage = "La contraseña debe tener al menos 4 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string ContraseniaNueva { get; set; } = "";

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(ContraseniaNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar nueva contraseña")]
    public string ConfirmarContrasenia { get; set; } = "";
}
