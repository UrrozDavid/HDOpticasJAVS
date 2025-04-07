using System.Drawing.Drawing2D;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;

namespace HDOpticasJAVS.Controllers
{
    public class CitaEnLineaController : Controller
    {
        // GET: Cita
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        // GET: Cita/Crear
        public ActionResult Crear()
        {
            return View();
        }

        [HttpGet]
        // GET: Cita/Ver
        public ActionResult VerCita()
        {
            return View();
        }

        // POST: Cita/Crear
        [HttpPost]

        public ActionResult VerCita(FormCollection collection)
        {
            TempData["ClientesNombre"] = collection["Nombre"];
            TempData["Fecha cita"] = collection["Fecha cita"];
            TempData["Hora cita"] = collection["Hora cita"];
            TempData["Especialidad"] = collection["Especialidad"];
            TempData["Estado"] = collection["Estado"];
            return RedirectToAction("Detalles", new { id = 1 });
        }

        public ActionResult Crear(FormCollection collection)
        {
            string nombre = collection["Nombre"];
            string fechaCita = collection["Fecha cita"];
            string horaCita = collection["Hora cita"];
            string especialidad = collection["Especialidad"];
            string estado = collection["Estado"];

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(fechaCita)
                || string.IsNullOrWhiteSpace(horaCita) || string.IsNullOrWhiteSpace(especialidad)
                || string.IsNullOrWhiteSpace(estado))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Crear");
            }

            TempData["SuccessMessage"] = "La cita fue registrada exitosamente.";
            return RedirectToAction("Index");
        }

        // GET: Cita/Detalles
        public ActionResult Detalles(int id = 1)
        {
            ViewBag.ClienteNombre = TempData["ClienteNombre"] ?? "Sofía Ramírez Chacón";
            ViewBag.FechaCita = TempData["Fecha cita"] ?? "2025-04-12";
            ViewBag.HoraCita = TempData["Hora cita"] ?? "10:30 a.m.";
            ViewBag.Especialidad = TempData["Especialidad"] ?? "Examen Visual Completo";
            ViewBag.Estado = TempData["Estado"] ?? "Activo";

            return View();
        }


        // GET: Cita/Editar
        public ActionResult Editar(int id = 1)
        {
            ViewBag.ClienteNombre = "Sofía Ramírez Chacón";
            ViewBag.FechaCita = "2025-04-12";
            ViewBag.HoraCita = "10:30 a.m.";
            ViewBag.Especialidad = "Examen Visual Completo";
            ViewBag.Estado = "Activo";
            return View();
        }

        // POST:   Cita/Editar
        [HttpPost]
        public ActionResult Editar(FormCollection collection)
        {
            string nombre = collection["Nombre"];
            string fechaCita = collection["Fecha cita"];
            string horaCita = collection["Hora cita"];
            string especialidad = collection["Especialidad"];
            string estado = collection["Estado"];

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(fechaCita) ||
                string.IsNullOrWhiteSpace(horaCita) || string.IsNullOrWhiteSpace(especialidad)
                || string.IsNullOrWhiteSpace(estado))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios para editar la cita.";
                return RedirectToAction("Editar", new { id = 1 });
            }

            // Guardamos en TempData para mostrarlo en Detalles
            TempData["ClienteNombre"] = nombre;
            TempData["Fecha cita"] = fechaCita;
            TempData["Hora cita"] = horaCita;
            TempData["Especialidad"] = especialidad;
            TempData["Estado"] = estado;

            TempData["SuccessMessage"] = "La información de la cita fue actualizada correctamente.";
            return RedirectToAction("Detalles", new { id = 1 });
        }

        // POST: Cita/GuardarCita
        [HttpPost]
        public ActionResult GuardarCita(FormCollection form)
        {
            string nombre = form["Nombre"];
            string cedula = form["Cedula"];
            string telefono = form["Telefono"];
            string correo = form["Correo"];
            string direccion = form["Direccion"];
            string edad = form["Edad"];

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(cedula)
                || string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(correo)
                || string.IsNullOrWhiteSpace(direccion) || string.IsNullOrWhiteSpace(edad))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Detalles", new { id = 1 });
            }

            TempData["SuccessMessage"] = "Los datos fueron agregados exitosamente.";
            return RedirectToAction("Detalles", new { id = 1 });
        }
    }
}