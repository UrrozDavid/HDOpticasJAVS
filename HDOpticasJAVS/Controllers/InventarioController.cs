using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HDOpticasJAVS;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Controllers
{
    public class InventarioController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // GET: Inventarios
        public async Task<ActionResult> Index(string filtro)
        {
            var inventario = db.Inventario.Include(i => i.Proveedor).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // Validación de caracteres especiales (ajustable según tu criterio)
                if (System.Text.RegularExpressions.Regex.IsMatch(filtro, @"[^a-zA-Z0-9\sáéíóúÁÉÍÓÚñÑ-]"))
                {
                    TempData["Mensaje"] = "⚠️ El valor ingresado no es válido.";
                    return View(new List<Inventario>());
                }

                // Buscar por Código de producto (si el valor ingresado coincide)
                var resultadoCodigo = await inventario
                    .Where(i => i.Codigo_Producto.Contains(filtro))
                    .ToListAsync();

                if (resultadoCodigo.Any())
                {
                    return View(resultadoCodigo);
                }

                // Si no se encuentra por código, buscar por nombre
                var resultadoNombre = await inventario
                    .Where(i => i.Nombre_Producto.Contains(filtro))
                    .ToListAsync();

                if (resultadoNombre.Any())
                {
                    return View(resultadoNombre);
                }

                TempData["Mensaje"] = "❌ No se encontró ningún producto con ese código o nombre.";
                return View(new List<Inventario>());
            }

            // Si no hay filtro, devolver todos
            return View(await inventario.ToListAsync());

         }


        // GET: Inventarios/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Inventario inventario = await db.Inventario.FindAsync(id);
            if (inventario == null)
            {
                return HttpNotFound();
            }
            return View(inventario);
        }

        // GET: Inventario/Create
        public ActionResult Create()
        {
            ViewBag.Id_Proveedor = new SelectList(db.Proveedor, "Id_Proveedor", "Nombre_Proveedor");
            return View(new HDOpticasJAVS.Models.ViewModels.Inventario());
        }

        // POST: Inventario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(HDOpticasJAVS.Models.ViewModels.Inventario model)
        {
            if (ModelState.IsValid)
            {
                // Validar si ya existe producto con ese nombre (ignorar mayúsculas/minúsculas)
                bool nombreExiste = db.Inventario.Any(p => p.Nombre_Producto.ToLower() == model.Nombre_Producto.ToLower());
                bool codigoExiste = db.Inventario.Any(p => p.Codigo_Producto.ToLower() == model.Codigo_Producto.ToLower());

                if (nombreExiste)
                {
                    ModelState.AddModelError("Nombre_Producto", "Ya existe un producto con ese nombre.");
                }

                if (codigoExiste)
                {
                    ModelState.AddModelError("Codigo_Producto", "Ya existe un producto con ese código.");
                }

                if (nombreExiste || codigoExiste)
                {
                    ViewBag.Id_Proveedor = new SelectList(db.Proveedor, "Id_Proveedor", "Nombre_Proveedor", model.Id_Proveedor);
                    return View(model);
                }
                // Mapear ViewModel a Entidad
                var entidad = new HDOpticasJAVS.Inventario
                {
                    Nombre_Producto = model.Nombre_Producto,
                    Codigo_Producto = model.Codigo_Producto,
                    Stock = model.Stock,
                    Precio = model.Precio,
                    Id_Proveedor = model.Id_Proveedor,
                    Descripcion = model.Descripcion,
                    Estado = model.Estado ?? "Activo", // asigna un estado por defecto si quieres
                    UsuarioCreador = model.UsuarioCreador,
                    FechaCreacion = model.FechaCreacion,
                    UsuarioModificador = model.UsuarioModificador,
                    FechaModificacion = model.FechaModificacion
                };

                TempData["Mensaje"] = "Producto creado correctamente.";
                db.Inventario.Add(entidad);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.Id_Proveedor = new SelectList(db.Proveedor, "Id_Proveedor", "Nombre_Proveedor", model.Id_Proveedor);
            return View(model);
        }

        // GET: Inventarios/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var entidad = await db.Inventario.FindAsync(id);
            if (entidad == null)
            {
                return HttpNotFound();
            }

            // Mapeo: Entidad → ViewModel
            var model = new HDOpticasJAVS.Models.ViewModels.Inventario
            {
                Id_Producto = entidad.Id_Producto,
                Nombre_Producto = entidad.Nombre_Producto,
                Codigo_Producto = entidad.Codigo_Producto,
                Stock = entidad.Stock ?? 0,
                Precio = entidad.Precio ?? 0m,
                Id_Proveedor = entidad.Id_Proveedor ?? 0,
                Descripcion = entidad.Descripcion,
                Estado = entidad.Estado,
                UsuarioCreador = entidad.UsuarioCreador,
                FechaCreacion = entidad.FechaCreacion ?? "",
                UsuarioModificador = entidad.UsuarioModificador,
                FechaModificacion = entidad.FechaModificacion ?? ""
            };

            ViewBag.Id_Proveedor = new SelectList(db.Proveedor, "Id_Proveedor", "Nombre_Proveedor", model.Id_Proveedor);
            return View(model);
        }

        // POST: Inventarios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(HDOpticasJAVS.Models.ViewModels.Inventario model)
        {
            var productoEnBD = await db.Inventario.FindAsync(model.Id_Producto);
            if (productoEnBD == null)
            {
                ModelState.AddModelError("", "El producto que intenta editar no existe.");
                ViewBag.Id_Proveedor = new SelectList(db.Proveedor, "Id_Proveedor", "Nombre_Proveedor", model.Id_Proveedor);
                return View(model);
            }

            model.Codigo_Producto = model.Codigo_Producto?.Trim();
            model.Nombre_Producto = model.Nombre_Producto?.Trim();

            // Validar manualmente binding de numéricos
            if (Request.Form["Stock"] != null)
            {
                int tempStock;
                if (!int.TryParse(Request.Form["Stock"], out tempStock))
                {
                    ModelState.AddModelError("Stock", "Debe ingresar un número válido para el stock.");
                }
            }
            if (Request.Form["Precio"] != null)
            {
                decimal tempPrecio;
                if (!decimal.TryParse(Request.Form["Precio"], out tempPrecio))
                {
                    ModelState.AddModelError("Precio", "Debe ingresar un número válido para el precio.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Id_Proveedor = new SelectList(db.Proveedor, "Id_Proveedor", "Nombre_Proveedor", model.Id_Proveedor);
                return View(model);
            }

            bool nombreDuplicado = db.Inventario.Any(p =>
                p.Id_Producto != model.Id_Producto &&
                p.Nombre_Producto.ToLower().Trim() == model.Nombre_Producto.ToLower());

            bool codigoDuplicado = db.Inventario.Any(p =>
                p.Id_Producto != model.Id_Producto &&
                p.Codigo_Producto.ToLower().Trim() == model.Codigo_Producto.ToLower());

            if (nombreDuplicado)
            {
                ModelState.AddModelError("Nombre_Producto", "Ya existe otro producto con este nombre.");
            }
            if (codigoDuplicado)
            {
                ModelState.AddModelError("Codigo_Producto", "Ya existe otro producto con este código.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Id_Proveedor = new SelectList(db.Proveedor, "Id_Proveedor", "Nombre_Proveedor", model.Id_Proveedor);
                return View(model);
            }

            productoEnBD.Nombre_Producto = model.Nombre_Producto;
            productoEnBD.Codigo_Producto = model.Codigo_Producto;
            productoEnBD.Stock = model.Stock;
            productoEnBD.Precio = model.Precio;
            productoEnBD.Id_Proveedor = model.Id_Proveedor;
            productoEnBD.Descripcion = model.Descripcion;
            productoEnBD.Estado = model.Estado;
            productoEnBD.UsuarioModificador = model.UsuarioModificador;
            productoEnBD.FechaModificacion = model.FechaModificacion;

            await db.SaveChangesAsync();

            TempData["Mensaje"] = "Producto editado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Inventarios/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            // Caso 4: id no seleccionado o inválido
            if (id == null || id <= 0)
            {
                TempData["Mensaje"] = "⚠️ No se ha seleccionado un producto válido para eliminar.";
                return RedirectToAction("Index");
            }

            var inventario = await db.Inventario.FindAsync(id);

            // Caso 2: producto no existe
            if (inventario == null)
            {
                TempData["Mensaje"] = "❌ El producto que intentó eliminar no se encuentra registrado.";
                return RedirectToAction("Index");
            }

            // Caso 1: producto encontrado, mostrar confirmación
            return View(inventario);
        }

        // POST: Inventarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var inventario = await db.Inventario.FindAsync(id);

            // Caso 2: producto no existe
            if (inventario == null)
            {
                TempData["Mensaje"] = "❌ No se pudo eliminar el producto porque no está registrado.";
                return RedirectToAction("Index");
            }

            // Caso 1: eliminar producto
            db.Inventario.Remove(inventario);
            await db.SaveChangesAsync();

            TempData["Mensaje"] = "✅ Producto eliminado correctamente.";
            return RedirectToAction("Index");
        }
        public JsonResult GetProductoDatos(int id)
        {
            var producto = db.Inventario
                .Where(p => p.Id_Producto == id)
                .Select(p => new { p.Precio, p.Stock })  // Agrego Stock
                .FirstOrDefault();

            return Json(producto, JsonRequestBehavior.AllowGet);
        }

        // GET: Inventarios/AjustarStock/5
        public async Task<ActionResult> AjustarStock(int id)
        {
            var producto = await db.Inventario.FindAsync(id);
            if (producto == null)
            {
                TempData["MensajeError"] = "Producto no encontrado.";
                return RedirectToAction("Index", "Inventario");
            }

            var model = new AjusteStockViewModel
            {
                Id_Producto = producto.Id_Producto,
                Nombre_Producto = producto.Nombre_Producto,
                StockActual = (int)producto.Stock // <-- asignamos el stock actual aquí
            };

            return View(model);
        }

        // POST: Inventarios/AjustarStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AjustarStock(AjusteStockViewModel model)
        {
            var producto = await db.Inventario.FindAsync(model.Id_Producto);
            if (producto == null)
            {
                TempData["MensajeError"] = "No se encontró el producto.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                model.Nombre_Producto = producto.Nombre_Producto; // para no perder el nombre si hay error
                return View(model);
            }

            int nuevoStock = (int)producto.Stock;

            if (model.Tipo == "Aumentar")
            {
                nuevoStock += model.Cantidad;
            }
            else if (model.Tipo == "Disminuir")
            {
                nuevoStock -= model.Cantidad;
                if (nuevoStock < 0)
                {
                    ModelState.AddModelError("Cantidad", "No se puede disminuir más del stock disponible.");
                    model.Nombre_Producto = producto.Nombre_Producto;
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("Tipo", "Tipo de ajuste inválido.");
                model.Nombre_Producto = producto.Nombre_Producto;
                return View(model);
            }

            producto.Stock = nuevoStock;
            producto.UsuarioModificador = User.Identity.Name;
            producto.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Opcional: aquí podrías registrar el motivo del ajuste en un log o tabla aparte

            await db.SaveChangesAsync();

            TempData["Mensaje"] = "Stock ajustado correctamente.";
            return RedirectToAction("Index");
        }


    }
}
