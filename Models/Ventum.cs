using System;
using System.Collections.Generic;

namespace proyecto.Models;

public partial class Ventum
{
    public int Folio { get; set; }

    public int IdUsuario { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<VentaConsola> VentaConsolas { get; set; } = new List<VentaConsola>();
}
