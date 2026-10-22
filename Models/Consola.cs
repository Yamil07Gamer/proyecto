using System.ComponentModel.DataAnnotations;

namespace proyecto.Models;

// Tabla: consolas  (también se usa como formulario de crear / actualizar)
public class Consola
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [Display(Name = "Año")]
    [Range(1970, 2100, ErrorMessage = "El año no es válido.")]
    public int? Anio { get; set; }

    [StringLength(50)]
    public string? Modelo { get; set; }

    [Display(Name = "Generación")]
    [StringLength(20)]
    public string? Generacion { get; set; }

    [Display(Name = "Región")]
    [StringLength(50)]
    public string? Region { get; set; }

    // Ruta dentro del proyecto (/img/console/ps5.png) o URL web
    [Display(Name = "Imagen (ruta o URL)")]
    [StringLength(1000)]
    public string? Imagen { get; set; }
}
