using HDOpticasJAVS;
using HDOpticasJAVS.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace hdopticasjavs.controllers
{
    public class Contabilidadcontroller : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // get: contabilidad
        public ActionResult index(string usuariofiltro)
        {
            var listausuarios = db.Contabilidad
                                  .Select(c => c.Usuario_Registro)
                                  .Distinct()
                                  .OrderBy(u => u)
                                  .ToList();

            ViewBag.usuariofiltro = new SelectList(listausuarios);

            var contabilidad = db.Contabilidad.AsQueryable();

            if (!string.IsNullOrEmpty(usuariofiltro))
            {
                contabilidad = contabilidad.Where(c => c.Usuario_Registro == usuariofiltro);
            }

            // diccionario id_tipomovimiento => nombre
            var parametros = db.Parametro.ToDictionary(p => p.Id_Parametro, p => p.Nombre_Parametro);
            ViewBag.parametros = parametros;

            ViewBag.totalsubtotal = contabilidad.Sum(c => (decimal?)c.Subtotal) ?? 0;
            ViewBag.totaltotal = contabilidad.Sum(c => (decimal?)c.Total) ?? 0;

            return View(contabilidad.ToList());
        }


        // get: contabilidad/details/5
        public async Task<ActionResult> details(int? id)
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



        // método para calcular saldo disponible real sumando los totales
        private decimal obtenersaldodisponible()
        {
            // suma todos los totales, que pueden ser positivos (ingresos) o negativos (egresos)
            return db.Contabilidad.Sum(c => (decimal?)c.Total) ?? 0m;
        }


        // get: contabilidad/create
        public ActionResult create()
        {
            ViewBag.id_producto = new SelectList(db.Inventario, "id_producto", "nombre_producto");
            ViewBag.id_tipomovimiento = new SelectList(db.Parametro, "id_parametro", "nombre_parametro");
            ViewBag.usuario_registro = new SelectList(db.Usuario, "cedula", "nombre");

            // obtener saldo real desde base de datos
            ViewBag.saldodisponible = obtenersaldodisponible();

            return View();
        }

        // id_venta dummy para contabilidad manual
        const int idventadummy = 0;

        // post: contabilidad/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> create([Bind(Include = "id_contabilidad,id_producto,cantidad,preciounitario,subtotal,descuento,iva,total,fecha_registro,usuario_registro,id_tipomovimiento,estado,usuariocreador,fechacreacion,usuariomodificador,fechamodificacion,tipooperacion,tiposervicio,tipomovimientoingresoegreso,origenmovimiento")] Contabilidad contabilidad)
        {
            // obtener saldo real antes de validar
            decimal saldodisponible = obtenersaldodisponible();

            if (contabilidad.TipoOperacion?.ToLower() != "venta")
            {
                contabilidad.Id_Venta = idventadummy;
            }

            if (string.IsNullOrEmpty(contabilidad.TipoOperacion))
            {
                ModelState.AddModelError("tipooperacion", "debe seleccionar un tipo de operación.");
            }

            if (string.IsNullOrEmpty(contabilidad.TipoMovimientoIngresoEgreso))
            {
                ModelState.AddModelError("tipomovimientoingresoegreso", "debe seleccionar si es ingreso o egreso.");
            }

            switch (contabilidad.TipoOperacion?.ToLower())
            {
                case "producto":
                    var producto = db.Inventario.Find(contabilidad.Id_Producto);
                    if (producto == null)
                    {
                        ModelState.AddModelError("id_producto", "producto no encontrado.");
                    }
                    else if (contabilidad.Cantidad > producto.Stock)
                    {
                        ModelState.AddModelError("cantidad", $"la cantidad no puede ser mayor que el stock disponible ({producto.Stock}).");
                    }
                    break;

                case "servicio":
                    if (string.IsNullOrEmpty(contabilidad.TipoServicio))
                        ModelState.AddModelError("tiposervicio", "debe seleccionar un tipo de servicio.");
                    break;

                case "pago":
                    if (string.IsNullOrEmpty(contabilidad.Usuario_Registro))
                        ModelState.AddModelError("usuario_registro", "debe seleccionar a quién se realiza el pago.");
                    if (contabilidad.Total <= 0)
                        ModelState.AddModelError("total", "el monto del pago debe ser mayor que cero.");
                    break;

                case "ingreso":
                    if (contabilidad.Total <= 0)
                        ModelState.AddModelError("total", "el monto del ingreso debe ser mayor que cero.");
                    break;

                default:
                    ModelState.AddModelError("tipooperacion", "tipo de operación no válido.");
                    break;
                }

                // ajustar signo del total según tipo y egreso/ingreso
                var totalabs = Math.Abs(Convert.ToDecimal(contabilidad.Total));

                if (contabilidad.TipoMovimientoIngresoEgreso?.ToLower() == "egreso")
                {
                    contabilidad.Total = -totalabs;

                    // validación: no permitir egresos que superen el saldo disponible
                    if (totalabs > saldodisponible)
                    {
                        ModelState.AddModelError("total", $"el monto del egreso excede el saldo disponible ({saldodisponible:c}).");
                    }
                }
                else
                {
                    contabilidad.Total = totalabs;
                }

                // validación final antes de guardar
                if (!ModelState.IsValid)
                {
                    ViewBag.id_producto = new SelectList(db.Inventario, "id_producto", "nombre_producto", contabilidad.Id_Producto);
                    ViewBag.id_tipomovimiento = new SelectList(db.Parametro, "id_parametro", "nombre_parametro", contabilidad.Id_TipoMovimiento);
                    ViewBag.usuario_registro = new SelectList(db.Usuario, "cedula", "nombre", contabilidad.Usuario_Registro);
                    ViewBag.saldodisponible = saldodisponible; // para mostrar en vista
                    return View(contabilidad);
                }

                // actualizar stock si es producto
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

                TempData["successmessage"] = "¡registro contable creado exitosamente!";
                return RedirectToAction("index");
            } 

            // get: contabilidad/edit/5
            public async Task<ActionResult> edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var contabilidad = await db.Contabilidad.FindAsync(id);
            if (contabilidad == null)
                return HttpNotFound();

            // aquí cargas las listas para dropdowns
            ViewBag.id_producto = new SelectList(db.Inventario, "id_producto", "nombre_producto", contabilidad.Id_Producto);
            ViewBag.id_venta = new SelectList(db.PuntoVenta, "id_venta", "id_venta", contabilidad.Id_Venta);
            ViewBag.usuario_registro = new SelectList(db.Usuario, "cedula", "nombre", contabilidad.Usuario_Registro);
            ViewBag.id_tipomovimiento = new SelectList(db.Parametro, "id_parametro", "nombre_parametro", contabilidad.Id_TipoMovimiento);

            return View(contabilidad);
        }

        // post: contabilidad/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> edit(Contabilidad contabilidad)
        {
            if (contabilidad.Id_Venta != 0)
            {
                bool existeduplicado = db.Contabilidad.Any(c => c.Id_Venta == contabilidad.Id_Venta
                          && c.Id_Contabilidad != contabilidad.Id_Contabilidad);
                if (existeduplicado)
                {
                    ModelState.AddModelError("id_venta", "ya existe un registro con esta venta asignada.");
                    cargarviewbags(contabilidad);
                    return View(contabilidad);
                }
            }

            if (ModelState.IsValid)
            {
                db.Entry(contabilidad).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["successmessage"] = "¡registro actualizado correctamente!";
                return RedirectToAction("index");
            }

            cargarviewbags(contabilidad);
            return View(contabilidad);
        }
        // método privado para evitar repetir viewbags
        private void cargarviewbags(Contabilidad contabilidad)
        {
            ViewBag.id_producto = new SelectList(db.Inventario, "id_producto", "nombre_producto", contabilidad.Id_Producto);
            ViewBag.id_tipomovimiento = new SelectList(db.Parametro, "id_parametro", "nombre_parametro", contabilidad.Id_TipoMovimiento);
            ViewBag.id_venta = new SelectList(db.PuntoVenta, "id_venta", "id_venta", contabilidad.Id_Venta);
            ViewBag.usuario_registro = new SelectList(db.Usuario, "cedula", "nombre", contabilidad.Usuario_Registro);
        }

        // get: contabilidad/delete/5
        public async Task<ActionResult> delete(int? id)
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

        // post: contabilidad/delete/5
        [HttpPost, ActionName("delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> deleteconfirmed(int id)
        {
            var contabilidad = await db.Contabilidad.FindAsync(id);
            if (contabilidad == null)
            {
                return HttpNotFound();
            }

            db.Contabilidad.Remove(contabilidad);
            await db.SaveChangesAsync();

            TempData["mensajeexito"] = "eliminación realizada correctamente";

            return RedirectToAction("index");
        }

        public ActionResult reporteegresosporcategoria()
        {
            // obtener categorías de egreso desde parametro si existe una categoría para ello, o como en el ejemplo:
            var categorias = db.Parametro.ToList();

            ViewBag.categoriasgastos = new SelectList(categorias, "id_parametro", "nombre_parametro");

            return View();
        }

        [HttpPost]
        public ActionResult generarreporteegresosporcategoria(int? id_tipomovimiento, DateTime? fechainicio, DateTime? fechafin)
        {
            if (fechainicio > fechafin)
            {
                ModelState.AddModelError("", "la fecha inicio no puede ser mayor que la fecha fin.");
                ViewBag.categoriasgastos = new SelectList(db.Parametro, "id_parametro", "nombre_parametro");
                return View("reporteegresosporcategoria");
            }

            var query = db.Contabilidad.AsQueryable();

            query = query.Where(c => c.TipoMovimientoIngresoEgreso == "egreso");

            if (id_tipomovimiento.HasValue)
                query = query.Where(c => c.Id_TipoMovimiento == id_tipomovimiento);

            if (fechainicio.HasValue)
                query = query.Where(c => c.Fecha_Registro >= fechainicio.Value);

            if (fechafin.HasValue)
                query = query.Where(c => c.Fecha_Registro <= fechafin.Value);

            var egresos = query
                .GroupBy(c => c.Id_TipoMovimiento)
                .Select(g => new EgresoCategoria
                {
                    Categoria = db.Parametro
                    .Where(p => p.Id_Parametro == g.Key)
                    .Select(p => p.Nombre_Parametro)
                    .FirstOrDefault() ?? "sin categoría",
                    Total = (decimal)g.Sum(x => x.Total)
                })
               .ToList();

            var model = new ReporteEgresosPorCategoriaViewModel
            {
                Id_TipoMovimiento = id_tipomovimiento,
                FechaInicio = fechainicio,
                FechaFin = fechafin,
            };

            ViewBag.categoriasgastos = new SelectList(db.Parametro, "id_parametro", "nombre_parametro");

            return View("reporteegresosporcategoria", model);
        }

    }
}
