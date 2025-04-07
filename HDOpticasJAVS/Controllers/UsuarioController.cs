using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly string filePath = AppDomain.CurrentDomain.BaseDirectory + "/data/Usuarios.txt";

        // Lista de roles disponibles
        private readonly List<string> roles = new List<string>
        {
            "Administrador", "Cajero", "Bodeguero", "Contador", "Recepcionista", "Especialista"
        };

        public ActionResult Index()
        {
            List<UsuarioViewModel> usuarios = new List<UsuarioViewModel>();
            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var data = line.Split(';');
                    usuarios.Add(new UsuarioViewModel
                    {
                        Cedula = data[0],
                        Nombre = data[1],
                        Apellido1 = data[2],
                        Apellido2 = data[3],
                        Correo = data[4],
                        Fecha_Nacimiento = DateTime.Parse(data[6]),
                        Rol = data[7]
                    });
                }
            }
            return View(usuarios);
        }

        public ActionResult Crear()
        {
            ViewBag.Roles = new SelectList(roles);
            return View();
        }

        [HttpPost]
        public ActionResult Crear(UsuarioViewModel usuario)
        {
            if (ModelState.IsValid)
            {
                string userData = $"{usuario.Cedula};{usuario.Nombre};{usuario.Apellido1};{usuario.Apellido2};{usuario.Correo};{usuario.Fecha_Nacimiento};{usuario.Rol}";
                System.IO.File.AppendAllText(filePath, userData + "\n");
                return RedirectToAction("Index");
            }
            ViewBag.Roles = new SelectList(roles);
            return View(usuario);
        }

        public ActionResult Editar(string cedula)
        {
            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var data = line.Split(';');
                    if (data[0] == cedula)
                    {
                        var usuario = new UsuarioViewModel
                        {
                            Cedula = data[0],
                            Nombre = data[1],
                            Apellido1 = data[2],
                            Apellido2 = data[3],
                            Correo = data[4],
                            Fecha_Nacimiento = DateTime.Parse(data[6]),
                            Rol = data[7]
                        };
                        ViewBag.Roles = new SelectList(roles, usuario.Rol);
                        return View(usuario);
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Editar(UsuarioViewModel usuario)
        {
            if (ModelState.IsValid && System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath).ToList();
                for (int i = 0; i < lines.Count; i++)
                {
                    var data = lines[i].Split(';');
                    if (data[0] == usuario.Cedula)
                    {
                        lines[i] = $"{usuario.Cedula};{usuario.Nombre};{usuario.Apellido1};{usuario.Apellido2};{usuario.Correo};{usuario.Fecha_Nacimiento};{usuario.Rol}";
                        break;
                    }
                }
                System.IO.File.WriteAllLines(filePath, lines);
                return RedirectToAction("Index");
            }
            ViewBag.Roles = new SelectList(roles);
            return View(usuario);
        }

        public ActionResult Detalles(string cedula)
        {
            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var data = line.Split(';');
                    if (data[0] == cedula)
                    {
                        var usuario = new UsuarioViewModel
                        {
                            Cedula = data[0],
                            Nombre = data[1],
                            Apellido1 = data[2],
                            Apellido2 = data[3],
                            Correo = data[4],
                            Fecha_Nacimiento = DateTime.Parse(data[6]),
                            Rol = data[7]
                        };
                        return View(usuario);
                    }
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult Eliminar(string cedula)
        {
            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath).ToList();
                lines = lines.Where(line => !line.StartsWith(cedula + ";")).ToList();
                System.IO.File.WriteAllLines(filePath, lines);
            }
            return RedirectToAction("Index");
        }
    }
}