using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using HDOpticasJAVS;

namespace HD_Opticas_JAVS.Controllers
{
    public class PuntoVentaController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        public ActionResult Index()
        {
            ViewBag.Productos = db.Inventario.Where(p => p.Estado == "A").ToList();
            ViewBag.MetodosPago = db.Parametro
                .Where(p => p.Id_TipoParametro == 3 && p.Estado == "A") // 2 = Tipo: Método de Pago
                .ToList();
            return View();
        }

        public ActionResult IndexAdmin()
        {
            var ventas = db.PuntoVenta.Where(v => v.Estado == "A").ToList();
            return View(ventas);
        }

        public ActionResult Create()
        {
            ViewBag.Productos = new SelectList(db.Inventario.Where(p => p.Estado == "A"), "Id_Producto", "Nombre_Producto");
            ViewBag.Clientes = new SelectList(db.Cliente.Where(c => c.Estado == "A"), "Cedula", "Cedula");
            ViewBag.MetodosPago = new SelectList(db.Parametro.Where(p => p.Id_TipoParametro == 2 && p.Estado == "A"), "Id_Parametro", "Nombre_Parametro");
            return View();
        }

        [HttpPost]
        public ActionResult Create(PuntoVenta venta)
        {
            try
            {
                venta.Estado = "A";
                venta.FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                venta.UsuarioCreador = User.Identity.Name;
                db.PuntoVenta.Add(venta);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Venta registrada correctamente.";
                return RedirectToAction("IndexAdmin");
            }
            catch
            {
                TempData["ErrorMessage"] = "Error al registrar la venta.";
                return RedirectToAction("Create");
            }
        }

        [HttpPost]
        public ActionResult Eliminar(int id)
        {
            var venta = db.PuntoVenta.Find(id);
            if (venta != null)
            {
                venta.Estado = "I";
                venta.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                venta.UsuarioModificador = User.Identity.Name;
                db.Entry(venta).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("IndexAdmin");
        }
    }
}