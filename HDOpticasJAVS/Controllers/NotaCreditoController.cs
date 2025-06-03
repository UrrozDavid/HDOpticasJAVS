using HDOpticasJAVS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class NotaCreditoController : Controller
    {
        // Simulación de almacenamiento en memoria
        private static List<NotaCredito> notas = new List<NotaCredito>();

        // GET: NotaCredito
        public ActionResult Index()
        {
            return View(notas);
        }

        // GET: NotaCredito/Crear?ventaId=#
        public ActionResult Crear(int ventaId)
        {
            ViewBag.Id_Venta = ventaId;
            return View();
        }

        // POST: NotaCredito/Crear
        [HttpPost]
        public ActionResult Crear(NotaCredito model)
        {
            if (ModelState.IsValid)
            {
                model.Id_NotaCredito = notas.Any() ? notas.Max(n => n.Id_NotaCredito) + 1 : 1;
                model.Fecha_Emision = DateTime.Now;
                model.Estado = "A";
                model.UsuarioCreador = "admin";
                model.FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                notas.Add(model);

                TempData["SuccessMessage"] = "Nota de crédito registrada.";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Datos inválidos.";
            return View(model);
        }

        // GET: NotaCredito/Aplicar?id=#
        public ActionResult Aplicar(int id)
        {
            ViewBag.IdNota = id;
            return View();
        }

        // POST: NotaCredito/Aplicar
        [HttpPost]
        public ActionResult Aplicar(int id, int nuevaVentaId)
        {
            var nota = notas.FirstOrDefault(n => n.Id_NotaCredito == id);
            if (nota == null)
                return HttpNotFound();

            nota.AplicadaEnVenta = nuevaVentaId;
            nota.Estado = "U";
            nota.UsuarioModificador = "admin";
            nota.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            TempData["SuccessMessage"] = "Nota de crédito aplicada.";
            return RedirectToAction("Index");
        }

        public static List<int> GetNotasAplicadas()
        {
            return notas
                .Where(n => n.Estado == "U" && n.AplicadaEnVenta.HasValue)
                .Select(n => n.AplicadaEnVenta.Value)
                .ToList();
        }
    }
}
