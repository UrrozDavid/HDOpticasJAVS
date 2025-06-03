using HDOpticasJAVS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class ReporteVentaController : Controller
    {
        private static List<ReporteVenta> ventas = new List<ReporteVenta>
        {
            new ReporteVenta { Id_Venta = 1, Fecha_Venta = DateTime.Today, MetodoPago = "Efectivo", Total = 113, Empleado = "Cajero1" },
            new ReporteVenta { Id_Venta = 2, Fecha_Venta = DateTime.Today, MetodoPago = "Tarjeta", Total = 226, Empleado = "Cajero2" },
            new ReporteVenta { Id_Venta = 3, Fecha_Venta = DateTime.Today.AddDays(-1), MetodoPago = "NotaCredito", Total = 90, Empleado = "Cajero1" }
        };

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ReporteDiario(DateTime fecha)
        {
            var ventasDelDia = ventas.Where(v => v.Fecha_Venta.Date == fecha.Date).ToList();
            ViewBag.TotalVentas = ventasDelDia.Sum(v => v.Total);
            ViewBag.CantidadTransacciones = ventasDelDia.Count;
            ViewBag.MetodosPago = ventasDelDia.GroupBy(v => v.MetodoPago)
                                              .Select(g => new { Metodo = g.Key, Total = g.Sum(x => x.Total) })
                                              .ToList();
            return View("Index", ventasDelDia);
        }
        public ActionResult ReporteMensual()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ReporteMensual(DateTime fechaInicio, DateTime fechaFin)
        {
            var ventas = ObtenerVentasSimuladas();
            var ventasFiltradas = ventas.Where(v => v.Fecha_Venta.Date >= fechaInicio.Date && v.Fecha_Venta.Date <= fechaFin.Date).ToList();

            ViewBag.TotalVentas = ventasFiltradas.Sum(v => v.Total);
            ViewBag.TotalTransacciones = ventasFiltradas.Count;
            ViewBag.FechaInicio = fechaInicio.ToShortDateString();
            ViewBag.FechaFin = fechaFin.ToShortDateString();

            return View(ventasFiltradas);
        }
        private List<PuntoVenta> ObtenerVentasSimuladas()
        {
            return new List<PuntoVenta>
    {
        new PuntoVenta { Id_Venta = 1, Id_Producto = 101, Cantidad = 2, Subtotal = 100, IVA = 13, Total = 113, Fecha_Venta = DateTime.Today, Hora_Venta = "10:30 AM", Id_MetodoPago = 1, NombreEmpleado = "Ana Vargas" },
        new PuntoVenta { Id_Venta = 2, Id_Producto = 102, Cantidad = 1, Subtotal = 200, IVA = 26, Total = 226, Fecha_Venta = DateTime.Today, Hora_Venta = "12:00 PM", Id_MetodoPago = 2, NombreEmpleado = "Luis Mora" }
    };
        }

    }
}