using System;
using System.Collections.Generic;

namespace proyecto.Data.Entidades;

public partial class Consola
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int? Anio { get; set; }

    public string? Modelo { get; set; }

    public string? Generacion { get; set; }

    public string? Region { get; set; }

    public string? Imagen { get; set; }

    public virtual Stock? Stock { get; set; }

    public virtual ICollection<VentaConsola> VentaConsolas { get; set; } = new List<VentaConsola>();
}
