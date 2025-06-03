using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class ClientesController : BaseController
    {
        // GET: Clientes
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        // GET: Clientes/Crear
        public ActionResult Crear()
        {
            return View();
        }

        public ActionResult Perfil()
        {
            return View();
        }

        // POST: Clientes/Crear
        [HttpPost]

       public ActionResult VerCliente(FormCollection collection)
        {
            TempData["ClienteNombre"] = collection["Nombre"];
            TempData["Cedula"] = collection["Cedula"];
            TempData["Telefono"] = collection["Telefono"];
            TempData["Correo"] = collection["Correo"];
            return RedirectToAction("Detalles", new { id = 1 });
        }

        public ActionResult Crear(FormCollection collection)
        {
            string nombre = collection["Nombre"];
            string cedula = collection["Cedula"];
            string telefono = collection["Telefono"];
            string correo = collection["Correo"];

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(cedula)
                || string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(correo))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Crear");
            }

            TempData["SuccessMessage"] = "El cliente fue registrado exitosamente.";
            return RedirectToAction("Index");
        }

        // GET: Clientes/Detalles
        public ActionResult Detalles(int id = 1)
        {
            ViewBag.ClienteNombre = TempData["ClienteNombre"] ?? "María González";
            ViewBag.Cedula = TempData["Cedula"] ?? "1-2345-6789";
            ViewBag.Telefono = TempData["Telefono"] ?? "8888-8888";
            ViewBag.Correo = TempData["Correo"] ?? "maria@example.com";

            return View();
        }


        // GET: Clientes/Editar
        public ActionResult Editar(int id = 1)
        {
            ViewBag.ClienteNombre = "María González";
            ViewBag.Cedula = "1-2345-6789";
            ViewBag.Telefono = "8888-8888";
            ViewBag.Correo = "maria@example.com";
            return View();
        }

        // POST: Clientes/Editar
        [HttpPost]
        public ActionResult Editar(FormCollection collection)
        {
            string nombre = collection["Nombre"];
            string cedula = collection["Cedula"];
            string telefono = collection["Telefono"];
            string correo = collection["Correo"];

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(cedula) ||
                string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(correo))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios para editar el cliente.";
                return RedirectToAction("Editar", new { id = 1 });
            }

            // Guardamos en TempData para mostrarlo en Detalles
            TempData["ClienteNombre"] = nombre;
            TempData["Cedula"] = cedula;
            TempData["Telefono"] = telefono;
            TempData["Correo"] = correo;

            TempData["SuccessMessage"] = "La información del cliente fue actualizada correctamente.";
            return RedirectToAction("Detalles", new { id = 1 });
        }

        // POST: Clientes/GuardarEvaluacion
        [HttpPost]
        public ActionResult GuardarEvaluacion(FormCollection form)
        {
            string fecha = form["Fecha"];
            string diagnostico = form["Diagnostico"];
            string tratamiento = form["Tratamiento"];
            string observaciones = form["Observaciones"];

            if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(diagnostico)
                || string.IsNullOrWhiteSpace(tratamiento) || string.IsNullOrWhiteSpace(observaciones))
            {
                TempData["ErrorMessage"] = "Todos los campos de la evaluación médica son obligatorios.";
                return RedirectToAction("Detalles", new { id = 1 });
            }

            TempData["SuccessMessage"] = "La evaluación médica fue agregada exitosamente.";
            return RedirectToAction("Detalles", new { id = 1 });
        }
    }
}
