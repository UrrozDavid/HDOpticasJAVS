using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class MarketingController : Controller
    {
        public ActionResult Index() => View();

        public ActionResult Crear() => View();

        [HttpPost]
        public ActionResult Crear(FormCollection collection)
        {
            string titulo = collection["Titulo"];
            string segmento = collection["Segmento"];
            string fecha = collection["FechaEnvio"];
            string mensaje = collection["Mensaje"];

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(segmento) ||
                string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(mensaje))
            {
                TempData["ErrorMessage"] = "Todos los campos de la campaña son obligatorios.";
                return RedirectToAction("Crear");
            }

            TempData["SuccessMessage"] = "La campaña fue creada exitosamente.";
            return RedirectToAction("Index");
        }

        public ActionResult Detalles(int id = 1)
        {
            ViewBag.Titulo = "Campaña de Lentes 2x1";
            ViewBag.Segmento = "Clientes frecuentes";
            ViewBag.FechaEnvio = "2025-04-15";
            ViewBag.Mensaje = "¡Aprovecha la promoción 2x1 en lentes recetados durante todo el mes!";
            return View();
        }

        public ActionResult Editar(int id = 1)
        {
            ViewBag.Titulo = "Campaña Lentes 2x1";
            ViewBag.Segmento = "Clientes frecuentes";
            ViewBag.FechaEnvio = "2025-04-15";
            ViewBag.Mensaje = "Aprovecha nuestra promo 2x1 en abril.";
            return View();
        }

        [HttpPost]
        public ActionResult Editar(FormCollection collection)
        {
            string titulo = collection["Titulo"];
            string segmento = collection["Segmento"];
            string fecha = collection["FechaEnvio"];
            string mensaje = collection["Mensaje"];

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(segmento) ||
                string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(mensaje))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios para editar la campaña.";
                return RedirectToAction("Editar", new { id = 1 });
            }

            TempData["SuccessMessage"] = "La campaña fue actualizada correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Eliminar(int id)
        {
            TempData["SuccessMessage"] = "La campaña fue eliminada exitosamente.";
            return RedirectToAction("Index");
        }
    }
}
