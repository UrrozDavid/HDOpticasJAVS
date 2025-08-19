using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using HDOpticasJAVS;

namespace HDOpticasJAVS.Controllers
{
    public class CitaEnLineaController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // GET: CitaEnLinea
        public ActionResult Index()
        {
            var citas = db.Cita
                .Include(c => c.Empleado)
                .Include(c => c.Usuario)        // Cliente
                .Include(c => c.Parametro)      // Estado
                .Include(c => c.Parametro1)     // Especialidad
                .Where(c => c.Estado == "A")
                .ToList();

            // Proyectar el nombre del especialista desde Usuario
            foreach (var c in citas)
            {
                var especialista = db.Usuario.FirstOrDefault(u => u.Cedula == c.Cedula_Especialista);
                c.NombreEspecialista = especialista != null
                    ? (especialista.Nombre + " " + especialista.Apellido1 + " " + especialista.Apellido2)
                    : "";
            }

            return View(citas);
        }

        // GET: CitaEnLinea/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Cita cita = db.Cita.Find(id);
            if (cita == null)
            {
                return HttpNotFound();
            }
            return View(cita);
        }

        // GET: CitaEnLinea/Create
        public ActionResult Create()
        {
            ViewBag.Cedula_Especialista = new SelectList(db.Empleado, "Cedula", "Direccion");
            ViewBag.Cedula_Usuario = new SelectList(db.Usuario, "Cedula", "Nombre");
            ViewBag.Id_EstadoCita = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 4),
                "Id_Parametro", "Nombre_Parametro"
            );

            ViewBag.Id_TipoEspecialista = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 6),"Id_Parametro", "Nombre_Parametro"
            );
            return View();
        }

        // POST: CitaEnLinea/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Cita cita)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    cita.UsuarioCreador = Session["Cedula"]?.ToString() ?? "Sistema";
                    cita.FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    db.Cita.Add(cita);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
            }

            // Reasignar los ViewBag necesarios para que los DropDown funcionen
            ViewBag.Cedula_Especialista = new SelectList(db.Empleado, "Cedula", "Direccion", cita.Cedula_Especialista);
            ViewBag.Cedula_Usuario = new SelectList(db.Usuario, "Cedula", "Nombre", cita.Cedula_Usuario);
            ViewBag.Id_EstadoCita = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 4), "Id_Parametro", "Nombre_Parametro", cita.Id_EstadoCita);
            ViewBag.Id_TipoEspecialista = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 6), "Id_Parametro", "Nombre_Parametro", cita.Id_TipoEspecialista);

            return View(cita);
        }

        // GET: CitaEnLinea/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var cita = db.Cita.Find(id);
            if (cita == null) return HttpNotFound();

            // Usuarios (clientes) activos
            var usuarios = db.Usuario
                .Where(u => u.Id_Rol == 2 && u.Estado == "A")
                .Select(u => new
                {
                    u.Cedula,
                    NombreCompleto = (u.Nombre ?? "") + " " + (u.Apellido1 ?? "") + " " + (u.Apellido2 ?? "")
                })
                .ToList();

            ViewBag.Cedula_Usuario = new SelectList(usuarios, "Cedula", "NombreCompleto", cita.Cedula_Usuario);

            // Estados de cita (catálogo)
            ViewBag.Id_EstadoCita = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 4 && p.Estado == "A"),
                "Id_Parametro", "Nombre_Parametro", cita.Id_EstadoCita
            );

            // Tipos de especialista
            ViewBag.Id_TipoEspecialista = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 6 && p.Estado == "A"),
                "Id_Parametro", "Nombre_Parametro", cita.Id_TipoEspecialista
            );

            var especialistas = (from e in db.Empleado
                                 join u in db.Usuario on e.Cedula equals u.Cedula
                                 where e.Estado == "A"
                                       && u.Estado == "A"
                                       && e.Id_Especialidad == cita.Id_TipoEspecialista
                                 select new
                                 {
                                     e.Cedula,
                                     NombreCompleto = (u.Nombre ?? "") + " " + (u.Apellido1 ?? "") + " " + (u.Apellido2 ?? "")
                                 })
                    .ToList();

            ViewBag.Especialistas = new SelectList(especialistas, "Cedula", "NombreCompleto", cita.Cedula_Especialista);

            // Estado A/I
            ViewBag.EstadosAI = new SelectList(new[]
            {
        new { Value = "A", Text = "Activo" },
        new { Value = "I", Text = "Inactivo" }
    }, "Value", "Text", cita.Estado);

            return View(cita);
        }

        // POST: CitaEnLinea/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id_Cita,Cedula_Usuario,Fecha_Cita,Hora_Cita,Id_TipoEspecialista,Cedula_Especialista,Id_EstadoCita,Estado")] Cita cita)
        {
            if (ModelState.IsValid)
            {
                var citaDb = db.Cita.Find(cita.Id_Cita);
                if (citaDb == null) return HttpNotFound();

                // Actualizar solo campos editables
                citaDb.Cedula_Usuario = cita.Cedula_Usuario;
                citaDb.Fecha_Cita = cita.Fecha_Cita;
                citaDb.Hora_Cita = cita.Hora_Cita;
                citaDb.Id_TipoEspecialista = cita.Id_TipoEspecialista;
                citaDb.Cedula_Especialista = cita.Cedula_Especialista;
                citaDb.Id_EstadoCita = cita.Id_EstadoCita;
                citaDb.Estado = cita.Estado;

                // Auditoría de edición
                citaDb.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                citaDb.UsuarioModificador = Session["Cedula"]?.ToString() ?? "Sistema";

                db.Entry(citaDb).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Exito"] = "La cita fue actualizada correctamente.";
                return RedirectToAction("Index");
            }

            // Si algo falla, recargamos combos con lo que vino del form
            var usuarios = db.Usuario
                .Where(u => u.Id_Rol == 2 && u.Estado == "A")
                .Select(u => new
                {
                    u.Cedula,
                    NombreCompleto = (u.Nombre ?? "") + " " + (u.Apellido1 ?? "") + " " + (u.Apellido2 ?? "")
                })
                .ToList();
            ViewBag.Cedula_Usuario = new SelectList(usuarios, "Cedula", "NombreCompleto", cita.Cedula_Usuario);

            ViewBag.Id_EstadoCita = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 4 && p.Estado == "A"),
                "Id_Parametro", "Nombre_Parametro", cita.Id_EstadoCita
            );

            ViewBag.Id_TipoEspecialista = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 6 && p.Estado == "A"),
                "Id_Parametro", "Nombre_Parametro", cita.Id_TipoEspecialista
            );

            var especialistas = (from e in db.Empleado
                                 join u in db.Usuario on e.Cedula equals u.Cedula
                                 where e.Estado == "A"
                                       && u.Estado == "A"
                                       && e.Id_Especialidad == cita.Id_TipoEspecialista
                                 select new
                                 {
                                     e.Cedula,
                                     NombreCompleto = (u.Nombre ?? "") + " " + (u.Apellido1 ?? "") + " " + (u.Apellido2 ?? "")
                                 })
                    .ToList();

            ViewBag.Especialistas = new SelectList(especialistas, "Cedula", "NombreCompleto", cita.Cedula_Especialista);

            ViewBag.EstadosAI = new SelectList(new[]
            {
        new { Value = "A", Text = "Activo" },
        new { Value = "I", Text = "Inactivo" }
    }, "Value", "Text", cita.Estado);

            return View(cita);
        }

        // GET: CitaEnLinea/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Cita cita = db.Cita.Find(id);
            if (cita == null)
            {
                return HttpNotFound();
            }
            return View(cita);
        }

        // POST: CitaEnLinea/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var cita = db.Cita.Find(id);
            if (cita == null)
                return HttpNotFound();

            db.Cita.Remove(cita);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        public ActionResult Calendario()
        {
            // Todas las especialidades disponibles
            var especialidades = db.Parametro
                .Where(p => p.Id_TipoParametro == 6)
                .ToList();

            ViewBag.Especialidades = new SelectList(especialidades, "Id_Parametro", "Nombre_Parametro");

            return View();
        }

        public JsonResult ObtenerEspecialistas(int idEspecialidad)
        {
            var especialistas = db.Empleado
                .Where(e => e.Id_Especialidad == idEspecialidad)
                .Join(db.Usuario,
                      e => e.Cedula,
                      u => u.Cedula,
                      (e, u) => new
                      {
                          Cedula = e.Cedula,
                          NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2
                      })
                .ToList();

            return Json(especialistas, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Crear(FormCollection collection)
        {
            try
            {
                string cedulaEspecialista = collection["Cedula_Especialista"];
                string especialidadId = collection["Id_TipoEspecialista"];
                string fecha = collection["Fecha"];
                string hora = collection["Hora"];

                // Validaciones
                if (string.IsNullOrWhiteSpace(cedulaEspecialista) ||
                    string.IsNullOrWhiteSpace(especialidadId) ||
                    string.IsNullOrWhiteSpace(fecha) ||
                    string.IsNullOrWhiteSpace(hora))
                {
                    TempData["MensajeCitaError"] = "Todos los campos son obligatorios.";
                    return RedirectToAction("Calendario");
                }

                string cedulaUsuario = Session["Cedula"] as string;
                if (cedulaUsuario == null)
                {
                    TempData["MensajeCitaError"] = "Debe iniciar sesión como cliente para agendar una cita.";
                    return RedirectToAction("Calendario");
                }

                DateTime fechaCita = DateTime.Parse(fecha);
                TimeSpan horaCita = TimeSpan.Parse(hora);
                int idEspecialidad = int.Parse(especialidadId);

                // Verificar conflicto
                bool citaExiste = db.Cita.Any(c =>
                    c.Fecha_Cita == fechaCita &&
                    c.Hora_Cita == horaCita &&
                    c.Cedula_Especialista == cedulaEspecialista &&
                    c.Estado == "A");

                if (citaExiste)
                {
                    TempData["MensajeCitaError"] = "La hora seleccionada ya está ocupada.";
                    return RedirectToAction("Calendario");
                }

                var nuevaCita = new Cita
                {
                    Cedula_Usuario = cedulaUsuario,
                    Fecha_Cita = fechaCita,
                    Hora_Cita = horaCita,
                    Id_TipoEspecialista = idEspecialidad,
                    Cedula_Especialista = cedulaEspecialista,
                    Id_EstadoCita = db.Parametro.FirstOrDefault(p => p.Nombre_Parametro == "Pendiente" && p.Id_TipoParametro == 4)?.Id_Parametro ?? 1,
                    Estado = "A",
                    UsuarioCreador = cedulaUsuario,
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TokenConfirmacion = Guid.NewGuid()
                };

                db.Cita.Add(nuevaCita);
                db.SaveChanges();

                var usuario = db.Usuario.FirstOrDefault(u => u.Cedula == cedulaUsuario);
                string correo = usuario?.Correo ?? "";
                string url = Url.Action("ConfirmarCita", "CitaEnLinea", new { id = nuevaCita.Id_Cita, token = nuevaCita.TokenConfirmacion }, protocol: Request.Url.Scheme);

                var mensaje = new MailMessage();
                mensaje.To.Add(correo);
                mensaje.Subject = "Confirmación de Cita - HD Ópticas JAVS";
                mensaje.Body = $"Hola {usuario?.Nombre},\n\nHas agendado una cita para el {fechaCita:dd/MM/yyyy} a las {horaCita}.\n\nPara confirmar tu cita, haz clic en el siguiente enlace:\n{url}\n\nGracias por confiar en nosotros.";
                mensaje.IsBodyHtml = false;
                mensaje.From = new MailAddress("hdopticasjavs@gmail.com");

                var smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("hdopticasjavs@gmail.com", "ysuk wivj qivo dacj");
                smtp.EnableSsl = true;

                smtp.Send(mensaje);

                TempData["MensajeCitaExito"] = $"La cita fue agendada correctamente para el {fechaCita:yyyy-MM-dd} a las {horaCita}.";
                return RedirectToAction("Calendario");
            }
            catch
            {
                TempData["MensajeCitaError"] = "Ocurrió un error al registrar la cita.";
                return RedirectToAction("Calendario");
            }
        }

        public ActionResult Editar(int id)
        {
            var cita = db.Cita.Find(id);
            if (cita == null)
                return HttpNotFound();

            ViewBag.Id_TipoEspecialista = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 6).ToList(),
                "Id_Parametro",
                "Nombre_Parametro",
                cita.Id_TipoEspecialista
            );

            ViewBag.Cedula_Especialista = new SelectList(
                db.Empleado.ToList(),
                "Cedula",
                "Cedula",
                cita.Cedula_Especialista
            );

            return View(cita);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Cita cita)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var citaExistente = db.Cita.Find(cita.Id_Cita);
                    if (citaExistente == null)
                    {
                        TempData["MensajeCitaError"] = "La cita no fue encontrada.";
                        return RedirectToAction("Calendario");
                    }

                    // Validación de rol (opcional)
                    var rol = Session["Rol"] as int?;
                    if (rol == 2) // Cliente
                    {
                        string cedulaSesion = Session["Cedula"]?.ToString();
                        if (citaExistente.Cedula_Usuario != cedulaSesion)
                        {
                            TempData["MensajeCitaError"] = "No tiene permiso para editar esta cita.";
                            return RedirectToAction("Calendario");
                        }
                    }

                    // Actualizar campos válidos
                    citaExistente.Fecha_Cita = cita.Fecha_Cita;
                    citaExistente.Hora_Cita = cita.Hora_Cita;
                    citaExistente.Id_TipoEspecialista = cita.Id_TipoEspecialista;
                    citaExistente.Cedula_Especialista = cita.Cedula_Especialista;

                    db.SaveChanges();
                    TempData["MensajeCitaExito"] = "La cita se actualizó correctamente.";

                    return RedirectToAction("Calendario");
                }
                catch (Exception ex)
                {
                    var detalle = ex.InnerException?.InnerException?.Message ?? ex.Message;
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + detalle);
                }
            }

            // Volver a cargar los dropdowns
            ViewBag.Id_TipoEspecialista = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 6).ToList(),
                "Id_Parametro",
                "Nombre_Parametro",
                cita.Id_TipoEspecialista
            );

            ViewBag.Cedula_Especialista = new SelectList(
                db.Empleado.ToList(),
                "Cedula",
                "Cedula",
                cita.Cedula_Especialista
            );

            return View(cita);
        }

        public ActionResult CitasDelCliente()
        {
            string cedulaUsuario = Session["Cedula"] as string;

            if (cedulaUsuario == null)
                return Content("<div class='alert alert-danger'>Debe iniciar sesión para ver sus citas.</div>");

            var citas = db.Cita
                .Where(c => c.Cedula_Usuario == cedulaUsuario && c.Estado == "A")
                .OrderByDescending(c => c.Fecha_Cita)
                .ToList();

            return PartialView("_CitasDelCliente", citas);
        }

        public ActionResult CitasDelEspecialista()
        {
            string cedulaEspecialista = Session["Cedula"] as string;

            if (cedulaEspecialista == null)
                return Content("<div class='alert alert-danger'>Debe iniciar sesión como especialista para ver sus citas.</div>");

            var citas = db.Cita
                .Include("Parametro")
                .Include("Usuario")
                .Where(c => c.Cedula_Especialista == cedulaEspecialista && c.Estado == "A")
                .OrderByDescending(c => c.Fecha_Cita)
                .ToList();

            return PartialView("_CitasDelEspecialista", citas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarCita(int idCita)
        {
            var cita = db.Cita.Find(idCita);
            string cedulaUsuario = Session["Cedula"] as string;

            if ((cita == null || cita.Cedula_Usuario != cedulaUsuario) && cita.Fecha_Cita <= DateTime.Now.AddHours(24))

            {
                TempData["MensajeCitaError"] = "No es posible cancelar la cita con menos de 24 horas de anticipación.";
                return RedirectToAction("Calendario");
            }

            cita.Estado = "I"; // Cancelada
            cita.UsuarioModificador = cedulaUsuario ?? "Cliente";
            cita.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            db.SaveChanges();

            TempData["MensajeCitaExito"] = "Cita cancelada correctamente.";
            return RedirectToAction("Calendario");
        }

        public ActionResult ConfirmarCita(int id, Guid token)
        {
            var cita = db.Cita.Find(id);

            if (cita == null || cita.TokenConfirmacion != token)
            {
                TempData["MensajeCitaError"] = "No se pudo confirmar la cita. Verifique el enlace.";
                return RedirectToAction("Calendario");
            }

            cita.Id_EstadoCita = 9; // Confirmada
            cita.UsuarioModificador = cita.Cedula_Usuario;
            cita.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            db.SaveChanges();

            TempData["MensajeCitaExito"] = "¡Cita confirmada exitosamente!";
            return RedirectToAction("Calendario");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
