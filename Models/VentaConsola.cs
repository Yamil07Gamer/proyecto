using System;
using System.Collections.Generic;

namespace proyecto.Models;

public partial class VentaConsola
{
    public int Id { get; set; }

    public int Folio { get; set; }

    public int IdConsola { get; set; }

    public int Cantidad { get; set; }

    public decimal Subtotal { get; set; }

    public virtual Ventum FolioNavigation { get; set; } = null!;

    public virtual Consola IdConsolaNavigation { get; set; } = null!;
}
