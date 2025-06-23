using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Helpers;
using System.Web.Mvc;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Controllers
{
    public class AccountController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Usuario, string Contrasenia)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var usuarioEncontrado = db.Usuario
                    .FirstOrDefault(u => u.Correo == Usuario || u.Cedula == Usuario);

                if (usuarioEncontrado == null)
                {
                    ViewBag.Mensaje = "El usuario ingresado no existe.";
                    RegistrarIntento(Usuario, false, "Usuario no encontrado");
                    return View();
                }

                if (usuarioEncontrado.Estado == "I")
                {
                    ViewBag.Mensaje = "La cuenta se encuentra inactiva.";
                    RegistrarIntento(Usuario, false, "Cuenta inactiva");
                    return View();
                }

                if (usuarioEncontrado.FechaBloqueoHasta != null && usuarioEncontrado.FechaBloqueoHasta > DateTime.Now)
                {
                    TimeSpan tiempoRestante = usuarioEncontrado.FechaBloqueoHasta.Value - DateTime.Now;
                    ViewBag.Mensaje = $"El usuario está bloqueado temporalmente. Intente de nuevo en {tiempoRestante.Minutes} minutos.";
                    RegistrarIntento(Usuario, false, "Usuario bloqueado temporalmente");
                    return View();
                }

                if (usuarioEncontrado.Contrasenia != Contrasenia)
                {
                    ViewBag.Mensaje = "La contraseña ingresada es incorrecta.";
                    RegistrarIntento(Usuario, false, "Contraseña incorrecta");

                    DateTime hace30Min = DateTime.Now.AddMinutes(-30);
                    int intentosFallidos = db.IntentoLogin
                        .Where(i => i.UsuarioIntentado == Usuario && i.Exito == false && i.FechaIntento >= hace30Min)
                        .Count();

                    if (intentosFallidos >= 5)
                    {
                        usuarioEncontrado.FechaBloqueoHasta = DateTime.Now.AddMinutes(30);
                        db.SaveChanges();
                        ViewBag.Mensaje = "Demasiados intentos fallidos. Su cuenta ha sido bloqueada por 30 minutos.";
                        return View();
                    }

                    return View();
                }

                // Login exitoso
                RegistrarIntento(Usuario, true, "Inicio de sesión exitoso");
                usuarioEncontrado.FechaBloqueoHasta = null;
                db.SaveChanges();

                Session["Usuario"] = usuarioEncontrado.Correo ?? usuarioEncontrado.Cedula;
                Session["Rol"] = usuarioEncontrado.Id_Rol;
                return RedirectToAction("Index", "Home");
            }
        }

        private void RegistrarIntento(string usuarioIntentado, bool exito, string razon)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var intento = new IntentoLogin
                {
                    UsuarioIntentado = usuarioIntentado,
                    FechaIntento = DateTime.Now,
                    Exito = exito,
                    Mensaje = razon
                };

                db.IntentoLogin.Add(intento);
                db.SaveChanges();
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        public ActionResult RegistroCliente()
        {
            ViewBag.Generos = db.Parametro
                .Where(p => p.Id_TipoParametro == 2 && p.Estado == "A")
                .ToList();

            return View();
        }

        [HttpPost]
        public ActionResult GuardarCliente(FormCollection form)
        {
            try
            {
                string cedula = form["Cedula"];
                string correo = form["Correo"];

                if (db.Usuario.Any(u => u.Cedula == cedula || u.Correo == correo))
                {
                    ViewBag.Mensaje = "Ya existe un usuario con esa cédula o correo.";
                    ViewBag.Generos = db.Parametro.Where(p => p.Id_TipoParametro == 1 && p.Estado == "A").ToList();
                    return View("RegistroCliente");
                }

                Usuario nuevoUsuario = new Usuario
                {
                    Cedula = cedula,
                    Nombre = form["Nombre"],
                    Apellido1 = form["Apellido1"],
                    Apellido2 = form["Apellido2"],
                    Correo = correo,
                    Contrasenia = form["Contrasenia"],
                    FechaNacimiento = Convert.ToDateTime(form["Fecha_Nacimiento"]),
                    Id_Rol = 2, // Rol Cliente = 2
                    Estado = "A",
                    UsuarioCreador = "Sistema",
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
                    UsuarioCreador = "Sistema",
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.Cliente.Add(nuevoCliente);
                db.SaveChanges();

                TempData["Exito"] = "Registro exitoso. Inicie sesión.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Error al registrar: " + ex.Message;
                ViewBag.Generos = db.Parametro.Where(p => p.Id_TipoParametro == 2 && p.Estado == "A").ToList();
                return View("RegistroCliente");
            }
        }

        public ActionResult Recuperar()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Recuperar(string email)
        {
            var usuario = db.Usuario.FirstOrDefault(u => u.Correo == email);

            if (usuario != null)
            {
                // Generar token
                string token = Guid.NewGuid().ToString();

                // Guardar en tabla temporal
                db.Database.ExecuteSqlCommand(
                    "INSERT INTO RecuperacionPassword (Correo, Token, FechaCreacion) VALUES (@p0, @p1, @p2)",
                    email, token, DateTime.Now);

                // Enlace de recuperación
                var link = Url.Action("CambiarContrasena", "Account", new { token = token }, protocol: Request.Url.Scheme);

                // Enviar correo
                string asunto = "Recuperar contraseña - HD Ópticas JAVS";
                string mensaje = $"Haz clic aquí para cambiar tu contraseña:<br><a href='{link}'>Cambiar Contraseña</a>";

                EnviarCorreo(email, asunto, mensaje);

                ViewBag.Mensaje = "Se envió un correo con el enlace de recuperación.";
            }
            else
            {
                ViewBag.Mensaje = "El correo no está registrado.";
            }

            return View();
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

        public ActionResult CambiarContrasena(string token)
        {
            var registro = db.Database.SqlQuery<RecuperacionPasswordModel>(
                "SELECT * FROM RecuperacionPassword WHERE Token = @p0", token).FirstOrDefault();

            if (registro == null || registro.FechaCreacion.AddMinutes(30) < DateTime.Now)
            {
                return Content("El enlace ha expirado o no es válido.");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public ActionResult CambiarContrasena(string token, string nuevaContrasena)
        {
            var registro = db.Database.SqlQuery<RecuperacionPasswordModel>(
                "SELECT * FROM RecuperacionPassword WHERE Token = @p0", token).FirstOrDefault();

            if (registro != null)
            {
                var usuario = db.Usuario.FirstOrDefault(u => u.Correo == registro.Correo);
                if (usuario != null)
                {
                    usuario.Contrasenia = nuevaContrasena;
                    db.SaveChanges();

                    // Eliminar token usado
                    db.Database.ExecuteSqlCommand("DELETE FROM RecuperacionPassword WHERE Token = @p0", token);

                    ViewBag.Mensaje = "Contraseña actualizada con éxito.";
                }
            }
            else
            {
                ViewBag.Mensaje = "Token inválido.";
            }

            return View();
        }

    }
}