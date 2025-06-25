using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
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
            var cita = db.Cita.Include(c => c.Empleado).Include(c => c.Usuario).Include(c => c.Parametro).Include(c => c.Parametro1).Where(u => u.Estado == "A")
                             .ToList();
            return View(cita.ToList());
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
            ViewBag.Id_TipoEspecialista = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 6),
                "Id_Parametro", "Nombre_Parametro"
            );
            return View();
        }

        // POST: CitaEnLinea/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id_Cita,Cedula_Usuario,Fecha_Cita,Hora_Cita,Id_TipoEspecialista,Cedula_Especialista,Id_EstadoCita,Estado,UsuarioCreador,FechaCreacion,UsuarioModificador,FechaModificacion,TokenConfirmacion")] Cita cita)
        {
            if (ModelState.IsValid)
            {
                db.Cita.Add(cita);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Cedula_Especialista = new SelectList(db.Empleado, "Cedula", "Direccion", cita.Cedula_Especialista);
            ViewBag.Cedula_Usuario = new SelectList(db.Usuario, "Cedula", "Nombre", cita.Cedula_Usuario);
            ViewBag.Id_EstadoCita = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro", cita.Id_EstadoCita);
            ViewBag.Id_TipoEspecialista = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro", cita.Id_TipoEspecialista);
            return View(cita);
        }

        // GET: CitaEnLinea/Edit/5
        public ActionResult Edit(int? id)
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
            ViewBag.Cedula_Especialista = new SelectList(db.Empleado, "Cedula", "Direccion", cita.Cedula_Especialista);
            ViewBag.Cedula_Usuario = new SelectList(db.Usuario, "Cedula", "Nombre", cita.Cedula_Usuario);
            ViewBag.Id_EstadoCita = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 4),
                "Id_Parametro", "Nombre_Parametro"
            );
            ViewBag.Id_TipoEspecialista = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 6),
                "Id_Parametro", "Nombre_Parametro"
            );
            return View(cita);
        }

        // POST: CitaEnLinea/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id_Cita,Cedula_Usuario,Fecha_Cita,Hora_Cita,Id_TipoEspecialista,Cedula_Especialista,Id_EstadoCita,Estado,UsuarioCreador,FechaCreacion,UsuarioModificador,FechaModificacion,TokenConfirmacion")] Cita cita)
        {
            if (ModelState.IsValid)
            {
                db.Entry(cita).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Cedula_Especialista = new SelectList(db.Empleado, "Cedula", "Direccion", cita.Cedula_Especialista);
            ViewBag.Cedula_Usuario = new SelectList(db.Usuario, "Cedula", "Nombre", cita.Cedula_Usuario);
            ViewBag.Id_EstadoCita = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro", cita.Id_EstadoCita);
            ViewBag.Id_TipoEspecialista = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro", cita.Id_TipoEspecialista);
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
            Cita cita = db.Cita.Find(id);
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

            if (cita == null || cita.Cedula_Usuario != cedulaUsuario)
            {
                TempData["MensajeCitaError"] = "No se pudo cancelar la cita.";
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
