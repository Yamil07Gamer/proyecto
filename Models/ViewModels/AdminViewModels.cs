using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace proyecto.Models.ViewModels;

// Admin/Index
public class AdminInicioViewModel
{
    public int TotalConsolas { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalAdmins { get; set; }
    public int UnidadesEnStock { get; set; }
    public List<Stock> StockBajo { get; set; } = new();
}

// Admin/CrearStock y Admin/EditarStock
public class StockFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Consola")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una consola.")]
    public int IdConsola { get; set; }

    [Required(ErrorMessage = "Las existencias son obligatorias.")]
    [Range(0, int.MaxValue, ErrorMessage = "Las existencias no pueden ser negativas.")]
    public int? Existencias { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(typeof(decimal), "0.01", "99999999", ErrorMessage = "El precio debe ser mayor a 0.")]
    [Display(Name = "Precio unitario")]
    public decimal? PrecioUnitario { get; set; }

    // Para el <select> de consolas
    public List<SelectListItem> Consolas { get; set; } = new();
}

// Admin/CambiarPermiso  (ascender o quitar admin, confirmando con la contraseña del admin)
public class CambiarPermisoViewModel
{
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string? CorreoUsuario { get; set; }

    // true = ascender a admin, false = quitar permisos
    public bool Ascender { get; set; }

    [Required(ErrorMessage = "Escribe tu contraseña para confirmar.")]
    [DataType(DataType.Password)]
    [Display(Name = "Tu contraseña")]
    public string ContraseniaAdmin { get; set; } = "";
}
