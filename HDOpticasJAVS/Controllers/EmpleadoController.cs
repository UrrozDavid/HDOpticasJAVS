using System.Data.Entity;
using System;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS;
using HDOpticasJAVS.Models;
using System.IO;
using System.Net.Mail;
using System.Net;

namespace HDOpticasJAVS.Controllers
{
    public class EmpleadoController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        public ActionResult Index()
        {
            var empleados = (from emp in db.Empleado
                             join usu in db.Usuario on emp.Cedula equals usu.Cedula
                             join rol in db.Parametro on usu.Id_Rol equals rol.Id_Parametro
                             select new EmpleadoViewModel
                             {
                                 Cedula = emp.Cedula,
                                 Nombre = usu.Nombre,
                                 Apellido1 = usu.Apellido1,
                                 Apellido2 = usu.Apellido2,
                                 Correo = usu.Correo,
                                 Direccion = emp.Direccion,
                                 NumeroTelefono = emp.Numero_Telefono,
                                 ContactoEmergencia = emp.Contacto_Emergencia,
                                 Placa_Vehiculo = emp.Placa_Vehiculo,
                                 Estado = usu.Estado
                             }).ToList();

            return View(empleados);
        }

        [HttpGet]
        public ActionResult Buscar(string criterio)
        {
            var empleados = (from emp in db.Empleado
                             join usu in db.Usuario on emp.Cedula equals usu.Cedula
                             join rol in db.Parametro on usu.Id_Rol equals rol.Id_Parametro
                             where usu.Nombre.Contains(criterio) ||
                                   emp.Cedula.Contains(criterio) ||
                                   rol.Nombre_Parametro.Contains(criterio)
                             select new EmpleadoViewModel
                             {
                                 Cedula = emp.Cedula,
                                 Nombre = usu.Nombre,
                                 Apellido1 = usu.Apellido1,
                                 Apellido2 = usu.Apellido2,
                                 Correo = usu.Correo,
                                 Direccion = emp.Direccion,
                                 NumeroTelefono = emp.Numero_Telefono,
                                 ContactoEmergencia = emp.Contacto_Emergencia,
                                 Placa_Vehiculo = emp.Placa_Vehiculo,
                                 Estado = usu.Estado,
                                 Rol = rol.Nombre_Parametro
                             }).ToList();

            if (empleados.Count == 0)
            {
                ViewBag.Mensaje = "No se encontraron resultados.";
            }

            return View("Index", empleados);
        }

        [HttpGet]
        public ActionResult Informe(string estado, string rol, string departamento)
        {
            var empleados = (from emp in db.Empleado
                             join usu in db.Usuario on emp.Cedula equals usu.Cedula
                             join paramRol in db.Parametro on usu.Id_Rol equals paramRol.Id_Parametro
                             where (string.IsNullOrEmpty(estado) || usu.Estado == estado)
                                && (string.IsNullOrEmpty(rol) || paramRol.Nombre_Parametro.Contains(rol))
                                && (string.IsNullOrEmpty(departamento))
                             select new EmpleadoViewModel
                             {
                                 Cedula = emp.Cedula,
                                 Nombre = usu.Nombre,
                                 Apellido1 = usu.Apellido1,
                                 Apellido2 = usu.Apellido2,
                                 Correo = usu.Correo,
                                 Direccion = emp.Direccion,
                                 NumeroTelefono = emp.Numero_Telefono,
                                 ContactoEmergencia = emp.Contacto_Emergencia,
                                 Placa_Vehiculo = emp.Placa_Vehiculo,
                                 Estado = usu.Estado,
                                 Rol = paramRol.Nombre_Parametro
                             }).ToList();

            ViewBag.Estado = estado;
            ViewBag.Rol = rol;
            ViewBag.Departamento = departamento;

            return View(empleados);
        }

        public ActionResult ExportarExcel(string estado, string rol, string departamento)
        {
            var empleados = (from emp in db.Empleado
                             join usu in db.Usuario on emp.Cedula equals usu.Cedula
                             join paramRol in db.Parametro on usu.Id_Rol equals paramRol.Id_Parametro
                             where (string.IsNullOrEmpty(estado) || usu.Estado == estado)
                                && (string.IsNullOrEmpty(rol) || paramRol.Nombre_Parametro.Contains(rol))
                                && (string.IsNullOrEmpty(departamento))
                             select new
                             {
                                 emp.Cedula,
                                 NombreCompleto = usu.Nombre + " " + usu.Apellido1 + " " + usu.Apellido2,
                                 usu.Correo,
                                 Rol = paramRol.Nombre_Parametro,
                                 Estado = usu.Estado == "A" ? "Activo" : "Inactivo"
                             }).ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var hoja = workbook.Worksheets.Add("Empleados");
                hoja.Cell(1, 1).InsertTable(empleados);

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    ms.Position = 0;
                    return File(ms.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "InformeEmpleados.xlsx");
                }
            }
        }

        public ActionResult ExportarPdf()
        {
            var empleados = (from emp in db.Empleado
                             join usu in db.Usuario on emp.Cedula equals usu.Cedula
                             join paramRol in db.Parametro on usu.Id_Rol equals paramRol.Id_Parametro
                             select new EmpleadoViewModel
                             {
                                 Cedula = emp.Cedula,
                                 Nombre = usu.Nombre,
                                 Apellido1 = usu.Apellido1,
                                 Apellido2 = usu.Apellido2,
                                 Correo = usu.Correo,
                                 Direccion = emp.Direccion,
                                 NumeroTelefono = emp.Numero_Telefono,
                                 ContactoEmergencia = emp.Contacto_Emergencia,
                                 Placa_Vehiculo = emp.Placa_Vehiculo,
                                 Estado = usu.Estado,
                                 Rol = paramRol.Nombre_Parametro
                             }).ToList();

            return new Rotativa.ViewAsPdf("InformePdf", empleados)
            {
                FileName = "Empleados.pdf",
                PageOrientation = Rotativa.Options.Orientation.Landscape
            };
        }

        public ActionResult PerfilEmpleado()
        {
            string cedula = Session["Cedula"]?.ToString();

            if (string.IsNullOrEmpty(cedula))
                return RedirectToAction("Login", "Account");

            var emp = (from u in db.Usuario
                       join e in db.Empleado on u.Cedula equals e.Cedula into ue
                       from empleado in ue.DefaultIfEmpty()
                       where u.Cedula == cedula
                       select new EmpleadoViewModel
                       {
                           Cedula = u.Cedula,
                           Nombre = u.Nombre,
                           Apellido1 = u.Apellido1,
                           Apellido2 = u.Apellido2,
                           Correo = u.Correo,
                           Direccion = empleado != null ? empleado.Direccion : "",
                           NumeroTelefono = empleado != null ? empleado.Numero_Telefono : "",
                           Placa_Vehiculo = empleado != null ? empleado.Placa_Vehiculo : ""
                       }).FirstOrDefault();

            if (emp == null)
                return HttpNotFound();

            return View("PerfilEmpleado", emp);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnviarCorreo(string mensajeAdicional)
        {
            try
            {
                string cedula = Session["Cedula"]?.ToString();

                if (string.IsNullOrEmpty(cedula))
                {
                    TempData["Mensaje"] = "La sesión ha expirado o no se encontró la cédula.";
                    return RedirectToAction("Login", "Cuenta");
                }

                var empleado = (from e in db.Empleado
                                join u in db.Usuario on e.Cedula equals u.Cedula
                                where e.Cedula == cedula
                                select new
                                {
                                    Nombre = u.Nombre,
                                    Apellido1 = u.Apellido1,
                                    Apellido2 = u.Apellido2,
                                    Correo = u.Correo,
                                    Telefono = e.Numero_Telefono,
                                    Direccion = e.Direccion
                                }).FirstOrDefault();

                if (empleado == null)
                {
                    TempData["Mensaje"] = "No se encontró la información del empleado.";
                    return RedirectToAction("PerfilEmpleado");
                }

                // Configuración de correo
                string correoRemitente = "hdopticasjavs@gmail.com";
                string contrasenaApp = "ysuk wivj qivo dacj";
                string correoDestinatario = "hdopticasjavs@gmail.com";

                var mensaje = new MailMessage();
                mensaje.From = new MailAddress("no-reply@optica.com", "Solicitud del Empleado"); // ficticio
                mensaje.To.Add(correoDestinatario);
                mensaje.ReplyToList.Add(new MailAddress(empleado.Correo)); // permite responder al correo real del empleado
                mensaje.Subject = "Solicitud desde perfil de empleado";
                mensaje.Body = $@"
Hola, el siguiente empleado ha enviado una solicitud desde su perfil:

Nombre: {empleado.Nombre} {empleado.Apellido1} {empleado.Apellido2}
Cédula: {cedula}
Correo: {empleado.Correo}
Teléfono: {empleado.Telefono}
Dirección: {empleado.Direccion}

Mensaje adicional del empleado:
{mensajeAdicional}

Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
";
                mensaje.IsBodyHtml = false;

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.Credentials = new NetworkCredential(correoRemitente, contrasenaApp);
                    smtp.Send(mensaje);
                }

                TempData["Mensaje"] = "Solicitud enviada correctamente.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR SMTP: " + ex.ToString());
                TempData["Mensaje"] = "Error al enviar el correo: " + ex.Message;
            }

            return RedirectToAction("PerfilEmpleado");
        }

        public ActionResult Crear()
        {
            ViewBag.Roles = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 1 && p.Estado == "A" && p.Nombre_Parametro.ToLower() != "cliente"),
                "Id_Parametro",
                "Nombre_Parametro"
            );
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

                TempData["Exito"] = "Usuario empleado creado exitosamente.";
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

            var empleado = db.Empleado.Find(cedula);
            if (empleado == null) return HttpNotFound();

            return View(empleado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Empleado empleado)
        {
            if (ModelState.IsValid)
            {
                var empleadoExistente = db.Empleado.Find(empleado.Cedula);
                if (empleadoExistente == null)
                    return HttpNotFound();

                // Validación de estado inactivo
                if (empleadoExistente.Estado == "I" && empleado.Estado != "A")
                {
                    ModelState.AddModelError("", "El usuario está inactivo. Solo puede activarlo para habilitar otros cambios.");

                    return View(empleadoExistente);
                }

                // Actualizar datos personales del Usuario asociado
                empleadoExistente.Usuario.Nombre = empleado.Usuario.Nombre;
                empleadoExistente.Usuario.Apellido1 = empleado.Usuario.Apellido1;
                empleadoExistente.Usuario.Apellido2 = empleado.Usuario.Apellido2;
                empleadoExistente.Usuario.Correo = empleado.Usuario.Correo;

                // Actualizar datos propios del Empleado
                empleadoExistente.Direccion = empleado.Direccion;
                empleadoExistente.Numero_Telefono = empleado.Numero_Telefono;
                empleadoExistente.Contacto_Emergencia = empleado.Contacto_Emergencia;
                empleadoExistente.Placa_Vehiculo = empleado.Placa_Vehiculo;
                empleadoExistente.Estado = empleado.Estado;

                // Registro de modificación
                empleadoExistente.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                empleadoExistente.UsuarioModificador = Session["Cedula"]?.ToString() ?? "Sistema";

                db.Entry(empleadoExistente).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(empleado);
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
    }
}

