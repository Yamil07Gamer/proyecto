namespace proyecto.Models;

// Resultado de sp_listar_stock (stock + datos de la consola)
public class Stock
{
    public int Id { get; set; }
    public int IdConsola { get; set; }
    public string Consola { get; set; } = "";
    public string? Modelo { get; set; }
    public string? Imagen { get; set; }
    public int Existencias { get; set; }
    public decimal PrecioUnitario { get; set; }
    public bool Disponible => Existencias > 0;
}
