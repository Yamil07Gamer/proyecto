namespace proyecto.Models;

// Resumen que devuelve sp_venta_general al terminar una compra
public class ResumenVenta
{
    public int Folio { get; set; }
    public string Cliente { get; set; } = "";
    public string Consola { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal SubtotalLinea { get; set; }
    public decimal SubtotalVenta { get; set; }
    public decimal Iva { get; set; }
    public decimal TotalVenta { get; set; }
    public int ExistenciasRestantes { get; set; }
}

// Renglón del historial de compras (sp_listar_compras_usuario)
public class CompraHistorial
{
    public int Folio { get; set; }
    public string Consola { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalVenta { get; set; }
}
