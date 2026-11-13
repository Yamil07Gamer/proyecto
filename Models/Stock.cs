using System;
using System.Collections.Generic;

namespace proyecto.Models;

public partial class Stock
{
    public int Id { get; set; }

    public int IdConsola { get; set; }

    public int Existencias { get; set; }

    public decimal PrecioUnitario { get; set; }

    public virtual Consola IdConsolaNavigation { get; set; } = null!;
}
