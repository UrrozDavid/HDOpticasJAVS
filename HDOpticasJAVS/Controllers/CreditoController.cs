using System;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS;

namespace HDOpticasJAVS.Controllers
{
    public class CreditoController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // =========================================================
        // ADMINISTRACIÓN DE CRÉDITOS (solo admin)
        // =========================================================
        public ActionResult AdminCreditos()
        {
            var creditos = (from c in db.NotaCredito
                            join u in db.Usuario on c.Cedula_Cliente equals u.Cedula
                            select new HDOpticasJAVS.Models.CreditoAdminViewModel
                            {
                                Id_NotaCredito = c.Id_NotaCredito,
                                Cedula_Cliente = c.Cedula_Cliente,
                                NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                                MontoOtorgado = c.MontoOtorgado,
                                SaldoPendiente = c.SaldoPendiente,
                                FechaOtorgado = c.FechaOtorgado,
                                Estado = c.Estado
                            }).ToList();

            return View(creditos);
        }

        public ActionResult EditarCredito(int id)
        {
            var credito = db.NotaCredito.Find(id);
            if (credito == null)
                return HttpNotFound();

            return View(credito);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarCredito(NotaCredito model)
        {
            if (ModelState.IsValid)
            {
                var credito = db.NotaCredito.Find(model.Id_NotaCredito);
                if (credito == null)
                    return HttpNotFound();

                // Actualizamos solo campos editables
                credito.SaldoPendiente = model.SaldoPendiente;
                credito.Estado = model.Estado;
                credito.UsuarioModificador = User.Identity.Name;
                credito.FechaModificacion = DateTime.Now;

                db.SaveChanges();

                TempData["MensajeExito"] = "Crédito actualizado correctamente.";
                return RedirectToAction("AdminCreditos");
            }

            return View(model);
        }

        public ActionResult CancelarCredito(int id)
        {
            var credito = db.NotaCredito.Find(id);
            if (credito == null)
                return HttpNotFound();

            credito.Estado = "C";
            credito.SaldoPendiente = 0;
            credito.UsuarioModificador = User.Identity.Name;
            credito.FechaModificacion = DateTime.Now;

            db.SaveChanges();

            TempData["MensajeExito"] = "Crédito cancelado exitosamente.";
            return RedirectToAction("AdminCreditos");
        }

        public ActionResult MisCreditos()
        {
            string cedulaCliente = Session["Cedula"]?.ToString();
            //var cedulaCliente = User.Identity.Name; ESTO FALLA
            var creditos = db.NotaCredito
                             .Where(c => c.Cedula_Cliente == cedulaCliente)
                             .ToList();

            return View(creditos);
        }

        public ActionResult PagarCredito(int id)
        {
            var credito = db.NotaCredito.Find(id);
            if (credito == null)
                return HttpNotFound();

            return View(credito);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PagarCredito(int id, decimal monto, string metodo, string numeroTarjeta, string nombreTitular, string fechaVencimiento, string cvv)
        {
            var credito = db.NotaCredito.Find(id);
            if (credito == null)
                return HttpNotFound();

            if (monto <= 0)
            {
                ModelState.AddModelError("", "El monto debe ser mayor a 0.");
                return View(credito);
            }

            if (monto > credito.SaldoPendiente)
            {
                ModelState.AddModelError("", "El monto no puede ser mayor al saldo pendiente.");
                return View(credito);
            }

            var pago = new PagoCredito
            {
                Id_NotaCredito = credito.Id_NotaCredito,
                Monto = monto,
                Metodo = metodo,
                FechaPago = DateTime.Now
            };
            db.PagoCredito.Add(pago);

            credito.SaldoPendiente -= monto;
            credito.FechaUltimoPago = DateTime.Now;

            if (credito.SaldoPendiente <= 0)
            {
                credito.Estado = "C";
            }

            db.SaveChanges();

            TempData["MensajeExito"] = "Pago registrado correctamente.";
            return RedirectToAction("MisCreditos");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}