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
//using HDOpticasJAVS.Models.ViewModels;
using Microsoft.Ajax.Utilities;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Controllers
{
    public class ContabilidadController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // GET: Contabilidad
        public ActionResult Index(string usuarioFiltro)
        {
            var listaUsuarios = db.Contabilidad
                                  .Select(c => c.Usuario_Registro)
                                  .Distinct()
                                  .OrderBy(u => u)
                                  .ToList();

            ViewBag.UsuarioFiltro = new SelectList(listaUsuarios);

            var contabilidad = db.Contabilidad.AsQueryable();

            if (!string.IsNullOrEmpty(usuarioFiltro))
            {
                contabilidad = contabilidad.Where(c => c.Usuario_Registro == usuarioFiltro);
            }

            // Diccionario Id_TipoMovimiento => Nombre
            var parametros = db.Parametro.ToDictionary(p => p.Id_Parametro, p => p.Nombre_Parametro);
            ViewBag.Parametros = parametros;

            ViewBag.TotalSubtotal = contabilidad.Sum(c => (decimal?)c.Subtotal) ?? 0;
            ViewBag.TotalTotal = contabilidad.Sum(c => (decimal?)c.Total) ?? 0;

            return View(contabilidad.ToList());
        }



        // GET: Contabilidad/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contabilidad contabilidad = await db.Contabilidad.FindAsync(id);
            if (contabilidad == null)
            {
                return HttpNotFound();
            }
            return View(contabilidad);
        }



        // Método para calcular saldo disponible real sumando los totales
        private decimal ObtenerSaldoDisponible()
        {
            // Suma todos los Totales, que pueden ser positivos (ingresos) o negativos (egresos)
            return db.Contabilidad.Sum(c => (decimal?)c.Total) ?? 0m;
        }


        // GET: Contabilidad/Create
        public ActionResult Create()
        {
            ViewBag.Id_Producto = new SelectList(db.Inventario, "Id_Producto", "Nombre_Producto");
            ViewBag.Id_TipoMovimiento = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro");
            ViewBag.Usuario_Registro = new SelectList(db.Usuario, "Cedula", "Nombre");

            // Obtener saldo real desde base de datos
            ViewBag.SaldoDisponible = ObtenerSaldoDisponible();

            return View();
        }

        // Id_Venta dummy para contabilidad manual
        const int IdVentaDummy = 0;

        // POST: Contabilidad/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id_Contabilidad,Id_Producto,Cantidad,PrecioUnitario,Subtotal,Descuento,IVA,Total,Fecha_Registro,Usuario_Registro,Id_TipoMovimiento,Estado,UsuarioCreador,FechaCreacion,UsuarioModificador,FechaModificacion,TipoOperacion,TipoServicio,TipoMovimientoIngresoEgreso,OrigenMovimiento")] Contabilidad contabilidad)
        {
            // Obtener saldo real antes de validar
            decimal saldoDisponible = ObtenerSaldoDisponible();

            if (contabilidad.TipoOperacion?.ToLower() != "venta")
            {
                contabilidad.Id_Venta = IdVentaDummy;
            }

            if (string.IsNullOrEmpty(contabilidad.TipoOperacion))
            {
                ModelState.AddModelError("TipoOperacion", "Debe seleccionar un tipo de operación.");
            }

            if (string.IsNullOrEmpty(contabilidad.TipoMovimientoIngresoEgreso))
            {
                ModelState.AddModelError("TipoMovimientoIngresoEgreso", "Debe seleccionar si es ingreso o egreso.");
            }

            switch (contabilidad.TipoOperacion?.ToLower())
            {
                case "producto":
                    var producto = db.Inventario.Find(contabilidad.Id_Producto);
                    if (producto == null)
                    {
                        ModelState.AddModelError("Id_Producto", "Producto no encontrado.");
                    }
                    else if (contabilidad.Cantidad > producto.Stock)
                    {
                        ModelState.AddModelError("Cantidad", $"La cantidad no puede ser mayor que el stock disponible ({producto.Stock}).");
                    }
                    break;

                case "servicio":
                    if (string.IsNullOrEmpty(contabilidad.TipoServicio))
                        ModelState.AddModelError("TipoServicio", "Debe seleccionar un tipo de servicio.");
                    break;

                case "pago":
                    if (string.IsNullOrEmpty(contabilidad.Usuario_Registro))
                        ModelState.AddModelError("Usuario_Registro", "Debe seleccionar a quién se realiza el pago.");
                    if (contabilidad.Total <= 0)
                        ModelState.AddModelError("Total", "El monto del pago debe ser mayor que cero.");
                    break;

                case "ingreso":
                    if (contabilidad.Total <= 0)
                        ModelState.AddModelError("Total", "El monto del ingreso debe ser mayor que cero.");
                    break;

                default:
                    ModelState.AddModelError("TipoOperacion", "Tipo de operación no válido.");
                    break;
            }

            // Ajustar signo del total según tipo y egreso/ingreso
            var totalAbs = Math.Abs(Convert.ToDecimal(contabilidad.Total));

            if (contabilidad.TipoMovimientoIngresoEgreso?.ToLower() == "egreso")
            {
                contabilidad.Total = -totalAbs;

                // Validación: no permitir egresos que superen el saldo disponible
                if (totalAbs > saldoDisponible)
                {
                    ModelState.AddModelError("Total", $"El monto del egreso excede el saldo disponible ({saldoDisponible:C}).");
                }
            }
            else
            {
                contabilidad.Total = totalAbs;
            }

            // Validación final antes de guardar
            if (!ModelState.IsValid)
            {
                ViewBag.Id_Producto = new SelectList(db.Inventario, "Id_Producto", "Nombre_Producto", contabilidad.Id_Producto);
                ViewBag.Id_TipoMovimiento = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro", contabilidad.Id_TipoMovimiento);
                ViewBag.Usuario_Registro = new SelectList(db.Usuario, "Cedula", "Nombre", contabilidad.Usuario_Registro);
                ViewBag.SaldoDisponible = saldoDisponible; // Para mostrar en vista
                return View(contabilidad);
            }

            // Actualizar stock si es producto
            if (contabilidad.TipoOperacion.ToLower() == "producto")
            {
                var producto = db.Inventario.Find(contabilidad.Id_Producto);
                if (producto != null)
                {
                    producto.Stock -= contabilidad.Cantidad;
                    db.Entry(producto).State = EntityState.Modified;
                }
            }

            if (contabilidad.Fecha_Registro == default)
                contabilidad.Fecha_Registro = DateTime.Now;

            db.Contabilidad.Add(contabilidad);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "¡Registro contable creado exitosamente!";
            return RedirectToAction("Index");
        }


        // GET: Contabilidad/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var contabilidad = await db.Contabilidad.FindAsync(id);
            if (contabilidad == null)
                return HttpNotFound();

            // Aquí cargas las listas para dropdowns
            ViewBag.Id_Producto = new SelectList(db.Inventario, "Id_Producto", "Nombre_Producto", contabilidad.Id_Producto);
            ViewBag.Id_Venta = new SelectList(db.PuntoVenta, "Id_Venta", "Id_Venta", contabilidad.Id_Venta);
            ViewBag.Usuario_Registro = new SelectList(db.Usuario, "Cedula", "Nombre", contabilidad.Usuario_Registro);
            ViewBag.Id_TipoMovimiento = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro", contabilidad.Id_TipoMovimiento);

            return View(contabilidad);
        }

        // POST: Contabilidad/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Contabilidad contabilidad)
        {
            if (contabilidad.Id_Venta != 0)
            {
                bool existeDuplicado = db.Contabilidad.Any(c => c.Id_Venta == contabilidad.Id_Venta
                                                                && c.Id_Contabilidad != contabilidad.Id_Contabilidad);
                if (existeDuplicado)
                {
                    ModelState.AddModelError("Id_Venta", "Ya existe un registro con esta venta asignada.");
                    CargarViewBags(contabilidad);
                    return View(contabilidad);
                }
            }

            if (ModelState.IsValid)
            {
                db.Entry(contabilidad).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "¡Registro actualizado correctamente!";
                return RedirectToAction("Index");
            }

            CargarViewBags(contabilidad);
            return View(contabilidad);
        }
        // Método privado para evitar repetir ViewBags
        private void CargarViewBags(Contabilidad contabilidad)
        {
            ViewBag.Id_Producto = new SelectList(db.Inventario, "Id_Producto", "Nombre_Producto", contabilidad.Id_Producto);
            ViewBag.Id_TipoMovimiento = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro", contabilidad.Id_TipoMovimiento);
            ViewBag.Id_Venta = new SelectList(db.PuntoVenta, "Id_Venta", "Id_Venta", contabilidad.Id_Venta);
            ViewBag.Usuario_Registro = new SelectList(db.Usuario, "Cedula", "Nombre", contabilidad.Usuario_Registro);
        }

        // GET: Contabilidad/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contabilidad contabilidad = await db.Contabilidad.FindAsync(id);
            if (contabilidad == null)
            {
                return HttpNotFound();
            }
            return View(contabilidad);
        }

        // POST: Contabilidad/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var contabilidad = await db.Contabilidad.FindAsync(id);
            if (contabilidad == null)
            {
                return HttpNotFound();
            }

            db.Contabilidad.Remove(contabilidad);
            await db.SaveChangesAsync();

            TempData["MensajeExito"] = "Eliminación realizada correctamente";

            return RedirectToAction("Index");
        }

        public ActionResult ReporteEgresosPorCategoria()
        {
            // Obtener categorías de egreso desde Parametro si existe una categoría para ello, o como en el ejemplo:
            var categorias = db.Parametro.ToList();

            ViewBag.CategoriasGastos = new SelectList(categorias, "Id_Parametro", "Nombre_Parametro");

            return View();
        }

        [HttpPost]
        public ActionResult GenerarReporteEgresosPorCategoria(int? Id_TipoMovimiento, DateTime? FechaInicio, DateTime? FechaFin)
        {
            if (FechaInicio > FechaFin)
            {
                ModelState.AddModelError("", "La fecha inicio no puede ser mayor que la fecha fin.");
                ViewBag.CategoriasGastos = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro");
                return View("ReporteEgresosPorCategoria");
            }

            var query = db.Contabilidad.AsQueryable();

            query = query.Where(c => c.TipoMovimientoIngresoEgreso == "Egreso");

            if (Id_TipoMovimiento.HasValue)
                query = query.Where(c => c.Id_TipoMovimiento == Id_TipoMovimiento);

            if (FechaInicio.HasValue)
                query = query.Where(c => c.Fecha_Registro >= FechaInicio.Value);

            if (FechaFin.HasValue)
                query = query.Where(c => c.Fecha_Registro <= FechaFin.Value);

            var egresos = query
                .GroupBy(c => c.Id_TipoMovimiento)
                .Select(g => new EgresoCategoria
                {
                    Categoria = db.Parametro
                    .Where(p => p.Id_Parametro == g.Key)
                    .Select(p => p.Nombre_Parametro)
                    .FirstOrDefault() ?? "Sin Categoría",
                    Total = (decimal)g.Sum(x => x.Total)
                })
                .ToList();

            var model = new ReporteEgresosPorCategoriaViewModel
            {
                Id_TipoMovimiento = Id_TipoMovimiento,
                FechaInicio = FechaInicio,
                FechaFin = FechaFin,
                EgresosPorCategoria = egresos
            };

            ViewBag.CategoriasGastos = new SelectList(db.Parametro, "Id_Parametro", "Nombre_Parametro");

            return View("ReporteEgresosPorCategoria", model);
        }


    }
}
