using HDOpticasJAVS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class PuntoVentaController : Controller
    {
        private static List<PuntoVenta> ventas = new List<PuntoVenta>
        {
            new PuntoVenta { Id_Venta = 1, Id_Producto = 101, Cedula_Cliente = "123456789", Cantidad = 2, Subtotal = 100, IVA = 13, Total = 113, Fecha_Venta = new DateTime(2025, 5, 1), Hora_Venta = "10:30 AM", Id_MetodoPago = 1, NombreEmpleado = "Ana Vargas" },
            new PuntoVenta { Id_Venta = 2, Id_Producto = 102, Cedula_Cliente = "987654321", Cantidad = 1, Subtotal = 200, IVA = 26, Total = 226, Fecha_Venta = new DateTime(2025, 5, 2), Hora_Venta = "12:00 PM", Id_MetodoPago = 2, NombreEmpleado = "Luis Mora" },
            new PuntoVenta { Id_Venta = 3, Id_Producto = 103, Cedula_Cliente = "555555555", Cantidad = 1, Subtotal = 150, IVA = 19.5m, Total = 169.5m, Fecha_Venta = new DateTime(2025, 5, 2), Hora_Venta = "2:00 PM", Id_MetodoPago = 1, NombreEmpleado = "Ana Vargas" }
        };

        public ActionResult Index()
        {
            return View(ventas);
        }

        public ActionResult IndexAdmin()
        {
            List<int> notasAplicadas = NotaCreditoController.GetNotasAplicadas();
            ViewBag.NotasCreditoAplicadas = notasAplicadas;
            return View(ventas);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(PuntoVenta venta)
        {
            if (ModelState.IsValid)
            {
                venta.Id_Venta = ventas.Any() ? ventas.Max(v => v.Id_Venta) + 1 : 1;
                venta.Fecha_Venta = DateTime.Now.Date;
                venta.Hora_Venta = DateTime.Now.ToString("hh:mm tt");
                ventas.Add(venta);
                TempData["SuccessMessage"] = "Venta registrada correctamente.";
                return RedirectToAction("IndexAdmin");
            }

            TempData["ErrorMessage"] = "Error al registrar la venta.";
            return View(venta);
        }

        public ActionResult Detalles(int id)
        {
            var venta = ventas.FirstOrDefault(v => v.Id_Venta == id);
            if (venta == null)
                return HttpNotFound();
            return View(venta);
        }

        public ActionResult Edit(int id)
        {
            var venta = ventas.FirstOrDefault(v => v.Id_Venta == id);
            if (venta == null)
                return HttpNotFound();
            return View(venta);
        }

        [HttpPost]
        public ActionResult Edit(PuntoVenta venta)
        {
            var original = ventas.FirstOrDefault(v => v.Id_Venta == venta.Id_Venta);
            if (original == null)
                return HttpNotFound();

            original.Id_Producto = venta.Id_Producto;
            original.Cedula_Cliente = venta.Cedula_Cliente;
            original.Cantidad = venta.Cantidad;
            original.Subtotal = venta.Subtotal;
            original.IVA = venta.IVA;
            original.Total = venta.Total;
            original.Id_MetodoPago = venta.Id_MetodoPago;

            TempData["SuccessMessage"] = "Venta actualizada correctamente.";
            return RedirectToAction("IndexAdmin");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var venta = ventas.FirstOrDefault(v => v.Id_Venta == id);
            if (venta != null)
            {
                ventas.Remove(venta);
                TempData["SuccessMessage"] = "Venta eliminada correctamente.";
            }
            return RedirectToAction("IndexAdmin");
        }

        public ActionResult CrearNotaCredito(int id)
        {
            return RedirectToAction("Crear", "NotaCredito", new { ventaId = id });
        }

        public ActionResult ReporteMetodoPago()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ReporteMetodoPago(string metodoPago)
        {
            var ventasFiltradas = ventas.Where(v =>
                (metodoPago == "Efectivo" && v.Id_MetodoPago == 1) ||
                (metodoPago == "Tarjeta" && v.Id_MetodoPago == 2) ||
                (metodoPago == "NotaCredito" && v.Id_MetodoPago == 3)
            ).ToList();

            ViewBag.MetodoSeleccionado = metodoPago;
            ViewBag.Total = ventasFiltradas.Sum(v => v.Total);
            ViewBag.Cantidad = ventasFiltradas.Count;

            return View(ventasFiltradas);
        }

        public ActionResult ReporteProductoMasVendido()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ReporteProductoMasVendido(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var rango = ventas;

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                rango = ventas.Where(v => v.Fecha_Venta >= fechaInicio && v.Fecha_Venta <= fechaFin).ToList();
            }

            var productosVendidos = rango
                .GroupBy(v => v.Id_Producto)
                .Select(g => new
                {
                    IdProducto = g.Key,
                    CantidadTotal = g.Sum(v => v.Cantidad),
                    MontoTotal = g.Sum(v => v.Total)
                })
                .OrderByDescending(g => g.CantidadTotal)
                .ToList();

            ViewBag.Rango = $"{fechaInicio?.ToShortDateString()} - {fechaFin?.ToShortDateString()}";
            return View(productosVendidos);
        }

        public ActionResult ReportePorEmpleado()
        {
            var reporte = ventas
                .GroupBy(v => v.NombreEmpleado)
                .Select(g => new
                {
                    Empleado = g.Key,
                    TotalVentas = g.Sum(v => v.Total),
                    CantidadVentas = g.Count()
                }).ToList();

            ViewBag.Reporte = reporte;
            return View();
        }


        public FileResult ExportarReporteEmpleado()
        {
            var reporte = ventas
                .GroupBy(v => v.NombreEmpleado)
                .Select(g => new
                {
                    Empleado = g.Key,
                    TotalVentas = g.Sum(v => v.Total),
                    CantidadVentas = g.Count()
                }).ToList();

            var csv = "Empleado,Total Vendido,Cantidad de Ventas\n";
            foreach (var item in reporte)
            {
                csv += $"{item.Empleado},{item.TotalVentas},{item.CantidadVentas}\n";
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "reporte_por_empleado.csv");
        }

        public ActionResult Tendencias()
        {
            var tendencias = ventas
                .GroupBy(v => new { v.Fecha_Venta.Year, v.Fecha_Venta.Month })
                .Select(g => new TendenciaVentas
                {
                    Periodo = $"{g.Key.Month}/{g.Key.Year}",
                    Total = g.Sum(v => v.Total)
                }).OrderBy(t => t.Periodo).ToList();

            return View(tendencias);
        }
    }
}
