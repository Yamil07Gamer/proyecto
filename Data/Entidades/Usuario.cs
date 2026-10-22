using System;
using System.Collections.Generic;

namespace proyecto.Data.Entidades;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Contrasenia { get; set; }

    public bool? Esadmin { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
