using HDOpticasJAVS.Models;
using HDOpticasJAVS.Models.ViewModels;
using HDOpticasJAVS.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;


namespace HDOpticasJAVS.Controllers
{
    public class ClientesController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // GET: Clientes
        public ActionResult Index()
        {
            var clientes = db.Cliente.ToList();
            return View(clientes);
        }

        // GET: Clientes/Crear
        public ActionResult Crear()
        {
            ViewBag.Generos = new SelectList(new[] { "Masculino", "Femenino", "Otro" });
            return View();
        }

        // POST: Clientes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(ClienteViewModel model)
        {
            ViewBag.Generos = new SelectList(new[] { "Masculino", "Femenino", "Otro" });

            string cedulaNormalizada = NormalizarCedula(model.Cedula);

            // Validar duplicado por cédula normalizada
            if (db.Cliente.Any(c => c.CedulaNormalizada == cedulaNormalizada))
            {
                ModelState.AddModelError("Cedula", "Ya existe un cliente registrado con esta cédula.");
                return View(model);
            }

            // Calcular edad si es necesario
            if (model.Fecha_Nacimiento != null && model.Edad == 0)
            {
                model.Edad = CalcularEdad(model.Fecha_Nacimiento);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var nuevo = new Cliente
                {
                    Cedula = model.Cedula,
                    CedulaNormalizada = cedulaNormalizada,
                    Nombre = model.Nombre,
                    Apellido1 = model.Apellido1,
                    Apellido2 = model.Apellido2,
                    Correo = model.Correo,
                    Fecha_Nacimiento = model.Fecha_Nacimiento,
                    Edad = model.Edad,
                    Genero = model.Genero,
                    Numero_Telefono = model.Numero_Telefono,
                    Padecimiento = model.Padecimiento,
                    Activo = true
                };

                db.Cliente.Add(nuevo);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cliente registrado exitosamente.";
                return RedirectToAction("Detalles", new { cedula = nuevo.Cedula });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al registrar el cliente: " + ex.Message);
                return View(model);
            }
        }


        // GET: Clientes/Detalles
        public ActionResult Detalles(string cedula)
        {
            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.ClienteNombre = $"{cliente.Nombre} {cliente.Apellido1} {cliente.Apellido2}";
            ViewBag.Cedula = cliente.Cedula;
            ViewBag.Correo = cliente.Correo;
            ViewBag.Telefono = cliente.Numero_Telefono;
            ViewBag.Fecha_Nacimiento = cliente.Fecha_Nacimiento?.ToString("yyyy-MM-dd");
            ViewBag.Edad = cliente.Edad;
            ViewBag.Genero = cliente.Genero;
            ViewBag.Padecimiento = cliente.Padecimiento;

            return View();
        }

        // Función para normalizar cédula (remueve guiones/espacios)
        private string NormalizarCedula(string cedula)
        {
            return new string(cedula?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
        }

        // Función para calcular edad
        private int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.Today;
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }
        public ActionResult Historial(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
            {
                TempData["ErrorMessage"] = "Debe especificar una cédula válida.";
                return RedirectToAction("Index");
            }

            var rolesPermitidos = new[] { 1, 2 };

            if (Session["Rol"] == null || !rolesPermitidos.Contains(Convert.ToInt32(Session["Rol"])))
            {
                db.LogSistema.Add(new LogSistema
                {
                    Fecha = DateTime.Now,
                    Modulo = "HistorialCliente",
                    Mensaje = "Intento de acceso no autorizado al historial clínico",
                    Usuario = Session["Usuario"]?.ToString() ?? "Desconocido"
                });

                db.IntentoAccesoHistorial.Add(new IntentoAccesoHistorial
                {
                    CedulaCliente = cedula,
                    Usuario = Session["Usuario"]?.ToString() ?? "Desconocido",
                    FechaIntento = DateTime.Now,
                    RolUsuario = Session["Rol"] != null ? (int?)Convert.ToInt32(Session["Rol"]) : null,
                    AccesoAutorizado = false
                });

                db.SaveChanges();

                TempData["ErrorMessage"] = "No tiene permisos para acceder al historial clínico.";
                return RedirectToAction("Index", "Clientes");
            }

            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);

            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            var historiales = db.HistorialCliente
                .Where(h => h.Cedula_Cliente == cedula)
                .OrderByDescending(h => h.FechaRegistro)
                .Select(h => new HistorialClienteViewModel
                {
                    CedulaCliente = h.Cedula_Cliente,
                    FechaRegistro = h.FechaRegistro,
                    Antecedentes = h.Antecedentes,
                    Diagnostico = h.Diagnostico,
                    Tratamiento = h.Tratamiento,
                    Observaciones = h.Observaciones,
                    UsuarioRegistro = h.Usuario_Registro
                })
                .ToList();

            if (!historiales.Any())
            {
                TempData["ErrorMessage"] = "Este cliente no tiene historial registrado.";
                return RedirectToAction("Index");
            }

            // 🔍 Construir diccionario con las fechas de seguimiento
            var fechasSeguimiento = db.AlertaSeguimiento
                .Where(a => a.Cedula_Cliente == cedula && a.FechaAlerta != null)
                .ToList()
                .GroupBy(a => a.FechaRegistro.HasValue ? a.FechaRegistro.Value.ToString("yyyyMMddHHmmss") : null)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.FechaAlerta);

            ViewBag.FechasSeguimiento = fechasSeguimiento;
            ViewBag.NombreCliente = $"{cliente.Nombre} {cliente.Apellido1} {cliente.Apellido2}";
            ViewBag.CedulaCliente = cedula;

            return View(historiales);
        }


        public ActionResult Editar(string cedula)
        {
            int? rol = Session["Rol"] as int?;
            if (rol == null || (rol != 1 && rol != 2)) // Solo Administrador o Recepcionista
            {
                RegistrarIntentoAccesoNoAutorizado(cedula); // 👈 Registro adicional
                TempData["ErrorMessage"] = "No tiene permisos para acceder a esta funcionalidad.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(cedula))
            {
                TempData["ErrorMessage"] = "Debe especificar una cédula válida.";
                return RedirectToAction("Index");
            }

            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            var viewModel = new ClienteViewModel
            {
                Cedula = cliente.Cedula,
                Nombre = cliente.Nombre,
                Apellido1 = cliente.Apellido1,
                Apellido2 = cliente.Apellido2,
                Numero_Telefono = cliente.Numero_Telefono,
                Correo = cliente.Correo,
                Fecha_Nacimiento = cliente.Fecha_Nacimiento ?? DateTime.MinValue,
                Edad = cliente.Edad ?? 0,
                Genero = cliente.Genero,
                Padecimiento = cliente.Padecimiento
            };

            ViewBag.Generos = new SelectList(new[] { "Masculino", "Femenino", "Otro" }, cliente.Genero);
            return View(viewModel);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(ClienteViewModel model)
        {
            ViewBag.Generos = new SelectList(new[] { "Masculino", "Femenino", "Otro" }, model.Genero);

            if (!ModelState.IsValid)
                return View(model);

            int? rol = Session["Rol"] as int?;
            if (rol == null || (rol != 1 && rol != 2)) // Solo Administrador o Recepcionista
            {
                RegistrarIntentoAccesoNoAutorizado(model.Cedula); // 👈 Registro adicional
                TempData["ErrorMessage"] = "No tiene permisos para editar información de clientes.";
                return RedirectToAction("Index");
            }

            var clienteExistente = db.Cliente.FirstOrDefault(c => c.Cedula == model.Cedula);
            if (clienteExistente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            // ✅ REGISTRAR EL ESTADO ANTERIOR
            var historial = new HistorialCambiosCliente
            {
                Cedula = clienteExistente.Cedula,
                Nombre = clienteExistente.Nombre,
                Apellido1 = clienteExistente.Apellido1,
                Apellido2 = clienteExistente.Apellido2,
                Correo = clienteExistente.Correo,
                Fecha_Nacimiento = clienteExistente.Fecha_Nacimiento,
                Edad = clienteExistente.Edad,
                Genero = clienteExistente.Genero,
                Padecimiento = clienteExistente.Padecimiento,
                Numero_Telefono = clienteExistente.Numero_Telefono,
                FechaCambio = DateTime.Now,
                UsuarioModificador = Session["Usuario"]?.ToString()
            };
            db.HistorialCambiosCliente.Add(historial);

            // ✅ ACTUALIZAR CON NUEVOS DATOS
            clienteExistente.Nombre = model.Nombre;
            clienteExistente.Apellido1 = model.Apellido1;
            clienteExistente.Apellido2 = model.Apellido2;
            clienteExistente.Numero_Telefono = model.Numero_Telefono;
            clienteExistente.Correo = model.Correo;
            clienteExistente.Fecha_Nacimiento = model.Fecha_Nacimiento;
            clienteExistente.Genero = model.Genero;
            clienteExistente.Padecimiento = model.Padecimiento;

            if (model.Fecha_Nacimiento != null)
                clienteExistente.Edad = CalcularEdad(model.Fecha_Nacimiento);

            clienteExistente.UsuarioModificador = Session["Usuario"]?.ToString();
            clienteExistente.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Datos actualizados correctamente.";
                return RedirectToAction("Detalles", new { cedula = model.Cedula });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar los cambios: " + ex.Message);
                return View(model);
            }
        }

        public ActionResult RegistrarHistorial(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
            {
                TempData["ErrorMessage"] = "Debe especificar una cédula válida.";
                return RedirectToAction("Index");
            }

            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.NombreCliente = $"{cliente.Nombre} {cliente.Apellido1} {cliente.Apellido2}";

            var nuevoHistorial = new HistorialClienteViewModel
            {
                CedulaCliente = cedula
            };

            return View(nuevoHistorial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarHistorial(HistorialClienteViewModel model)
        {
            // Validación del modelo
            if (!ModelState.IsValid)
            {
                var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == model.CedulaCliente);
                ViewBag.NombreCliente = cliente != null ? $"{cliente.Nombre} {cliente.Apellido1} {cliente.Apellido2}" : "Cliente";
                return View(model);
            }

            try
            {
                // Capturar la fecha del formulario (manual porque no está en el ViewModel)
                DateTime? fechaSeguimiento = null;
                if (Request["fechaSeguimiento"] != null && DateTime.TryParse(Request["fechaSeguimiento"], out DateTime fechaForm))
                {
                    if (fechaForm.Date < DateTime.Now.Date)
                    {
                        var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == model.CedulaCliente);
                        ViewBag.NombreCliente = cliente != null ? $"{cliente.Nombre} {cliente.Apellido1} {cliente.Apellido2}" : "Cliente";
                        ViewBag.FechaInvalida = "La fecha de seguimiento no puede ser anterior a hoy.";
                        return View(model);
                    }

                    fechaSeguimiento = fechaForm;
                }

                // Registrar historial clínico
                var nuevoHistorial = new HistorialCliente
                {
                    Cedula_Cliente = model.CedulaCliente,
                    FechaRegistro = DateTime.Now,
                    Antecedentes = model.Antecedentes,
                    Diagnostico = model.Diagnostico,
                    Tratamiento = model.Tratamiento,
                    Observaciones = model.Observaciones,
                    Usuario_Registro = Session["Usuario"]?.ToString() ?? "Desconocido"
                };

                db.HistorialCliente.Add(nuevoHistorial);
                db.SaveChanges();

                // Registrar alerta solo si se indicó una fecha válida
                if (fechaSeguimiento.HasValue)
                {
                    var alerta = new AlertaSeguimiento
                    {
                        Cedula_Cliente = model.CedulaCliente,
                        FechaRegistro = nuevoHistorial.FechaRegistro,
                        FechaAlerta = fechaSeguimiento.Value,
                        Mensaje = "Seguimiento clínico programado",
                        Enviada = false,
                        MedioEnvio = "Interno"
                    };

                    db.AlertaSeguimiento.Add(alerta);
                    db.SaveChanges();
                }


                TempData["SuccessMessage"] = "Historial registrado exitosamente.";
                return RedirectToAction("Historial", new { cedula = model.CedulaCliente });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al registrar el historial: " + ex.Message;
                return View(model);
            }
        }


        public ActionResult AccesosDenegados()
        {
            // Solo Administradores pueden acceder
            if (Session["Rol"] == null || (int)Session["Rol"] != 1)
            {
                TempData["ErrorMessage"] = "Acceso denegado. Solo administradores pueden ver esta sección.";
                return RedirectToAction("Index");
            }

            var intentos = db.LogSistema
                .Where(l => l.Modulo == "HistorialCliente" && l.Mensaje.Contains("Intento de acceso no autorizado"))
                .OrderByDescending(l => l.Fecha)
                .Select(l => new IntentoAccesoHistorialViewModel
                {
                    Fecha = l.Fecha,
                    Modulo = l.Modulo,
                    Mensaje = l.Mensaje,
                    Usuario = l.Usuario
                })
                .ToList();

            return View(intentos);
        }
        public ActionResult EditarHistorial(string cedula, string fecha)
        {
            if (string.IsNullOrEmpty(cedula) || string.IsNullOrEmpty(fecha))
            {
                TempData["ErrorMessage"] = "Datos insuficientes para editar el historial.";
                return RedirectToAction("Index");
            }

            if (!DateTime.TryParse(fecha, out DateTime fechaRegistro))
            {
                TempData["ErrorMessage"] = "Fecha de historial inválida.";
                return RedirectToAction("Index");
            }

            var historial = db.HistorialCliente
     .Where(h => h.Cedula_Cliente == cedula)
     .OrderByDescending(h => h.FechaRegistro)
     .ToList()
     .FirstOrDefault(h => Math.Abs((h.FechaRegistro - fechaRegistro).TotalSeconds) < 1);



            if (historial == null)
            {
                TempData["ErrorMessage"] = "Registro no encontrado.";
                return RedirectToAction("Index");
            }

            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            ViewBag.NombreCliente = cliente != null ? $"{cliente.Nombre} {cliente.Apellido1} {cliente.Apellido2}" : "Cliente";

            // Buscar si existe alerta vinculada
            var alerta = db.AlertaSeguimiento.FirstOrDefault(a =>
                a.Cedula_Cliente == cedula &&
                a.FechaRegistro == historial.FechaRegistro);

            var viewModel = new HistorialClienteViewModel
            {
                CedulaCliente = historial.Cedula_Cliente,
                FechaRegistro = historial.FechaRegistro,
                Antecedentes = historial.Antecedentes,
                Diagnostico = historial.Diagnostico,
                Tratamiento = historial.Tratamiento,
                Observaciones = historial.Observaciones,
                UsuarioRegistro = historial.Usuario_Registro,
                FechaProximoSeguimiento = alerta?.FechaAlerta
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarHistorial(HistorialClienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.NombreCliente = "Cliente";
                return View(model);
            }

            // ============= 1) Reconstruir fecha original =============
            DateTime? fechaRegistroOriginal = null;
            string iso = Request["FechaRegistroIso"];
            string ticksStr = Request["FechaRegistroTicks"];

            if (!string.IsNullOrWhiteSpace(iso))
            {
                if (DateTime.TryParse(
                        iso,
                        null,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out DateTime parsed))
                {
                    fechaRegistroOriginal = parsed;
                }
            }
            else if (!string.IsNullOrWhiteSpace(ticksStr))
            {
                if (long.TryParse(ticksStr, out long ticks))
                    fechaRegistroOriginal = new DateTime(ticks);
            }

            if (string.IsNullOrEmpty(model.CedulaCliente) || !fechaRegistroOriginal.HasValue)
            {
                TempData["ErrorMessage"] = "Datos insuficientes para identificar el historial.";
                return RedirectToAction("Index");
            }

            // ventana ±1 segundo
            DateTime minTs = fechaRegistroOriginal.Value.AddSeconds(-1);
            DateTime maxTs = fechaRegistroOriginal.Value.AddSeconds(1);

            var historial = db.HistorialCliente.FirstOrDefault(h =>
                h.Cedula_Cliente == model.CedulaCliente &&
                h.FechaRegistro >= minTs &&
                h.FechaRegistro <= maxTs
            );

            if (historial == null)
            {
                TempData["ErrorMessage"] = "Registro no encontrado.";
                return RedirectToAction("Index");
            }

            // ============= 2) Validar fecha de seguimiento =============
            if (model.FechaProximoSeguimiento.HasValue &&
                model.FechaProximoSeguimiento.Value.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "La fecha de seguimiento debe ser hoy o futura.";
                return RedirectToAction(
                    "EditarHistorial",
                    new { cedula = model.CedulaCliente, fecha = historial.FechaRegistro.ToString("o") }
                );
            }

            // ============= 3) Actualizar historial =============
            historial.Antecedentes = model.Antecedentes;
            historial.Diagnostico = model.Diagnostico;
            historial.Tratamiento = model.Tratamiento;
            historial.Observaciones = model.Observaciones;
            db.Entry(historial).State = EntityState.Modified;

            // fecha mínima válida para columnas datetime en SQL
            DateTime minSqlDate = new DateTime(1753, 1, 1);

            // ============= 4) Preparar alerta (sin guardar aún) =============
            if (model.FechaProximoSeguimiento.HasValue)
            {
                // Normalizamos la fecha de alerta
                DateTime rawFechaAlerta = model.FechaProximoSeguimiento.Value;
                DateTime safeFechaAlerta = rawFechaAlerta < minSqlDate ? DateTime.Today : rawFechaAlerta;

                var alerta = db.AlertaSeguimiento.FirstOrDefault(a =>
                    a.Cedula_Cliente == model.CedulaCliente &&
                    a.FechaRegistro >= minTs &&
                    a.FechaRegistro <= maxTs
                );

                if (alerta != null)
                {
                    alerta.FechaAlerta = safeFechaAlerta;
                    alerta.Mensaje = "Seguimiento clínico reprogramado";
                    alerta.Enviada = false;
                    alerta.MedioEnvio = "Interno";
                    alerta.TipoAlerta = "Clinico";
                    alerta.Estado = "Pendiente";

                    db.Entry(alerta).State = EntityState.Modified;
                }
                else
                {
                    // usamos la fecha de historial; por si acaso la normalizamos
                    DateTime rawFechaRegistroHist = historial.FechaRegistro;
                    DateTime safeFechaRegistroHist = rawFechaRegistroHist < minSqlDate ? DateTime.Now : rawFechaRegistroHist;

                    var nuevaAlerta = new AlertaSeguimiento
                    {
                        Cedula_Cliente = model.CedulaCliente,
                        FechaRegistro = safeFechaRegistroHist,
                        FechaAlerta = safeFechaAlerta,
                        Mensaje = "Seguimiento clínico agregado",
                        Enviada = false,
                        MedioEnvio = "Interno",
                        TipoAlerta = "Clinico",
                        Estado = "Pendiente"
                    };

                    db.AlertaSeguimiento.Add(nuevaAlerta);
                }
            }

            // ============= 5) Guardar TODO =============
            bool oldValidateFlag = db.Configuration.ValidateOnSaveEnabled;
            db.Configuration.ValidateOnSaveEnabled = false;

            try
            {
                // 5.1 Normalizar TODAS las fechas de TODAS las entidades que se van a guardar
                foreach (var entry in db.ChangeTracker.Entries()
                                         .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
                {
                    var entity = entry.Entity;
                    var props = entity.GetType().GetProperties()
                                      .Where(p =>
                                          p.PropertyType == typeof(DateTime) ||
                                          p.PropertyType == typeof(DateTime?));

                    foreach (var prop in props)
                    {
                        var val = prop.GetValue(entity, null);
                        if (val == null) continue;

                        DateTime dt = (DateTime)val;
                        if (dt < minSqlDate)
                        {
                            // si es nullable y venía "vacía", podrías poner null,
                            // pero para evitar problemas usamos el mínimo permitido
                            prop.SetValue(entity, minSqlDate, null);
                        }
                    }
                }

                // 5.2 Guardar cambios
                db.SaveChanges();
                TempData["SuccessMessage"] = "Historial actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al guardar los cambios: " + ex.GetBaseException().Message;

                // log con contexto separado
                try
                {
                    using (var dbLog = new HD_Opticas_JAVS_BDEntities())
                    {
                        dbLog.LogSistema.Add(new LogSistema
                        {
                            Fecha = DateTime.Now,
                            Modulo = "ClientesController.EditarHistorial",
                            Mensaje = "Error general al guardar historial: " + ex.ToString(),
                            Usuario = (Session["Usuario"] ?? "Sistema").ToString()
                        });
                        dbLog.SaveChanges();
                    }
                }
                catch
                {
                    
                }
            }
            finally
            {
                db.Configuration.ValidateOnSaveEnabled = oldValidateFlag;
            }

            return RedirectToAction("Historial", "Clientes", new { cedula = model.CedulaCliente });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarHistorial(string cedula, DateTime? fecha)
        {
            if (fecha == null)
            {
                TempData["ErrorMessage"] = "Fecha inválida.";
                return RedirectToAction("Historial", new { cedula = cedula });
            }

            var historial = db.HistorialCliente
                              .ToList()
                              .FirstOrDefault(h =>
                                  h.Cedula_Cliente == cedula &&
                                  h.FechaRegistro.ToString("yyyy-MM-dd HH:mm") == fecha.Value.ToString("yyyy-MM-dd HH:mm"));

            if (historial == null)
            {
                TempData["ErrorMessage"] = "Registro no encontrado.";
                return RedirectToAction("Historial", new { cedula = cedula });
            }

            db.HistorialCliente.Remove(historial);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Historial eliminado correctamente.";
            return RedirectToAction("Historial", new { cedula = cedula });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DesactivarCliente(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
            {
                TempData["ErrorMessage"] = "Cédula no válida.";
                return RedirectToAction("Index");
            }

            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            cliente.Activo = false;
            db.SaveChanges();
            TempData["SuccessMessage"] = "Cliente desactivado correctamente.";
            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReactivarCliente(string cedula)
        {
            if (Session["Rol"] == null || (int)Session["Rol"] != 1)
            {
                TempData["ErrorMessage"] = "No tiene permisos para reactivar clientes.";
                return RedirectToAction("Index");
            }

            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            try
            {
                cliente.Activo = true;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cliente reactivado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al reactivar el cliente: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public ActionResult VerCambiosCliente(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
            {
                TempData["ErrorMessage"] = "Debe especificar una cédula válida.";
                return RedirectToAction("Index");
            }

            if (Session["Rol"] == null || (int)Session["Rol"] != 1) // Solo Admin
            {
                TempData["ErrorMessage"] = "Acceso denegado. Solo administradores pueden ver el historial de cambios.";
                return RedirectToAction("Index");
            }

            var cambios = db.HistorialCambiosCliente
                .Where(h => h.Cedula == cedula)
                .OrderByDescending(h => h.FechaCambio)
                .ToList();

            if (!cambios.Any())
            {
                TempData["ErrorMessage"] = "Este cliente no tiene historial de cambios registrado.";
                return RedirectToAction("Index");
            }

            ViewBag.NombreCliente = db.Cliente
                .Where(c => c.Cedula == cedula)
                .Select(c => c.Nombre + " " + c.Apellido1 + " " + c.Apellido2)
                .FirstOrDefault() ?? "Cliente";

            ViewBag.Cedula = cedula;

            return View(cambios);
        }
        private void RegistrarIntentoAccesoNoAutorizado(string cedula)
        {
            var intento = new IntentoAccesoHistorial
            {
                CedulaCliente = cedula,
                FechaIntento = DateTime.Now,
                Usuario = Session["Usuario"]?.ToString() ?? "Desconocido",
                Motivo = "Intento de editar cliente sin permisos"
            };

            db.IntentoAccesoHistorial.Add(intento);
            db.SaveChanges();
        }
        public void EnviarAlertaSeguimientoEmail(string correoDestino, string nombreCliente, DateTime fechaCita)
        {
            if (string.IsNullOrEmpty(correoDestino) || string.IsNullOrEmpty(nombreCliente))
                throw new ArgumentException("Datos incompletos para enviar el correo.");

            var fromAddress = new MailAddress("hdopticasjavs@gmail.com", "Soporte HD Ópticas JAVS");
            var toAddress = new MailAddress(correoDestino);
            const string fromPassword = "ysuk wivj qivo dacj"; // Contraseña de app
            const string subject = "📅 Recordatorio de Cita - HD Ópticas JAVS";

            string body = $@"
<h3>Estimado/a {nombreCliente},</h3>
<p>Este es un recordatorio de su próxima cita de seguimiento agendada para:</p>
<h4 style='color:darkblue'>{fechaCita:dddd, dd MMMM yyyy - hh:mm tt}</h4>
<p>Por favor confirme su asistencia o contáctenos si desea reprogramar.</p>
<br>
<p style='color:gray'>Este es un mensaje automático de HD Ópticas JAVS.</p>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            })
            {
                smtp.Send(message);
            }
        }

        public void RevisarAlertasPendientes()
        {
            var hoy = DateTime.Today;
            var anticipacion = GetAnticipacionDias();
            var limite = hoy.AddDays(anticipacion);

            // Buscar alertas pendientes cuya FechaAlerta esté entre hoy y hoy+anticipación
            var alertas = db.AlertaSeguimiento
                .Where(a => a.Estado == "Pendiente"
                         && DbFunctions.TruncateTime(a.FechaAlerta) >= hoy
                         && DbFunctions.TruncateTime(a.FechaAlerta) <= limite)
                .ToList();

            foreach (var alerta in alertas)
            {
                bool enviada = false;
                string mensaje = "";

                try
                {
                    var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == alerta.Cedula_Cliente);
                    if (cliente != null && !string.IsNullOrEmpty(cliente.Correo))
                    {
                        EnviarAlertaSeguimientoEmail(cliente.Correo, cliente.Nombre + " " + cliente.Apellido1, alerta.FechaAlerta);

                        alerta.Estado = "Enviado";
                        alerta.FechaEnvio = DateTime.Now;
                        alerta.Mensaje = "Recordatorio enviado exitosamente";

                        enviada = true;
                        mensaje = "Recordatorio enviado exitosamente";
                    }
                    else
                    {
                        alerta.Estado = "Fallido";
                        alerta.Mensaje = "Correo no disponible para el cliente";
                        alerta.FechaEnvio = DateTime.Now;

                        enviada = false;
                        mensaje = "Correo no disponible para el cliente";
                    }
                }
                catch (Exception ex)
                {
                    alerta.Estado = "Fallido";
                    alerta.Mensaje = $"Error: {ex.Message}";
                    alerta.FechaEnvio = DateTime.Now;

                    enviada = false;
                    mensaje = $"Error: {ex.Message}";
                }

                // Guardar cambios de estado en AlertaSeguimiento
                db.Entry(alerta).State = EntityState.Modified;

                // Registrar en historial
                db.AlertaSeguimientoHistorial.Add(new AlertaSeguimientoHistorial
                {
                    Cedula_Cliente = alerta.Cedula_Cliente,
                    FechaAlerta = alerta.FechaAlerta,
                    Mensaje = mensaje,
                    Enviada = enviada,
                    MedioEnvio = "Automático"
                });

                db.SaveChanges();
            }
        }



        public ActionResult EjecutarAlertas()
        {
            RevisarAlertasPendientes();
            TempData["SuccessMessage"] = "Alertas revisadas y correos enviados (si aplicaba).";
            return RedirectToAction("Index", "Clientes");
        }
        public ActionResult HistorialAlertas(string cedula)
        {
            var historial = db.AlertaSeguimientoHistorial
                              .Where(h => h.Cedula_Cliente == cedula)
                              .OrderByDescending(h => h.FechaAlerta)
                              .Select(h => new AlertaHistorialViewModel
                              {
                                  Cedula_Cliente = h.Cedula_Cliente,
                                  FechaAlerta = h.FechaAlerta,
                                  Mensaje = h.Mensaje,
                                  Enviada = h.Enviada,
                                  MedioEnvio = h.MedioEnvio
                              })
                              .ToList();

            ViewBag.Cedula = cedula;
            return View(historial);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarAlerta(AlertaSeguimiento alerta)
        {
            
            if (alerta.FechaAlerta == default(DateTime))
            {
                TempData["ErrorMessage"] = "Debe ingresar una fecha de alerta para registrar la alerta.";
                return RedirectToAction("Historial", new { cedula = alerta.Cedula_Cliente });
            }

            
            if (alerta.FechaAlerta.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "La fecha de alerta debe ser futura o al menos hoy.";
                return RedirectToAction("Historial", new { cedula = alerta.Cedula_Cliente });
            }

            if (ModelState.IsValid)
            {
                alerta.FechaConfiguracion = DateTime.Now;
                alerta.Estado = "Pendiente";
                alerta.ConfiguradaPor = Session["Usuario"]?.ToString();

                db.AlertaSeguimiento.Add(alerta);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Alerta registrada correctamente.";
            }

            return RedirectToAction("Historial", new { cedula = alerta.Cedula_Cliente });
        }

        public ActionResult ExportarHistorialExcel(string cedula)
        {
            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Historial", new { cedula });
            }

            var historiales = db.HistorialCliente
                .Where(h => h.Cedula_Cliente == cedula)
                .OrderByDescending(h => h.FechaRegistro)
                .ToList();

            var fechasSeguimiento = db.AlertaSeguimiento
                .Where(a => a.Cedula_Cliente == cedula && a.FechaAlerta != null)
                .ToList()
                .GroupBy(a => a.FechaRegistro.HasValue ? a.FechaRegistro.Value.ToString("yyyyMMddHHmmss") : null)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.FechaAlerta);

            var data = historiales.Select(h => new
            {
                Fecha_Registro = h.FechaRegistro.ToString("yyyy-MM-dd HH:mm"),
                h.Antecedentes,
                h.Diagnostico,
                h.Tratamiento,
                h.Observaciones,
                Registrado_Por = h.Usuario_Registro,
                Proximo_Seguimiento = fechasSeguimiento.ContainsKey(h.FechaRegistro.ToString("yyyyMMddHHmmss"))
                    ? fechasSeguimiento[h.FechaRegistro.ToString("yyyyMMddHHmmss")]?.ToString("yyyy-MM-dd")
                    : ""
            }).ToList();

            var grid = new System.Web.UI.WebControls.GridView();
            grid.DataSource = data;
            grid.DataBind();

            Response.ClearContent();
            Response.AddHeader("content-disposition", $"attachment; filename=Historial_{cliente.Nombre}_{cliente.Apellido1}.xls");
            Response.ContentType = "application/excel";

            var sw = new System.IO.StringWriter();
            var htw = new System.Web.UI.HtmlTextWriter(sw);
            grid.RenderControl(htw);
            Response.Write(sw.ToString());
            Response.End();

            return new EmptyResult();
        }

        public ActionResult ExportarHistorialPdf(string cedula)
        {
            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Historial", new { cedula });
            }

            var historiales = db.HistorialCliente
                .Where(h => h.Cedula_Cliente == cedula)
                .OrderByDescending(h => h.FechaRegistro)
                .ToList();

            var fechasSeguimiento = db.AlertaSeguimiento
                .Where(a => a.Cedula_Cliente == cedula && a.FechaAlerta != null)
                .ToList()
                .GroupBy(a => a.FechaRegistro.HasValue ? a.FechaRegistro.Value.ToString("yyyyMMddHHmmss") : null)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.FechaAlerta);

            using (var ms = new System.IO.MemoryStream())
            {
                var doc = new iTextSharp.text.Document();
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD);
                var tableFont = iTextSharp.text.FontFactory.GetFont("Arial", 10);

                doc.Add(new iTextSharp.text.Paragraph("Historial Clínico del Cliente", titleFont));
                doc.Add(new iTextSharp.text.Paragraph("Nombre: " + cliente.Nombre + " " + cliente.Apellido1 + " " + cliente.Apellido2));
                doc.Add(new iTextSharp.text.Paragraph("Cédula: " + cliente.Cedula));
                doc.Add(new iTextSharp.text.Paragraph(" "));

                var table = new iTextSharp.text.pdf.PdfPTable(7);
                table.WidthPercentage = 100;
                table.AddCell("Fecha Registro");
                table.AddCell("Antecedentes");
                table.AddCell("Diagnóstico");
                table.AddCell("Tratamiento");
                table.AddCell("Observaciones");
                table.AddCell("Registrado Por");
                table.AddCell("Próximo Seguimiento");

                foreach (var h in historiales)
                {
                    table.AddCell(h.FechaRegistro.ToString("yyyy-MM-dd HH:mm") ?? "");
                    table.AddCell(h.Antecedentes);
                    table.AddCell(h.Diagnostico);
                    table.AddCell(h.Tratamiento);
                    table.AddCell(h.Observaciones);
                    table.AddCell(h.Usuario_Registro);
                    string clave = h.FechaRegistro.ToString("yyyyMMddHHmmss");
                    table.AddCell(fechasSeguimiento.ContainsKey(clave) ? fechasSeguimiento[clave]?.ToString("yyyy-MM-dd") ?? "" : "");
                }

                doc.Add(table);
                doc.Close();

                byte[] pdfBytes = ms.ToArray();
                return File(pdfBytes, "application/pdf", $"Historial_{cliente.Nombre}_{cliente.Apellido1}.pdf");
            }
        }
        public ActionResult Perfil()
        {
            string cedula = Session["Cedula"]?.ToString();

            if (string.IsNullOrEmpty(cedula))
                return RedirectToAction("Login", "Cuenta");

            var usuario = db.Usuario.FirstOrDefault(u => u.Cedula == cedula);
            if (usuario == null)
                return HttpNotFound();

            var model = new PerfilClienteViewModel
            {
                NombreUsuario = usuario.Nombre,
                HistorialCitas = db.Cita
                    .Where(c => c.Cedula_Usuario == cedula)
                    .OrderByDescending(c => c.Fecha_Cita)
                    .Select(c => new CitaViewModel
                    {
                        Fecha = c.Fecha_Cita,
                        Descripcion = c.Estado
                    }).ToList(),

                HistorialCompras = db.PuntoVenta
                    .Where(v => v.Cedula_Cliente == cedula)
                    .OrderByDescending(v => v.Fecha_Venta)
                    .Select(v => new CompraViewModel
                    {
                        Producto = db.Inventario
                                    .Where(i => i.Id_Producto == v.Id_Venta)
                                    .Select(i => i.Nombre_Producto)
                                    .FirstOrDefault(),

                        Monto = v.Total ?? 0,
                        Fecha = v.Fecha_Venta ?? DateTime.MinValue
                    }).ToList(),

                UltimaActualizacion = DateTime.Today
            };

            return View(model);
        }

        private int GetAnticipacionDias()
        {
            try
            {
                var diasDb = db.Database.SqlQuery<int?>(
                    "SELECT TOP 1 AnticipacionDias FROM ConfigNotificaciones WITH (NOLOCK)"
                ).FirstOrDefault();

                if (diasDb.HasValue && diasDb.Value > 0)
                    return diasDb.Value;
            }
            catch
            {
                
            }

            
            var val = ConfigurationManager.AppSettings["AnticipacionDias"];
            if (int.TryParse(val, out int dias) && dias > 0)
                return dias;

            
            return 3;
        }
    }

}


    
