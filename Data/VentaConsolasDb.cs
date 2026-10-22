using proyecto.Data.Entidades;
using proyecto.Models;
using proyecto.Models.ViewModels;
// using System.Data;
// using Microsoft.Data.SqlClient;

namespace proyecto.Data;

/*
 * ================================================================
 *  Capa de datos de VentaConsolas
 * ================================================================
 *  Cada método tiene dos partes:
 *
 *   1. BASE DE DATOS (comentada): llama al procedure correspondiente
 *      de procedures_VentaConsolas.sql con Microsoft.Data.SqlClient.
 *
 *   2. DATOS DE PRUEBA (activa): listas en memoria con los mismos
 *      registros de script.sql y las mismas validaciones y mensajes
 *      de los procedures, para que las ventanas funcionen sin SQL Server.
 *
 *  Para conectar la base de datos:
 *   - Instalar el paquete Microsoft.Data.SqlClient (ver proyecto.csproj).
 *   - Descomentar los "using" de arriba, el constructor y los helpers
 *     del final, y el bloque BASE DE DATOS de cada método.
 *   - Borrar el bloque DATOS DE PRUEBA de cada método y la región
 *     "Datos de prueba" del final.
 *   - En Program.cs cambiar AddSingleton por AddScoped.
 *
 *  Usuarios de prueba (contraseñas de script.sql):
 *   - Admin:   yalexeicparedes@gmail.com / admin1234
 *   - Normal:  ana.perez@correo.com      / 1234
 * ================================================================
 */
public class VentaConsolasDb
{
    public const decimal TasaIva = 0.16m;   // IVA México, igual que sp_venta_general

    /* ===== BASE DE DATOS =====
    private readonly string _cadena;

    public VentaConsolasDb(IConfiguration configuracion)
    {
        _cadena = configuracion.GetConnectionString("VentaConsolas")
                  ?? throw new InvalidOperationException("Falta ConnectionStrings:VentaConsolas en appsettings.json");
    }
    ========================= */


    // =============================================================
    // USUARIO
    // =============================================================

    // sp_usuario_login: devuelve el usuario si correo y contraseña coinciden
    public Usuario? Login(string correo, string contrasenia)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_login");
            cmd.Parameters.AddWithValue("@correo", correo);
            cmd.Parameters.AddWithValue("@contrasenia", contrasenia);
            using var dr = cmd.ExecuteReader();
            return dr.Read() ? LeerUsuario(dr) : null;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            var u = _usuarios.FirstOrDefault(x =>
                string.Equals(x.Correo, correo, StringComparison.OrdinalIgnoreCase) &&
                x.Contrasenia == contrasenia);

            if (u is null) return null;
            if (u.Activo != true) throw new ErrorNegocio("Tu cuenta está dada de baja.");
            return Copia(u);
        }
    }

    // sp_usuario_obtener: datos del usuario (para Mi usuario y para refrescar la sesión)
    public Usuario? ObtenerUsuario(int id)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_obtener");
            cmd.Parameters.AddWithValue("@id", id);
            using var dr = cmd.ExecuteReader();
            return dr.Read() ? LeerUsuario(dr) : null;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            var u = _usuarios.FirstOrDefault(x => x.Id == id);
            return u is null ? null : Copia(u);
        }
    }

    // sp_usuario_crear  (esadmin siempre en 0)
    public int CrearUsuario(CrearUsuarioViewModel vm)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_crear");
            cmd.Parameters.AddWithValue("@nombre", vm.Nombre.Trim());
            cmd.Parameters.AddWithValue("@telefono", Valor(vm.Telefono));
            cmd.Parameters.AddWithValue("@correo", vm.Correo.Trim());
            cmd.Parameters.AddWithValue("@contrasenia", vm.Contrasenia);
            cmd.Parameters.AddWithValue("@esadmin", false);
            var id = cmd.Parameters.Add("@id", SqlDbType.Int);
            id.Direction = ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return (int)id.Value;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            if (string.IsNullOrWhiteSpace(vm.Nombre))
                throw new ErrorNegocio("El nombre del usuario es obligatorio.");
            if (CorreoOcupado(vm.Correo, null))
                throw new ErrorNegocio("Ya existe un usuario registrado con ese correo.");

            var u = new Usuario
            {
                Id = ++_ultimoUsuario,
                Nombre = vm.Nombre.Trim(),
                Telefono = Limpio(vm.Telefono),
                Correo = vm.Correo.Trim(),
                Contrasenia = vm.Contrasenia,
                Esadmin = false,
                Activo = true
            };
            _usuarios.Add(u);
            return u.Id;
        }
    }

    // sp_usuario_actualizar: solo el propio usuario de la sesión
    public void ActualizarUsuario(int idSesion, ActualizarUsuarioViewModel vm)
    {
        /* ===== BASE DE DATOS =====
        Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_actualizar");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@nombre", vm.Nombre.Trim());
            cmd.Parameters.AddWithValue("@telefono", Valor(vm.Telefono));
            cmd.Parameters.AddWithValue("@correo", vm.Correo.Trim());
            return cmd.ExecuteNonQuery();
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            var u = UsuarioDeSesion(idSesion);
            if (string.IsNullOrWhiteSpace(vm.Nombre))
                throw new ErrorNegocio("El nombre del usuario es obligatorio.");
            if (CorreoOcupado(vm.Correo, u.Id))
                throw new ErrorNegocio("Ya existe un usuario registrado con ese correo.");

            u.Nombre = vm.Nombre.Trim();
            u.Telefono = Limpio(vm.Telefono);
            u.Correo = vm.Correo.Trim();
        }
    }

    // sp_usuario_cambiar_contrasenia: pide la contraseña actual
    public void CambiarContrasenia(int idSesion, string actual, string nueva)
    {
        /* ===== BASE DE DATOS =====
        Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_cambiar_contrasenia");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@contrasenia_actual", actual);
            cmd.Parameters.AddWithValue("@contrasenia_nueva", nueva);
            return cmd.ExecuteNonQuery();
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            var u = UsuarioDeSesion(idSesion);
            if (u.Contrasenia != actual)
                throw new ErrorNegocio("La contraseña actual es incorrecta.");
            u.Contrasenia = nueva;
        }
    }

    // sp_usuario_dar_baja: solo el propio usuario de la sesión
    public void DarDeBaja(int idSesion)
    {
        /* ===== BASE DE DATOS =====
        Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_dar_baja");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            return cmd.ExecuteNonQuery();
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            var u = UsuarioDeSesion(idSesion);
            if (u.Esadmin == true && _usuarios.Count(x => x.Esadmin == true && x.Activo == true) == 1)
                throw new ErrorNegocio("No se puede dar de baja al último administrador activo.");
            u.Activo = false;
        }
    }

    // sp_usuario_verificar_contrasenia: confirma la identidad del usuario de la sesión
    public bool VerificarContrasenia(int idSesion, string contrasenia)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_verificar_contrasenia");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@contrasenia", contrasenia);
            var valido = cmd.Parameters.Add("@valido", SqlDbType.Bit);
            valido.Direction = ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return (bool)valido.Value;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            return UsuarioDeSesion(idSesion).Contrasenia == contrasenia;
        }
    }

    // sp_listar_usuarios (solo admin)
    public List<Usuario> ListarUsuarios(int idSesion)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_listar_usuarios");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            using var dr = cmd.ExecuteReader();
            var lista = new List<Usuario>();
            while (dr.Read()) lista.Add(LeerUsuario(dr));
            return lista;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            ValidarAdmin(idSesion);
            return _usuarios.OrderBy(x => x.Nombre).Select(x => Copia(x)).ToList();
        }
    }

    // sp_usuario_ascender_admin (solo admin)
    public void AscenderAdmin(int idSesion, int id)
    {
        /* ===== BASE DE DATOS =====
        Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_ascender_admin");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery();
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            ValidarAdmin(idSesion);
            var u = _usuarios.FirstOrDefault(x => x.Id == id)
                    ?? throw new ErrorNegocio("El usuario no existe.");
            if (u.Activo != true) throw new ErrorNegocio("El usuario está dado de baja.");
            u.Esadmin = true;
        }
    }

    // sp_usuario_quitar_admin (solo admin)
    public void QuitarAdmin(int idSesion, int id)
    {
        /* ===== BASE DE DATOS =====
        Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_usuario_quitar_admin");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery();
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            ValidarAdmin(idSesion);
            var u = _usuarios.FirstOrDefault(x => x.Id == id)
                    ?? throw new ErrorNegocio("El usuario no existe.");
            if (u.Esadmin == true && u.Activo == true && _usuarios.Count(x => x.Esadmin == true && x.Activo == true) == 1)
                throw new ErrorNegocio("No se puede quitar el permiso al último administrador activo.");
            u.Esadmin = false;
        }
    }


    // =============================================================
    // CONSOLAS
    // =============================================================

    // sp_listar_consolas
    public List<Consola> ListarConsolas()
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_listar_consolas");
            using var dr = cmd.ExecuteReader();
            var lista = new List<Consola>();
            while (dr.Read()) lista.Add(LeerConsola(dr));
            return lista;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            return _consolas.OrderBy(x => x.Nombre).Select(x => Copia(x)).ToList();
        }
    }

    // sp_listar_consola @id
    public Consola? ObtenerConsola(int id)
    {
        /* ===== BASE DE DATOS =====
        try
        {
            return Ejecutar(() =>
            {
                using var cn = Conectar();
                using var cmd = Procedimiento(cn, "sp_listar_consola");
                cmd.Parameters.AddWithValue("@id", id);
                using var dr = cmd.ExecuteReader();
                return dr.Read() ? LeerConsola(dr) : null;
            });
        }
        catch (ErrorNegocio)
        {
            return null;   // la consola no existe
        }
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            var c = _consolas.FirstOrDefault(x => x.Id == id);
            return c is null ? null : Copia(c);
        }
    }

    // sp_consola_crear (solo admin)
    public int CrearConsola(int idSesion, Consola c)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_consola_crear");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@nombre", c.Nombre.Trim());
            cmd.Parameters.AddWithValue("@anio", Valor(c.Anio));
            cmd.Parameters.AddWithValue("@modelo", Valor(c.Modelo));
            cmd.Parameters.AddWithValue("@generacion", Valor(c.Generacion));
            cmd.Parameters.AddWithValue("@region", Valor(c.Region));
            cmd.Parameters.AddWithValue("@imagen", Valor(c.Imagen));
            var id = cmd.Parameters.Add("@id", SqlDbType.Int);
            id.Direction = ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return (int)id.Value;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            ValidarAdmin(idSesion);
            ValidarConsola(c);
            var nueva = Copia(c);
            nueva.Id = ++_ultimaConsola;
            Normalizar(nueva);
            _consolas.Add(nueva);
            return nueva.Id;
        }
    }

    // sp_consola_actualizar (solo admin)
    public void ActualizarConsola(int idSesion, Consola c)
    {
        /* ===== BASE DE DATOS =====
        Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_consola_actualizar");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@id", c.Id);
            cmd.Parameters.AddWithValue("@nombre", c.Nombre.Trim());
            cmd.Parameters.AddWithValue("@anio", Valor(c.Anio));
            cmd.Parameters.AddWithValue("@modelo", Valor(c.Modelo));
            cmd.Parameters.AddWithValue("@generacion", Valor(c.Generacion));
            cmd.Parameters.AddWithValue("@region", Valor(c.Region));
            cmd.Parameters.AddWithValue("@imagen", Valor(c.Imagen));
            return cmd.ExecuteNonQuery();
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            ValidarAdmin(idSesion);
            var actual = _consolas.FirstOrDefault(x => x.Id == c.Id)
                         ?? throw new ErrorNegocio("La consola no existe.");
            ValidarConsola(c);

            actual.Nombre = c.Nombre;
            actual.Anio = c.Anio;
            actual.Modelo = c.Modelo;
            actual.Generacion = c.Generacion;
            actual.Region = c.Region;
            actual.Imagen = c.Imagen;
            Normalizar(actual);
        }
    }


    // =============================================================
    // STOCK
    // =============================================================

    // sp_listar_stock
    public List<StockListado> ListarStock()
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_listar_stock");
            using var dr = cmd.ExecuteReader();
            var lista = new List<StockListado>();
            while (dr.Read()) lista.Add(LeerStock(dr));
            return lista;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            return (from s in _stock
                    join c in _consolas on s.IdConsola equals c.Id
                    orderby c.Nombre
                    select new StockListado
                    {
                        Id = s.Id,
                        IdConsola = c.Id,
                        Consola = c.Nombre,
                        Modelo = c.Modelo,
                        Imagen = c.Imagen,
                        Existencias = s.Existencias,
                        PrecioUnitario = s.PrecioUnitario
                    }).ToList();
        }
    }

    // Un registro de stock (se filtra el resultado de sp_listar_stock)
    public StockListado? ObtenerStock(int id) => ListarStock().FirstOrDefault(x => x.Id == id);

    public StockListado? ObtenerStockPorConsola(int idConsola) =>
        ListarStock().FirstOrDefault(x => x.IdConsola == idConsola);

    // sp_stock_crear (solo admin)
    public int CrearStock(int idSesion, int idConsola, int existencias, decimal precioUnitario)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_stock_crear");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@id_consola", idConsola);
            cmd.Parameters.AddWithValue("@existencias", existencias);
            cmd.Parameters.AddWithValue("@precio_unitario", precioUnitario);
            var id = cmd.Parameters.Add("@id", SqlDbType.Int);
            id.Direction = ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return (int)id.Value;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            ValidarAdmin(idSesion);
            ValidarStock(null, idConsola, existencias, precioUnitario);
            var s = new RegistroStock
            {
                Id = ++_ultimoStock,
                IdConsola = idConsola,
                Existencias = existencias,
                PrecioUnitario = precioUnitario
            };
            _stock.Add(s);
            return s.Id;
        }
    }

    // sp_stock_actualizar (solo admin)
    public void ActualizarStock(int idSesion, int id, int idConsola, int existencias, decimal precioUnitario)
    {
        /* ===== BASE DE DATOS =====
        Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_stock_actualizar");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@id_consola", idConsola);
            cmd.Parameters.AddWithValue("@existencias", existencias);
            cmd.Parameters.AddWithValue("@precio_unitario", precioUnitario);
            return cmd.ExecuteNonQuery();
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            ValidarAdmin(idSesion);
            var s = _stock.FirstOrDefault(x => x.Id == id)
                    ?? throw new ErrorNegocio("El registro de stock no existe.");
            ValidarStock(id, idConsola, existencias, precioUnitario);
            s.IdConsola = idConsola;
            s.Existencias = existencias;
            s.PrecioUnitario = precioUnitario;
        }
    }


    // =============================================================
    // VENTA
    // =============================================================

    // sp_venta_general: descuenta stock, crea venta + detalle y calcula subtotal, IVA y total
    public ResumenVenta VentaGeneral(int idUsuario, int idConsola, int cantidad)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_venta_general");
            cmd.Parameters.AddWithValue("@id_usuario", idUsuario);
            cmd.Parameters.AddWithValue("@id_consola", idConsola);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            cmd.Parameters.AddWithValue("@tasa_iva", TasaIva);
            var folio = cmd.Parameters.Add("@folio", SqlDbType.Int);
            folio.Direction = ParameterDirection.InputOutput;
            folio.Value = DBNull.Value;   // venta nueva
            using var dr = cmd.ExecuteReader();
            if (!dr.Read()) throw new ErrorNegocio("No se pudo registrar la venta.");
            return new ResumenVenta
            {
                Folio = dr.GetInt32(dr.GetOrdinal("folio")),
                Cliente = dr.GetString(dr.GetOrdinal("cliente")),
                Consola = dr.GetString(dr.GetOrdinal("consola")),
                Cantidad = dr.GetInt32(dr.GetOrdinal("cantidad")),
                PrecioUnitario = dr.GetDecimal(dr.GetOrdinal("precio_unitario")),
                SubtotalLinea = dr.GetDecimal(dr.GetOrdinal("subtotal_linea")),
                SubtotalVenta = dr.GetDecimal(dr.GetOrdinal("subtotal_venta")),
                Iva = dr.GetDecimal(dr.GetOrdinal("iva")),
                TotalVenta = dr.GetDecimal(dr.GetOrdinal("total_venta")),
                ExistenciasRestantes = dr.GetInt32(dr.GetOrdinal("existencias_restantes"))
            };
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            if (cantidad <= 0) throw new ErrorNegocio("La cantidad debe ser mayor a 0.");
            var u = _usuarios.FirstOrDefault(x => x.Id == idUsuario)
                    ?? throw new ErrorNegocio("El usuario no existe.");
            if (u.Activo != true) throw new ErrorNegocio("El usuario está dado de baja.");
            var c = _consolas.FirstOrDefault(x => x.Id == idConsola)
                    ?? throw new ErrorNegocio("La consola no existe.");
            var s = _stock.FirstOrDefault(x => x.IdConsola == idConsola)
                    ?? throw new ErrorNegocio("No hay stock registrado para esa consola.");
            if (s.Existencias < cantidad)
                throw new ErrorNegocio("Stock insuficiente para la consola solicitada.");

            s.Existencias -= cantidad;

            var subtotal = s.PrecioUnitario * cantidad;
            var total = Math.Round(subtotal * (1 + TasaIva), 2);
            var venta = new RegistroVenta
            {
                Folio = ++_ultimoFolio,
                IdUsuario = idUsuario,
                IdConsola = idConsola,
                Consola = c.Nombre,
                Cantidad = cantidad,
                Subtotal = subtotal,
                Total = total
            };
            _ventas.Add(venta);

            return new ResumenVenta
            {
                Folio = venta.Folio,
                Cliente = u.Nombre,
                Consola = c.Nombre,
                Cantidad = cantidad,
                PrecioUnitario = s.PrecioUnitario,
                SubtotalLinea = subtotal,
                SubtotalVenta = subtotal,
                Iva = total - subtotal,
                TotalVenta = total,
                ExistenciasRestantes = s.Existencias
            };
        }
    }

    // sp_listar_compras_usuario: historial del usuario de la sesión
    public List<CompraHistorial> ListarComprasUsuario(int idSesion)
    {
        /* ===== BASE DE DATOS =====
        return Ejecutar(() =>
        {
            using var cn = Conectar();
            using var cmd = Procedimiento(cn, "sp_listar_compras_usuario");
            cmd.Parameters.AddWithValue("@id_sesion", idSesion);
            using var dr = cmd.ExecuteReader();
            var lista = new List<CompraHistorial>();
            while (dr.Read())
            {
                lista.Add(new CompraHistorial
                {
                    Folio = dr.GetInt32(dr.GetOrdinal("folio")),
                    Consola = dr.GetString(dr.GetOrdinal("consola")),
                    Cantidad = dr.GetInt32(dr.GetOrdinal("cantidad")),
                    Subtotal = dr.GetDecimal(dr.GetOrdinal("subtotal")),
                    TotalVenta = dr.GetDecimal(dr.GetOrdinal("total_venta"))
                });
            }
            return lista;
        });
        ========================= */

        // ===== DATOS DE PRUEBA =====
        lock (_candado)
        {
            UsuarioDeSesion(idSesion);
            return _ventas.Where(v => v.IdUsuario == idSesion)
                          .OrderByDescending(v => v.Folio)
                          .Select(v => new CompraHistorial
                          {
                              Folio = v.Folio,
                              Consola = v.Consola,
                              Cantidad = v.Cantidad,
                              Subtotal = v.Subtotal,
                              TotalVenta = v.Total
                          }).ToList();
        }
    }


    /* ===== BASE DE DATOS: helpers =====
    private SqlConnection Conectar()
    {
        var cn = new SqlConnection(_cadena);
        cn.Open();
        return cn;
    }

    private static SqlCommand Procedimiento(SqlConnection cn, string nombre) =>
        new SqlCommand(nombre, cn) { CommandType = CommandType.StoredProcedure };

    private static object Valor(object? valor) =>
        valor is string texto && string.IsNullOrWhiteSpace(texto) ? DBNull.Value : valor ?? DBNull.Value;

    // Los THROW 50000-59999 de los procedures se convierten en ErrorNegocio
    private static T Ejecutar<T>(Func<T> accion)
    {
        try
        {
            return accion();
        }
        catch (SqlException ex) when (ex.Number >= 50000 && ex.Number < 60000)
        {
            throw new ErrorNegocio(ex.Message);
        }
    }

    private static string? Texto(SqlDataReader dr, string columna)
    {
        var i = dr.GetOrdinal(columna);
        return dr.IsDBNull(i) ? null : dr.GetString(i);
    }

    private static Usuario LeerUsuario(SqlDataReader dr) => new Usuario
    {
        Id = dr.GetInt32(dr.GetOrdinal("id")),
        Nombre = dr.GetString(dr.GetOrdinal("nombre")),
        Telefono = Texto(dr, "telefono"),
        Correo = Texto(dr, "correo"),
        Esadmin = !dr.IsDBNull(dr.GetOrdinal("esadmin")) && dr.GetBoolean(dr.GetOrdinal("esadmin")),
        Activo = !dr.IsDBNull(dr.GetOrdinal("activo")) && dr.GetBoolean(dr.GetOrdinal("activo"))
    };

    private static Consola LeerConsola(SqlDataReader dr) => new Consola
    {
        Id = dr.GetInt32(dr.GetOrdinal("id")),
        Nombre = dr.GetString(dr.GetOrdinal("nombre")),
        Anio = dr.IsDBNull(dr.GetOrdinal("anio")) ? null : dr.GetInt32(dr.GetOrdinal("anio")),
        Modelo = Texto(dr, "modelo"),
        Generacion = Texto(dr, "generacion"),
        Region = Texto(dr, "region"),
        Imagen = Texto(dr, "imagen")
    };

    private static StockListado LeerStock(SqlDataReader dr) => new StockListado
    {
        Id = dr.GetInt32(dr.GetOrdinal("id")),
        IdConsola = dr.GetInt32(dr.GetOrdinal("id_consola")),
        Consola = dr.GetString(dr.GetOrdinal("consola")),
        Modelo = Texto(dr, "modelo"),
        Imagen = Texto(dr, "imagen"),
        Existencias = dr.GetInt32(dr.GetOrdinal("existencias")),
        PrecioUnitario = dr.GetDecimal(dr.GetOrdinal("precio_unitario"))
    };
    ========================= */


    #region Datos de prueba (borrar al conectar la base de datos)

    private readonly object _candado = new();

    private class RegistroStock
    {
        public int Id { get; set; }
        public int IdConsola { get; set; }
        public int Existencias { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    private class RegistroVenta
    {
        public int Folio { get; set; }
        public int IdUsuario { get; set; }
        public int IdConsola { get; set; }
        public string Consola { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }

    // Mismos registros que script.sql
    private readonly List<Usuario> _usuarios = new()
    {
        new() { Id = 1,  Nombre = "Ana Pérez",       Telefono = "9611000001", Correo = "ana.perez@correo.com",       Contrasenia = "1234" },
        new() { Id = 2,  Nombre = "Luis Hernández",  Telefono = "9611000002", Correo = "luis.hernandez@correo.com",  Contrasenia = "1234" },
        new() { Id = 3,  Nombre = "María López",     Telefono = "9611000003", Correo = "maria.lopez@correo.com",     Contrasenia = "1234" },
        new() { Id = 4,  Nombre = "Carlos Ruiz",     Telefono = "9611000004", Correo = "carlos.ruiz@correo.com",     Contrasenia = "1234" },
        new() { Id = 5,  Nombre = "Sofía Gómez",     Telefono = "9611000005", Correo = "sofia.gomez@correo.com",     Contrasenia = "1234" },
        new() { Id = 6,  Nombre = "Jorge Martínez",  Telefono = "9611000006", Correo = "jorge.martinez@correo.com",  Contrasenia = "1234" },
        new() { Id = 7,  Nombre = "Laura Sánchez",   Telefono = "9611000007", Correo = "laura.sanchez@correo.com",   Contrasenia = "1234" },
        new() { Id = 8,  Nombre = "Diego Torres",    Telefono = "9611000008", Correo = "diego.torres@correo.com",    Contrasenia = "1234" },
        new() { Id = 9,  Nombre = "Valeria Ramírez", Telefono = "9611000009", Correo = "valeria.ramirez@correo.com", Contrasenia = "1234" },
        new() { Id = 10, Nombre = "Andrés Flores",   Telefono = "9611000010", Correo = "andres.flores@correo.com",   Contrasenia = "1234" },
        new() { Id = 11, Nombre = "Yamil Alexei",    Telefono = "9611000011", Correo = "yalexeicparedes@gmail.com",  Contrasenia = "admin1234", Esadmin = true }
    };

    private readonly List<Consola> _consolas = new()
    {
        new() { Id = 1,  Nombre = "Nintendo Switch",       Anio = 2026, Modelo = "BME-S-KB6AA",   Generacion = "gen 2", Region = "europea" },
        new() { Id = 2,  Nombre = "PlayStation 5",         Anio = 2020, Modelo = "CFI-1115A",     Generacion = "gen 9", Region = "americana" },
        new() { Id = 3,  Nombre = "Xbox Series X",         Anio = 2020, Modelo = "1882",          Generacion = "gen 9", Region = "americana" },
        new() { Id = 4,  Nombre = "Nintendo Switch Lite",  Anio = 2019, Modelo = "HDH-S-BKAAA",   Generacion = "gen 1", Region = "asiatica" },
        new() { Id = 5,  Nombre = "PlayStation 4 Slim",    Anio = 2016, Modelo = "CUH-2115B",     Generacion = "gen 8", Region = "europea" },
        new() { Id = 6,  Nombre = "Xbox One S",            Anio = 2016, Modelo = "1681",          Generacion = "gen 8", Region = "americana" },
        new() { Id = 7,  Nombre = "Nintendo Switch OLED",  Anio = 2021, Modelo = "HEG-S-KABAA",   Generacion = "gen 2", Region = "asiatica" },
        new() { Id = 8,  Nombre = "PlayStation 5 Digital", Anio = 2020, Modelo = "CFI-1016B",     Generacion = "gen 9", Region = "europea" },
        new() { Id = 9,  Nombre = "Xbox Series S",         Anio = 2020, Modelo = "1883",          Generacion = "gen 9", Region = "americana" },
        new() { Id = 10, Nombre = "Steam Deck",            Anio = 2022, Modelo = "STEAMDECK-512", Generacion = "gen 3", Region = "americana" }
    };

    private readonly List<RegistroStock> _stock = new()
    {
        new() { Id = 1,  IdConsola = 1,  Existencias = 15, PrecioUnitario = 8499.00m },
        new() { Id = 2,  IdConsola = 2,  Existencias = 8,  PrecioUnitario = 10999.00m },
        new() { Id = 3,  IdConsola = 3,  Existencias = 10, PrecioUnitario = 9999.00m },
        new() { Id = 4,  IdConsola = 4,  Existencias = 20, PrecioUnitario = 5999.00m },
        new() { Id = 5,  IdConsola = 5,  Existencias = 6,  PrecioUnitario = 6499.00m },
        new() { Id = 6,  IdConsola = 6,  Existencias = 4,  PrecioUnitario = 5499.00m },
        new() { Id = 7,  IdConsola = 7,  Existencias = 12, PrecioUnitario = 8999.00m },
        new() { Id = 8,  IdConsola = 8,  Existencias = 9,  PrecioUnitario = 9499.00m },
        new() { Id = 9,  IdConsola = 9,  Existencias = 14, PrecioUnitario = 6999.00m },
        new() { Id = 10, IdConsola = 10, Existencias = 5,  PrecioUnitario = 12999.00m }
    };

    private readonly List<RegistroVenta> _ventas = new();

    private int _ultimoUsuario = 11, _ultimaConsola = 10, _ultimoStock = 10, _ultimoFolio = 0;

    // --- validaciones equivalentes a los procedures ---

    private Usuario UsuarioDeSesion(int idSesion)   // sp_validar_sesion
    {
        var u = _usuarios.FirstOrDefault(x => x.Id == idSesion && x.Activo == true);
        return u ?? throw new ErrorNegocio("Sesión no válida: el usuario no existe o está dado de baja.");
    }

    private void ValidarAdmin(int idSesion)          // sp_validar_admin
    {
        if (UsuarioDeSesion(idSesion).Esadmin != true)
            throw new ErrorNegocio("Solo un administrador puede realizar esta acción.");
    }

    private bool CorreoOcupado(string? correo, int? excepto) =>
        !string.IsNullOrWhiteSpace(correo) &&
        _usuarios.Any(x => x.Id != excepto &&
                           string.Equals(x.Correo, correo.Trim(), StringComparison.OrdinalIgnoreCase));

    private static void ValidarConsola(Consola c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre))
            throw new ErrorNegocio("El nombre de la consola es obligatorio.");
        if (c.Anio is not null && (c.Anio < 1970 || c.Anio > DateTime.Now.Year + 1))
            throw new ErrorNegocio("El año de la consola no es válido.");
    }

    private void ValidarStock(int? id, int idConsola, int existencias, decimal precio)
    {
        if (!_consolas.Any(x => x.Id == idConsola))
            throw new ErrorNegocio("La consola no existe.");
        if (_stock.Any(x => x.IdConsola == idConsola && x.Id != id))
            throw new ErrorNegocio("Ya existe un registro de stock para esa consola.");
        if (existencias < 0)
            throw new ErrorNegocio("Las existencias no pueden ser negativas.");
        if (precio <= 0)
            throw new ErrorNegocio("El precio unitario debe ser mayor a 0.");
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static void Normalizar(Consola c)
    {
        c.Nombre = c.Nombre.Trim();
        c.Modelo = Limpio(c.Modelo);
        c.Generacion = Limpio(c.Generacion);
        c.Region = Limpio(c.Region);
        c.Imagen = Limpio(c.Imagen);
    }

    // Copias para que las vistas no modifiquen las listas directamente
    private static Usuario Copia(Usuario u) => new()
    {
        Id = u.Id, Nombre = u.Nombre, Telefono = u.Telefono, Correo = u.Correo,
        Contrasenia = u.Contrasenia, Esadmin = u.Esadmin, Activo = u.Activo
    };

    private static Consola Copia(Consola c) => new()
    {
        Id = c.Id, Nombre = c.Nombre, Anio = c.Anio, Modelo = c.Modelo,
        Generacion = c.Generacion, Region = c.Region, Imagen = c.Imagen
    };

    #endregion
}
