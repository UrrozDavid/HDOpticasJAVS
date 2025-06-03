using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class ContabilidadController : Controller
    {
        // Lista de ventas simulada en memoria
        private static List<Venta> ventas = new List<Venta>
        {
            new Venta { Id_Venta = 1, Id_Producto = 1, Total = 10000, Numero_Factura = "A001", Fecha = "2025-04-01", Monto = 5000, Cliente = "Juan Pérez", Tipo_Pago = "Efectivo", Status = "Pagado" },
            new Venta { Id_Venta = 2, Id_Producto = 2, Total = 16000, Numero_Factura = "A002", Fecha = "2025-04-02", Monto = 8000, Cliente = "María López", Tipo_Pago = "Tarjeta", Status = "Pendiente" }
        };

        public ActionResult Index()
        {
            ViewBag.Ventas = ventas;
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            string idProducto = collection["Id_Producto"];
            string total = collection["Total"];
            string numeroFactura = collection["Numero_Factura"];
            string fecha = collection["Fecha"];
            string monto = collection["Monto"];
            string cliente = collection["Cliente"];
            string tipoPago = collection["Tipo_Pago"];
            string status = collection["Status"];

            if (string.IsNullOrWhiteSpace(idProducto) || string.IsNullOrWhiteSpace(total) ||
                string.IsNullOrWhiteSpace(numeroFactura) || string.IsNullOrWhiteSpace(fecha) ||
                string.IsNullOrWhiteSpace(monto) || string.IsNullOrWhiteSpace(cliente) ||
                string.IsNullOrWhiteSpace(tipoPago) || string.IsNullOrWhiteSpace(status))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios para crear una venta.";
                return RedirectToAction("Create");
            }

            int nuevoId = ventas.Count > 0 ? ventas.Max(v => v.Id_Venta) + 1 : 1;

            // Agregar venta a la lista en memoria
            ventas.Add(new Venta
            {
                Id_Venta = nuevoId,
                Id_Producto = int.Parse(idProducto),
                Total = decimal.Parse(total),
                Numero_Factura = numeroFactura,
                Fecha = fecha,
                Monto = decimal.Parse(monto),
                Cliente = cliente,
                Tipo_Pago = tipoPago,
                Status = status
            });

            TempData["SuccessMessage"] = "Venta registrada exitosamente.";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var venta = ventas.FirstOrDefault(v => v.Id_Venta == id);
            if (venta == null)
                return HttpNotFound();

            return View(venta);  // Aquí pasamos el objeto venta directamente como el modelo
        }

        [HttpPost]
        public ActionResult Edit(FormCollection collection, int id)
        {
            var venta = ventas.FirstOrDefault(v => v.Id_Venta == id);
            if (venta == null)
                return HttpNotFound();

            venta.Id_Producto = int.Parse(collection["Id_Producto"]);
            venta.Total = decimal.Parse(collection["Total"]);
            venta.Numero_Factura = collection["Numero_Factura"];
            venta.Fecha = collection["Fecha"];
            venta.Monto = decimal.Parse(collection["Monto"]);
            venta.Cliente = collection["Cliente"];
            venta.Tipo_Pago = collection["Tipo_Pago"];
            venta.Status = collection["Status"];

            TempData["SuccessMessage"] = "Venta actualizada correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var venta = ventas.FirstOrDefault(v => v.Id_Venta == id);
            if (venta != null)
            {
                ventas.Remove(venta);
                TempData["SuccessMessage"] = "Venta eliminada exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Venta no encontrada.";
            }

            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
        {
            var venta = ventas.FirstOrDefault(v => v.Id_Venta == id);

            if (venta == null)
            {
                return HttpNotFound();
            }

            return View(venta);
        }
    }

    public class Venta
    {
        public int Id_Venta { get; set; }
        public int Id_Producto { get; set; }
        public decimal Total { get; set; }
        public string Numero_Factura { get; set; }
        public string Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Cliente { get; set; }
        public string Tipo_Pago { get; set; }
        public string Status { get; set; }
    }
}
