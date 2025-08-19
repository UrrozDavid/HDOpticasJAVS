using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Controllers
{
    public class DashboardController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        public ActionResult Index()
        {
            // Total ventas por mes del año actual
            var currentYear = DateTime.Now.Year;
            var ventasAgrupadas = db.PuntoVenta
                .Where(v => v.Fecha_Venta.HasValue && v.Fecha_Venta.Value.Year == currentYear && v.Estado == "A")
                .GroupBy(v => v.Fecha_Venta.Value.Month)
                .Select(g => new
                {
                    Mes = g.Key,
                    Total = g.Sum(v => v.Total ?? 0)
            }).ToList();

            var ventasMensuales = Enumerable.Range(1, 12).Select(m => new {
                Mes = m,
                Total = ventasAgrupadas.FirstOrDefault(v => v.Mes == m)?.Total ?? 0
            }).ToList();

            // Desempeño (podemos asumir que es la cantidad de ventas por mes)
            var desempeño = db.PuntoVenta
                .Where(v => v.Fecha_Venta.HasValue && v.Fecha_Venta.Value.Year == currentYear && v.Estado == "A")
                .GroupBy(v => v.Fecha_Venta.Value.Month)
                .Select(g => new
                {
                    Mes = g.Key,
                    Cantidad = g.Count()
                }).ToList();

            // Gastos vs Ingresos (asumiendo que solo tenemos ingresos; si tienes gastos, cambia esto)
            var totalIngresos = db.PuntoVenta
                .Where(v => v.Fecha_Venta.HasValue && v.Fecha_Venta.Value.Year == currentYear && v.Estado == "A")
                .Sum(v => (decimal?)v.Total) ?? 0;

            var totalGastos = 1120m; // De momento no hay gastos en DB, fijo por ahora

            // Ventas por Método de Pago
            var ventasPorMetodoPago = db.PuntoVenta
                .Where(v => v.Fecha_Venta.HasValue && v.Estado == "A")
                .GroupBy(v => v.Id_MetodoPago)
                .Join(db.Parametro.Where(p => p.Id_TipoParametro == 3),
                    g => g.Key,
                    p => p.Id_Parametro,
                    (g, p) => new
                    {
                        Metodo = p.Nombre_Parametro,
                        Total = g.Sum(v => v.Total ?? 0)
                    })
                .ToList();
            ViewBag.VentasPorMetodoPago = ventasPorMetodoPago;

            // Productos más Vendidos
            var productosMasVendidos = db.DetalleVenta
                .Where(d => d.PuntoVenta.Estado == "A")
                .GroupBy(d => d.Id_Producto)
                .Select(g => new
                {
                    Producto = g.FirstOrDefault().Inventario.Nombre_Producto,
                    Cantidad = g.Sum(d => d.Cantidad ?? 0)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(10)
                .ToList();
            ViewBag.ProductosMasVendidos = productosMasVendidos;

            ViewBag.VentasMensuales = ventasMensuales;
            ViewBag.DesempeñoMensual = desempeño;
            ViewBag.TotalIngresos = totalIngresos;
            ViewBag.TotalGastos = totalGastos;

            // Notificaciones de Citas
            var hoy = DateTime.Today;
            var fechaLimite = hoy.AddDays(2);

            // Citas de hoy
            var citasHoy = db.Cita
                .Where(c => DbFunctions.TruncateTime(c.Fecha_Cita) == hoy && c.Estado == "A")
                .ToList();

            foreach (var cita in citasHoy)
            {
                cita.NombreEspecialista = db.Usuario
                    .Where(u => u.Cedula == cita.Cedula_Especialista)
                    .Select(u => u.Nombre + " " + u.Apellido1)
                    .FirstOrDefault();
            }

            // Citas próximas
            var citasProximas = db.Cita
                .Where(c => DbFunctions.TruncateTime(c.Fecha_Cita) > hoy &&
                            DbFunctions.TruncateTime(c.Fecha_Cita) <= fechaLimite &&
                            c.Estado == "A")
                .ToList();

            foreach (var cita in citasProximas)
            {
                cita.NombreEspecialista = db.Usuario
                    .Where(u => u.Cedula == cita.Cedula_Especialista)
                    .Select(u => u.Nombre + " " + u.Apellido1)
                    .FirstOrDefault();
            }

            // Citas atrasadas
            var citasAtrasadas = db.Cita
                .Where(c => DbFunctions.TruncateTime(c.Fecha_Cita) < hoy && c.Estado == "A")
                .ToList();

            foreach (var cita in citasAtrasadas)
            {
                cita.NombreEspecialista = db.Usuario
                    .Where(u => u.Cedula == cita.Cedula_Especialista)
                    .Select(u => u.Nombre + " " + u.Apellido1)
                    .FirstOrDefault();
            }

            ViewBag.CitasHoy = citasHoy;
            ViewBag.CitasProximas = citasProximas;
            ViewBag.CitasAtrasadas = citasAtrasadas;

            return View();
        }

        public JsonResult ResumenDiario(DateTime fecha)
        {
            var resumen = db.PuntoVenta
                .Where(v => DbFunctions.TruncateTime(v.Fecha_Venta) == fecha.Date && v.Estado == "A")
                .GroupBy(v => 1)
                .Select(g => new {
                    TotalVentas = g.Sum(x => x.Total ?? 0),
                    CantidadTransacciones = g.Count(),
                    MetodosPago = g.GroupBy(x => x.Id_MetodoPago)
                                   .Select(mp => new {
                                       Metodo = mp.Key,
                                       Total = mp.Sum(x => x.Total ?? 0),
                                       Cantidad = mp.Count()
                                   })
                }).FirstOrDefault();

            return Json(resumen, JsonRequestBehavior.AllowGet);
        }

        public JsonResult VentasPorMetodo()
        {
            var ventasPorMetodoPago = db.PuntoVenta
                .Where(v => v.Fecha_Venta.HasValue && v.Estado == "A")
                .GroupBy(v => v.Id_MetodoPago)
                .Join(db.Parametro.Where(p => p.Id_TipoParametro == 3),
                    g => g.Key,
                    p => p.Id_Parametro,
                    (g, p) => new
                    {
                        Metodo = p.Nombre_Parametro, // Nombre del método de pago
                        Total = g.Sum(v => v.Total ?? 0)
                    })
                .ToList();

            return Json(ventasPorMetodoPago, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ProductosMasVendidos()
        {
            var productosMasVendidos = db.DetalleVenta
                .Where(d => d.PuntoVenta.Estado == "A")
                .GroupBy(d => d.Id_Producto)
                .Select(g => new
                {
                    Producto = g.FirstOrDefault().Inventario.Nombre_Producto,
                    Cantidad = g.Sum(d => d.Cantidad ?? 0)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(10)
                .ToList();

            return Json(productosMasVendidos, JsonRequestBehavior.AllowGet);
        }
    }
}