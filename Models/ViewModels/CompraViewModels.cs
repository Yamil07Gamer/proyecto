using System.ComponentModel.DataAnnotations;

namespace proyecto.Models.ViewModels;

// MiCompra/Confirmar
public class CompraViewModel
{
    public int IdConsola { get; set; }

    [Required(ErrorMessage = "Indica la cantidad.")]
    [Range(1, 100, ErrorMessage = "La cantidad debe ser entre 1 y 100.")]
    public int Cantidad { get; set; } = 1;

    // Solo para mostrar en pantalla (se recargan del stock al comprar, nunca se confía en el formulario)
    public StockListado? Producto { get; set; }
    public decimal TasaIva { get; set; } = 0.16m;
}
