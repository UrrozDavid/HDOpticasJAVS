using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class InventarioController : Controller
    {
        // Lista de productos simulada en memoria
        private static List<Producto> productos = new List<Producto>
        {
            new Producto { Id_Producto = 1, Nombre_Producto = "Lente 2x1", Codigo_Producto = "001", Stock = 100, Precio = 5000, Id_Proveedor = 1, Descripcion = "Lentes ópticos" },
            new Producto { Id_Producto = 2, Nombre_Producto = "Lente de Sol", Codigo_Producto = "002", Stock = 50, Precio = 8000, Id_Proveedor = 2, Descripcion = "Lentes de sol deportivos" }
        };

        public ActionResult Index()
        {
            ViewBag.Productos = productos;
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            string nombreProducto = collection["Nombre_Producto"];
            string codigoProducto = collection["Codigo_Producto"];
            string stock = collection["Stock"];
            string precio = collection["Precio"];
            string descripcion = collection["Descripcion"];
            string idProveedor = collection["Id_Proveedor"];

            if (string.IsNullOrWhiteSpace(nombreProducto) || string.IsNullOrWhiteSpace(codigoProducto) ||
                string.IsNullOrWhiteSpace(stock) || string.IsNullOrWhiteSpace(precio) ||
                string.IsNullOrWhiteSpace(descripcion) || string.IsNullOrWhiteSpace(idProveedor))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios para crear un producto.";
                return RedirectToAction("Crear");
            }

            int nuevoId = productos.Count > 0 ? productos.Max(p => p.Id_Producto) + 1 : 1;

            // Agregar producto a la lista en memoria
            productos.Add(new Producto
            {
                Id_Producto = nuevoId,
                Nombre_Producto = nombreProducto,
                Codigo_Producto = codigoProducto,
                Stock = int.Parse(stock),
                Precio = decimal.Parse(precio),
                Descripcion = descripcion,
                Id_Proveedor = int.Parse(idProveedor)
            });

            TempData["SuccessMessage"] = "Producto creado exitosamente.";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var producto = productos.FirstOrDefault(p => p.Id_Producto == id);
            if (producto == null)
                return HttpNotFound();

            ViewBag.Producto = producto;
            return View();
        }

        [HttpPost]
        public ActionResult Edit(FormCollection collection, int id)
        {
            var producto = productos.FirstOrDefault(p => p.Id_Producto == id);
            if (producto == null)
                return HttpNotFound();

            producto.Nombre_Producto = collection["Nombre_Producto"];
            producto.Codigo_Producto = collection["Codigo_Producto"];
            producto.Stock = int.Parse(collection["Stock"]);
            producto.Precio = decimal.Parse(collection["Precio"]);
            producto.Descripcion = collection["Descripcion"];
            producto.Id_Proveedor = int.Parse(collection["Id_Proveedor"]);

            TempData["SuccessMessage"] = "Producto actualizado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var producto = productos.FirstOrDefault(p => p.Id_Producto == id);
            if (producto != null)
            {
                productos.Remove(producto);
                TempData["SuccessMessage"] = "Producto eliminado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Producto no encontrado.";
            }

            return RedirectToAction("Index");
        }
    

    public ActionResult Details(int id)
        {
            var producto = productos.FirstOrDefault(p => p.Id_Producto == id);

            if (producto == null)
            {
                return HttpNotFound();
            }

            // Pasamos el producto como modelo a la vista
            return View(producto);
        }
    }

    // Clase para simular los productos
    public class Producto
    {
        public int Id_Producto { get; set; }
        public string Nombre_Producto { get; set; }
        public string Codigo_Producto { get; set; }
        public int Stock { get; set; }
        public decimal Precio { get; set; }
        public string Descripcion { get; set; }
        public int Id_Proveedor { get; set; }
    }

  


}
