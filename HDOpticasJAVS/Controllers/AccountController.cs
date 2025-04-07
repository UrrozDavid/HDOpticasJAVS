using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Controllers
{
    public class AccountController : Controller
    {
        private readonly string usuariosPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Usuarios.txt");
        private readonly string clientesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Clientes.txt");


        public ActionResult Login()
        {
            return View();
        }
        // GET: Login
        [HttpPost]
        public ActionResult Login(string Usuario, string Contrasenia)
        {
            try
            {
                if (System.IO.File.Exists(usuariosPath))
                {
                    var lineas = System.IO.File.ReadAllLines(usuariosPath);
                    foreach (var linea in lineas)
                    {
                        var partes = linea.Split(';');
                        if (partes.Length >= 8)
                        {
                            var cedula = partes[0];
                            var correo = partes[4];
                            var contrasenia = partes[5];
                            var rol = partes[7];

                            if ((correo == Usuario || cedula == Usuario) && contrasenia == Contrasenia)
                            {
                                // Autenticación exitosa
                                Session["Usuario"] = correo;
                                Session["Rol"] = rol;
                                Session["NombreCompleto"] = $"{partes[1]} {partes[2]}";

                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }

                ViewBag.Mensaje = "Usuario o contraseña incorrectos.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Error al iniciar sesión: " + ex.Message;
                return View();
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        private void CrearDirectorioSiNoExiste(string ruta)
        {
            string directorio = Path.GetDirectoryName(ruta);
            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio); // Crea la carpeta Data si no existe
            }
        }

        public ActionResult RegistroCliente()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GuardarCliente(ClienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Mensaje = "Error en los datos ingresados.";
                return View("RegistroCliente");
            }

            try
            {
                // Guardar en Usuarios.txt
                string usuarioData = $"{model.Cedula};{model.Nombre};{model.Apellido1};{model.Apellido2};{model.Correo};{model.Contrasenia};{model.Fecha_Nacimiento};Cliente";
                System.IO.File.AppendAllText(usuariosPath, usuarioData + Environment.NewLine);

                // Guardar en Clientes.txt
                string clienteData = $"{model.Cedula};{model.Edad};{model.Genero};{model.Padecimiento};{model.Numero_Telefono}";
                System.IO.File.AppendAllText(clientesPath, clienteData + Environment.NewLine);

                ViewBag.Mensaje = "Cliente registrado exitosamente.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Error al guardar los datos: " + ex.Message;
                return View("RegistroCliente");
            }
        }
    }
}