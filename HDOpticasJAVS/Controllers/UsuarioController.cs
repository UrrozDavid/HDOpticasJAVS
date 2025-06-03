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
    public class UsuarioController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        public ActionResult Index()
        {
            var usuarios = db.Usuario
                             .Include(u => u.Parametro)
                             //.Where(u => u.Estado == "A")
                             .ToList();
            return View(usuarios);
        }

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

        [HttpGet]
        public ActionResult Editar(string cedula)
        {
            if (cedula == null) return HttpNotFound();

            var usuario = db.Usuario.Find(cedula);
            if (usuario == null) return HttpNotFound();

            //ViewBag.Id_Rol = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 1), "Id_Parametro", "Nombre_Parametro", usuario.Id_Rol);
            var roles = db.Parametro
                .Where(p => p.Id_TipoParametro == 1)
                .Select(p => new SelectListItem
                {
                    Value = p.Id_Parametro.ToString(),
                    Text = p.Nombre_Parametro,
                    Selected = (p.Id_Parametro == usuario.Id_Rol)
                }).ToList();

            ViewBag.Id_Rol = roles;
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                var usuarioExistente = db.Usuario.Find(usuario.Cedula);
                if (usuarioExistente == null)
                    return HttpNotFound();

                if (usuarioExistente.Estado == "I" && usuario.Estado != "A")
                {
                    ModelState.AddModelError("", "El usuario está inactivo. Solo puede activarlo para habilitar otros cambios.");

                    usuario.Nombre = usuarioExistente.Nombre;
                    usuario.Apellido1 = usuarioExistente.Apellido1;
                    usuario.Apellido2 = usuarioExistente.Apellido2;
                    usuario.Correo = usuarioExistente.Correo;
                    usuario.FechaNacimiento = usuarioExistente.FechaNacimiento;
                    usuario.Id_Rol = usuarioExistente.Id_Rol;
                    usuario.Estado = usuarioExistente.Estado;
                    usuario.FechaBloqueoHasta = usuarioExistente.FechaBloqueoHasta;

                    ViewBag.Id_Rol = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 1), "Id_Parametro", "Nombre_Parametro", usuario.Id_Rol);
                    return View(usuario);
                }

                var cambio = new Usuario_UltimoCambio
                {
                    Cedula = usuarioExistente.Cedula,
                    Nombre = usuarioExistente.Nombre,
                    Apellido1 = usuarioExistente.Apellido1,
                    Apellido2 = usuarioExistente.Apellido2,
                    Contrasenia = usuarioExistente.Contrasenia,
                    FechaNacimiento = usuarioExistente.FechaNacimiento,
                    Correo = usuarioExistente.Correo,
                    Id_Rol = usuarioExistente.Id_Rol,
                    FechaBloqueoHasta = usuarioExistente.FechaBloqueoHasta,
                    FechaCambio = DateTime.Now,
                    Revertido = false
                };
                db.Usuario_UltimoCambio.Add(cambio);

                string estadoAnterior = usuarioExistente.Estado;
                int? rolAnterior = usuarioExistente.Id_Rol;

                usuarioExistente.Nombre = usuario.Nombre;
                usuarioExistente.Apellido1 = usuario.Apellido1;
                usuarioExistente.Apellido2 = usuario.Apellido2;
                usuarioExistente.Correo = usuario.Correo;
                usuarioExistente.FechaNacimiento = usuario.FechaNacimiento;
                usuarioExistente.Id_Rol = usuario.Id_Rol;
                usuarioExistente.Estado = usuario.Estado;
                usuarioExistente.FechaBloqueoHasta = usuario.FechaBloqueoHasta;
                usuarioExistente.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                usuarioExistente.UsuarioModificador = Session["Cedula"]?.ToString() ?? "Sistema";

                db.Entry(usuarioExistente).State = EntityState.Modified;
                db.SaveChanges();

                bool cambioEstado = estadoAnterior != usuario.Estado;
                bool cambioRol = rolAnterior != usuario.Id_Rol;

                if (cambioEstado && !cambioRol)
                {
                    string nombreEstadoAnterior = estadoAnterior == "A" ? "Activo" : "Inactivo";
                    string nombreEstadoNuevo = usuario.Estado == "A" ? "Activo" : "Inactivo";
                    EnviarCorreoEspecifico(usuarioExistente.Correo, "Cambio de Estado",
                        $"Tu estado de usuario ha sido actualizado de '{nombreEstadoAnterior}' a '{nombreEstadoNuevo}'.");
                }
                else if (!cambioEstado && cambioRol)
                {
                    string nombreRolAnterior = db.Parametro
                        .Where(p => p.Id_Parametro == rolAnterior)
                        .Select(p => p.Nombre_Parametro)
                        .FirstOrDefault() ?? rolAnterior.ToString();

                    string nombreRolNuevo = db.Parametro
                        .Where(p => p.Id_Parametro == usuario.Id_Rol)
                        .Select(p => p.Nombre_Parametro)
                        .FirstOrDefault() ?? usuario.Id_Rol.ToString();
                    EnviarCorreoEspecifico(usuarioExistente.Correo, "Cambio de Rol",
                        $"Tu rol ha sido actualizado de '{nombreRolAnterior}' a '{nombreRolNuevo}'.");
                }
                else
                {
                    EnviarCorreoCambios(usuarioExistente.Correo);
                }

                return RedirectToAction("Index");
            }

            var roles = db.Parametro
        .Where(p => p.Id_TipoParametro == 1)
        .ToList()
        .Select(p => new SelectListItem
        {
            Value = p.Id_Parametro.ToString(),
            Text = p.Nombre_Parametro,
            Selected = (p.Id_Parametro == usuario.Id_Rol)
        }).ToList();

            ViewBag.Roles = roles;
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RevertirUltimoCambio(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
            {
                TempData["Mensaje"] = "Cédula inválida.";
                return RedirectToAction("Editar", new { cedula = cedula });
            }

            var ultimo = db.Usuario_UltimoCambio
                .Where(u => u.Cedula == cedula && u.Revertido == false)
                .OrderByDescending(u => u.FechaCambio)
                .FirstOrDefault();

            if (ultimo == null)
            {
                TempData["Mensaje"] = "No hay cambios para revertir.";
                return RedirectToAction("Editar", new { cedula = cedula });
            }

            var usuario = db.Usuario.Find(cedula);
            if (usuario == null)
            {
                TempData["Mensaje"] = "Usuario no encontrado.";
                return RedirectToAction("Editar", new { cedula = cedula });
            }

            usuario.Nombre = ultimo.Nombre;
            usuario.Apellido1 = ultimo.Apellido1;
            usuario.Apellido2 = ultimo.Apellido2;
            usuario.Contrasenia = ultimo.Contrasenia;
            usuario.FechaNacimiento = ultimo.FechaNacimiento;
            usuario.Correo = ultimo.Correo;
            usuario.Id_Rol = ultimo.Id_Rol;
            usuario.FechaBloqueoHasta = ultimo.FechaBloqueoHasta;
            usuario.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            usuario.UsuarioModificador = Session["Cedula"]?.ToString() ?? "Sistema";

            ultimo.Revertido = true;

            db.SaveChanges();

            TempData["Mensaje"] = "Último cambio revertido con éxito.";

            return RedirectToAction("Editar", new { cedula = cedula });
        }

        private void EnviarCorreoEspecifico(string correo, string asunto, string mensaje)
        {
            var mail = new MailMessage();
            mail.To.Add(correo);
            mail.Subject = asunto;
            mail.Body = mensaje;
            mail.IsBodyHtml = true;
            mail.From = new MailAddress("hdopticasjavs@gmail.com");

            using (var smtp = new SmtpClient())
            {
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("hdopticasjavs@gmail.com", "ysuk wivj qivo dacj");
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
        }

        private void EnviarCorreoCambios(string correo)
        {
            var mail = new MailMessage();
            mail.To.Add(correo);
            mail.Subject = "Cambios en su cuenta de usuario";
            mail.Body = "Se han aplicado cambios en su cuenta de usuario.";
            mail.IsBodyHtml = true;
            mail.From = new MailAddress("hdopticasjavs@gmail.com");

            using (var smtp = new SmtpClient())
            {
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("hdopticasjavs@gmail.com", "ysuk wivj qivo dacj");
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
        }

        public ActionResult Detalles(string cedula)
        {
            if (cedula == null) return HttpNotFound();

            var usuario = db.Usuario.Include(u => u.Parametro).FirstOrDefault(u => u.Cedula == cedula);
            if (usuario == null) return HttpNotFound();

            return View(usuario);
        }

        public ActionResult Eliminar(string cedula)
        {
            if (cedula == null) return HttpNotFound();

            var usuario = db.Usuario.Find(cedula);
            if (usuario == null || usuario.Estado == "I") return HttpNotFound();

            return View(usuario);
        }

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