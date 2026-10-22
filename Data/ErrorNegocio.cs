namespace proyecto.Data;

// Error de una regla del negocio (correo repetido, stock insuficiente, contraseña incorrecta...).
// Con la base de datos conectada, son los THROW 500xx de los procedures.
// Los controllers lo atrapan y muestran el mensaje en la vista.
public class ErrorNegocio : Exception
{
    public ErrorNegocio(string mensaje) : base(mensaje) { }
}
