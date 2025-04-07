using System.Drawing.Drawing2D;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;

namespace HDOpticasJAVS.Controllers
{
    public class EmpleadoController : Controller
    {
        // GET: Empleado
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        // GET: Empleado/Crear
        public ActionResult Crear()
        {
            return View();
        }

        // POST: Empleado/Crear
        [HttpPost]

        public ActionResult VerEmpleado(FormCollection collection)
        {
            TempData["EmpleadoNombre"] = collection["EmpleadoNombre"];
            TempData["Cedula"] = collection["Cedula"];
            TempData["Telefono"] = collection["Telefono"];
            TempData["Correo"] = collection["Correo"];
            TempData["Direccion"] = collection["Direccion"];
            TempData["Contacto de emergencia"] = collection["Contacto de emergencia"];
            return RedirectToAction("Detalles", new { id = 1 });
        }

        public ActionResult Crear(FormCollection collection)
        {
            string nombre = collection["EmpleadoNombre"];
            string cedula = collection["Cedula"];
            string telefono = collection["Telefono"];
            string correo = collection["Correo"];
            string direccion = collection["Direccion"];
            string contactoDeEmergencia = collection["ContactoDeEmergencia"];

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(cedula)
                || string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(correo)
                || string.IsNullOrWhiteSpace(direccion) || string.IsNullOrWhiteSpace(contactoDeEmergencia))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Crear");
            }

            TempData["SuccessMessage"] = "El empelado fue registrado exitosamente.";
            return RedirectToAction("Index");
        }

        // GET: Empleado/Detalles
        public ActionResult Detalles(int id = 1)
        {
            ViewBag.EmpleadoNombre = TempData["EmpleadoNombre"] ?? "María Fernanda Lopez";
            ViewBag.Cedula = TempData["Cedula"] ?? "1-2345-6789";
            ViewBag.Telefono = TempData["Telefono"] ?? "8888-1234";
            ViewBag.Correo = TempData["Correo"] ?? "maria.lopez @example.com";
            ViewBag.Direccion = TempData["Direccion"] ?? "San José, Costa Rica";
            ViewBag.ContactoDeEmergencia = TempData["Contacto de emergencia"] ?? "Carlos López - 8822-3344";

            return View();
        }


        // GET: Empelados/Editar
        public ActionResult Editar(int id = 1)
        {
            ViewBag.EmpleadoNombre = "María Fernanda Lopez";
            ViewBag.Cedula = "1-2345-6789";
            ViewBag.Telefono = "8888-1234";
            ViewBag.Correo = "maria.lopez @example.com";
            ViewBag.Direccion = "San José, Costa Rica";
            ViewBag.ContactoDeEmergencia = "Carlos López - 8822-3344";
            return View();
        }

        // POST:   Empleado/Editar
        [HttpPost]
        public ActionResult Editar(FormCollection collection)
        {
            string nombre = collection["EmpleadoNombre"];
            string cedula = collection["Cedula"];
            string telefono = collection["Telefono"];
            string correo = collection["Correo"];
            string direccion = collection["Direccion"];
            string contactoDeEmergencia = collection["Contacto de emergencia"];

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(cedula) ||
                string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(correo)
               || string.IsNullOrWhiteSpace(direccion) || string.IsNullOrWhiteSpace(contactoDeEmergencia))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios para editar el empleado.";
                return RedirectToAction("Editar", new { id = 1 });
            }

            // Guardamos en TempData para mostrarlo en Detalles
            TempData["EmpleadoNombre"] = nombre;
            TempData["Cedula"] = cedula;
            TempData["Telefono"] = telefono;
            TempData["Correo"] = correo;
            TempData["Direccion"] = direccion;
            TempData["Contacto de emergencia"] = contactoDeEmergencia;

            TempData["SuccessMessage"] = "La información del empleado fue actualizada correctamente.";
            return RedirectToAction("Detalles", new { id = 1 });
        }

        //[HttpPost]
        //public ActionResult Eliminar(int id)
        //{
        //    var empleado = Empleado.FirstOrDefault(v => v.Cedula == id);
        //    if (empleado != null)
        //    {
        //        Empleado.Remove(empleado);
        //        TempData["SuccessMessage"] = "Empleado eliminada exitosamente.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "Empleado no encontrada.";
        //    }

        //    return RedirectToAction("Index");
        //}

        // POST: Empleado/GuardarDatos
        [HttpPost]
        public ActionResult GuardarDatos(FormCollection form)
        {
            string puesto = form["Puesto"];
            string departamento = form["Departamento"];
            string fecha = form["Fecha de ingreso"];
            string contrato = form["Tipo de contrato"];
            string horario = form["Horario laboral"];
            string salario = form["Salario"];
            string supervisor = form["Supervisor directo"];

            if (string.IsNullOrWhiteSpace(puesto) || string.IsNullOrWhiteSpace(departamento)
                || string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(contrato)
                || string.IsNullOrWhiteSpace(horario) || string.IsNullOrWhiteSpace(salario)
                || string.IsNullOrWhiteSpace(supervisor))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Detalles", new { id = 1 });
            }

            TempData["SuccessMessage"] = "Los datos fueron agregados exitosamente.";
            return RedirectToAction("Detalles", new { id = 1 });
        }
    }
}
