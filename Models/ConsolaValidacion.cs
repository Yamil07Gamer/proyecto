using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace proyecto.Models;

// Validaciones y nombres del formulario de consolas (Admin/CrearConsola y Admin/EditarConsola).
// Va en un archivo aparte porque Consola.cs lo genera el scaffold:
// si se vuelve a correr, Consola.cs se reemplaza pero este archivo se conserva.
[ModelMetadataType(typeof(ConsolaValidacion))]
public partial class Consola
{
}

public class ConsolaValidacion
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

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

    [Display(Name = "Imagen (ruta o URL)")]
    [StringLength(1000)]
    public string? Imagen { get; set; }
}
