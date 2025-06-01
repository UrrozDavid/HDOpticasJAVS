using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;
using HDOpticasJAVS;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Controllers
{
    public class UsuarioController : Controller
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // GET: Usuario
        public ActionResult Index()
        {
            var usuarios = db.Usuario
                             .Include(u => u.Parametro)
                             .Where(u => u.Estado == "A")
                             .ToList();
            return View(usuarios);
        }

        // GET: Usuario/CrearCliente
        public ActionResult CrearCliente()
        {
            ViewBag.Generos = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 2 && p.Estado == "A"), "Id_Parametro", "Nombre_Parametro");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarCliente(FormCollection form)
        {
            try
            {
                string cedula = form["Cedula"];
                string correo = form["Correo"];

                if (db.Usuario.Any(u => u.Cedula == cedula || u.Correo == correo))
                {
                    ViewBag.Mensaje = "Ya existe un usuario con esa cédula o correo.";
                    ViewBag.Generos = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 2 && p.Estado == "A"), "Id_Parametro", "Nombre_Parametro");
                    return View("CrearCliente");
                }

                Usuario nuevoUsuario = new Usuario
                {
                    Cedula = cedula,
                    Nombre = form["Nombre"],
                    Apellido1 = form["Apellido1"],
                    Apellido2 = form["Apellido2"],
                    Correo = correo,
                    Contrasenia = null,
                    FechaNacimiento = Convert.ToDateTime(form["FechaNacimiento"]),
                    Id_Rol = 2, // Rol Cliente
                    Estado = "A",
                    UsuarioCreador = Session["Cedula"]?.ToString() ?? "Sistema",
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.Usuario.Add(nuevoUsuario);

                Cliente nuevoCliente = new Cliente
                {
                    Cedula = cedula,
                    Edad = int.Parse(form["Edad"]),
                    Id_Genero = int.Parse(form["Id_Genero"]),
                    Padecimiento = form["Padecimiento"],
                    Numero_Telefono = form["Numero_Telefono"],
                    Estado = "A",
                    UsuarioCreador = Session["Cedula"]?.ToString() ?? "Sistema",
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.Cliente.Add(nuevoCliente);
                db.SaveChanges();

                EnviarCorreoRecuperacion(correo);

                TempData["Exito"] = "Usuario cliente creado exitosamente. Se ha enviado un correo para establecer la contraseña.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Error al registrar: " + ex.Message;
                ViewBag.Generos = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 2 && p.Estado == "A"), "Id_Parametro", "Nombre_Parametro");
                return View("CrearCliente");
            }
        }

        // GET: Usuario/CrearEmpleado
        public ActionResult CrearEmpleado()
        {
            ViewBag.Roles = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 1 && p.Estado == "A"), "Id_Parametro", "Nombre_Parametro");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarEmpleado(FormCollection form)
        {
            try
            {
                string cedula = form["Cedula"];
                string correo = form["Correo"];

                if (db.Usuario.Any(u => u.Cedula == cedula || u.Correo == correo))
                {
                    ViewBag.Mensaje = "Ya existe un usuario con esa cédula o correo.";
                    ViewBag.Roles = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 1 && p.Estado == "A"), "Id_Parametro", "Nombre_Parametro");
                    return View("CrearEmpleado");
                }

                Usuario nuevoUsuario = new Usuario
                {
                    Cedula = cedula,
                    Nombre = form["Nombre"],
                    Apellido1 = form["Apellido1"],
                    Apellido2 = form["Apellido2"],
                    Correo = correo,
                    Contrasenia = null,
                    FechaNacimiento = Convert.ToDateTime(form["FechaNacimiento"]),
                    Id_Rol = int.Parse(form["Id_Rol"]),
                    Estado = "A",
                    UsuarioCreador = Session["Cedula"]?.ToString() ?? "Sistema",
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.Usuario.Add(nuevoUsuario);

                Empleado nuevoEmpleado = new Empleado
                {
                    Cedula = cedula,
                    Direccion = form["Direccion"],
                    Placa_Vehiculo = form["Placa_Vehiculo"],
                    Numero_Telefono = form["Numero_Telefono"],
                    Contacto_Emergencia = form["Contacto_Emergencia"],
                    Estado = "A",
                    UsuarioCreador = Session["Cedula"]?.ToString() ?? "Sistema",
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.Empleado.Add(nuevoEmpleado);
                db.SaveChanges();

                EnviarCorreoRecuperacion(correo);

                TempData["Exito"] = "Usuario empleado creado exitosamente. Se ha enviado un correo para establecer la contraseña.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Error al registrar: " + ex.Message;
                ViewBag.Roles = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 1 && p.Estado == "A"), "Id_Parametro", "Nombre_Parametro");
                return View("CrearEmpleado");
            }
        }

        // GET: Usuario/Editar/5
        public ActionResult Editar(string cedula)
        {
            if (cedula == null) return HttpNotFound();

            var usuario = db.Usuario.Find(cedula);
            if (usuario == null || usuario.Estado == "I") return HttpNotFound();

            ViewBag.Id_Rol = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 1), "Id_Parametro", "Nombre_Parametro", usuario.Id_Rol);
            return View(usuario);
        }

        // POST: Usuario/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                usuario.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                usuario.UsuarioModificador = Session["Cedula"]?.ToString() ?? "Sistema";
                usuario.Estado = "A"; // Aseguramos que no se sobrescriba el estado a null

                db.Entry(usuario).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Id_Rol = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 1), "Id_Parametro", "Nombre_Parametro", usuario.Id_Rol);
            return View(usuario);
        }

        // GET: Usuario/Detalles/5
        public ActionResult Detalles(string cedula)
        {
            if (cedula == null) return HttpNotFound();

            var usuario = db.Usuario.Include(u => u.Parametro).FirstOrDefault(u => u.Cedula == cedula && u.Estado == "A");
            if (usuario == null) return HttpNotFound();

            return View(usuario);
        }

        // GET: Usuario/Eliminar/5
        public ActionResult Eliminar(string cedula)
        {
            if (cedula == null) return HttpNotFound();

            var usuario = db.Usuario.Find(cedula);
            if (usuario == null || usuario.Estado == "I") return HttpNotFound();

            return View(usuario);
        }

        // POST: Usuario/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarConfirmado(string cedula)
        {
            var usuario = db.Usuario.Find(cedula);
            if (usuario != null && usuario.Estado != "I")
            {
                usuario.Estado = "I";
                usuario.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                usuario.UsuarioModificador = Session["Cedula"]?.ToString() ?? "sistema";

                db.Entry(usuario).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EnviarRecuperacion(string correo)
        {
            var usuario = db.Usuario.FirstOrDefault(u => u.Correo == correo);
            if (usuario == null) return Json("Correo no registrado.");

            string token = Guid.NewGuid().ToString();

            db.Database.ExecuteSqlCommand(
                "INSERT INTO RecuperacionPassword (Correo, Token, FechaCreacion) VALUES (@p0, @p1, @p2)",
                correo, token, DateTime.Now);

            var link = Url.Action("CambiarContrasena", "Account", new { token = token }, protocol: Request.Url.Scheme);

            string asunto = "Recuperar contraseña - HD Ópticas JAVS";
            string mensaje = $"Haz clic aquí para cambiar tu contraseña:<br><a href='{link}'>Cambiar Contraseña</a>";

            try
            {
                EnviarCorreo(correo, asunto, mensaje);
                return Json(new { mensaje = "Correo enviado exitosamente.", ok = true });
            }
            catch (Exception ex)
            {
                return Json("Error al enviar el correo: " + ex.Message);
            }
        }

        private void EnviarCorreo(string para, string asunto, string cuerpoHtml)
        {
            var fromAddress = new MailAddress("hdopticasjavs@gmail.com", "Soporte HD Ópticas JAVS");
            var toAddress = new MailAddress(para);
            //const string fromPassword = "LamejorOpt1ca!";
            const string fromPassword = "ysuk wivj qivo dacj";

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
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }

        private void EnviarCorreoRecuperacion(string correo)
        {
            string token = Guid.NewGuid().ToString();

            db.Database.ExecuteSqlCommand(
                "INSERT INTO RecuperacionPassword (Correo, Token, FechaCreacion) VALUES (@p0, @p1, @p2)",
                correo, token, DateTime.Now);

            var link = Url.Action("CambiarContrasena", "Account", new { token = token }, protocol: Request.Url.Scheme);

            string asunto = "Establecer contraseña - HD Ópticas JAVS";
            string mensaje = $"<p>Bienvenido a HD Ópticas JAVS.</p><p>Para establecer su contraseña, haga clic en el siguiente enlace:</p><p><a href='{link}'>Establecer Contraseña</a></p>";

            EnviarCorreo(correo, asunto, mensaje);
        }

    }
}