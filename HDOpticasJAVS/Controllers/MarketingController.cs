using HDOpticasJAVS.Models;
using HDOpticasJAVS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS.Helpers;


namespace HDOpticasJAVS.Controllers
{
    public class MarketingController : Controller
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        public ActionResult Index()
        {
            return RedirectToAction("Historial");
        }

        public ActionResult Crear()
        {
            RevisarCampaniasProgramadas();

            var model = new CampaniaMarketingViewModel
            {
                Fecha_Inicio = DateTime.Today, // ⬅️ Establece una fecha válida por defecto
                ClientesSeleccionados = db.Cliente
         .Where(c => c.Estado == "A" && c.Usuario.Correo != null)
         .Select(c => new ClienteSeleccionado
         {
             Cedula = c.Cedula,
             NombreCompleto = c.Usuario.Nombre + " " + c.Usuario.Apellido1,
             Correo = c.Usuario.Correo,
             Seleccionado = false
         }).ToList()

            };

            return View(model);
        }

        [HttpPost]
        public ActionResult EnviarCampania(CampaniaMarketingViewModel model)
        {
            if (model.Fecha_Programada.HasValue && model.Fecha_Programada.Value.Date < DateTime.Today)
            {
                TempData["Mensaje"] = "⚠️ La fecha programada no puede ser en el pasado.";
                return RedirectToAction("Crear");
            }

            var campania = new CampaniaMarketing
            {
                Nombre_Campania = model.Nombre_Campania,
                Descripcion = model.Descripcion,
                Tipo = model.Tipo,
                Fecha_Inicio = model.Fecha_Inicio,
                Fecha_Programada = model.Fecha_Programada,
                Estado = model.Fecha_Programada.HasValue ? "P" : "A",
                UsuarioCreador = User.Identity.Name,
                FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            db.CampaniaMarketing.Add(campania);
            db.SaveChanges();

            foreach (var cliente in model.ClientesSeleccionados.Where(c => c.Seleccionado))
            {
                db.CampaniaCliente.Add(new CampaniaCliente
                {
                    Id_Campania = campania.Id_Campania,
                    Cedula_Cliente = cliente.Cedula,
                    Correo_Cliente = cliente.Correo,
                    Estado = "A",
                    UsuarioCreador = User.Identity.Name,
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            db.SaveChanges(); 

            
            if (!model.Fecha_Programada.HasValue)
            {
                CampaniaHelper.ProcesarCampaniaPorId(campania.Id_Campania);
            }

            TempData["Mensaje"] = "✅ Campaña creada correctamente.";
            return RedirectToAction("Historial");
        }


        private void RevisarCampaniasProgramadas()
        {
            var hoy = DateTime.Today;

            var campañas = db.CampaniaMarketing
                .Where(c => c.Estado == "P" && c.Fecha_Programada != null && c.Fecha_Programada == hoy)
                .ToList();

            foreach (var c in campañas)
            {
                c.Estado = "A";
                c.Fecha_Inicio = hoy;
                c.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                c.UsuarioModificador = User.Identity.Name;

                db.Entry(c).State = System.Data.Entity.EntityState.Modified;
            }

            db.SaveChanges();
        }

        public ActionResult Historial()
        {
            var campañas = db.CampaniaMarketing
                .OrderByDescending(c => c.Id_Campania)
                .Select(c => new CampaniaMarketingViewModel
                {
                    Id_Campania = c.Id_Campania,
                    Nombre_Campania = c.Nombre_Campania,
                    Descripcion = c.Descripcion,
                    Tipo = c.Tipo,
                    Fecha_Inicio = c.Fecha_Inicio ?? DateTime.Today,
                    Fecha_Fin = c.Fecha_Fin,
                    Fecha_Programada = c.Fecha_Programada,
                    Estado = c.Estado  
                })
                .ToList();

            return View(campañas);
        }



        public ActionResult EditarHistorial(int id)
        {
            var campania = db.CampaniaMarketing.Find(id);
            if (campania == null)
                return HttpNotFound();

            var model = new CampaniaMarketingViewModel
            {
                Id_Campania = campania.Id_Campania,
                Nombre_Campania = campania.Nombre_Campania,
                Descripcion = campania.Descripcion,
                Tipo = campania.Tipo,
                Fecha_Inicio = campania.Fecha_Inicio ?? DateTime.Today,
                Fecha_Programada = campania.Fecha_Programada
            };

            return View("EditarHistorial", model);
        }

        [HttpPost]
        public ActionResult EditarHistorial(CampaniaMarketingViewModel model)
        {
            if (model.Fecha_Programada.HasValue && model.Fecha_Programada.Value.Date < DateTime.Today)
            {
                TempData["Mensaje"] = "⚠️ La fecha programada no puede ser en el pasado.";
                return View("EditarHistorial", model);
            }

            if (ModelState.IsValid)
            {
                var campania = db.CampaniaMarketing.Find(model.Id_Campania);
                if (campania == null)
                    return HttpNotFound();

                campania.Nombre_Campania = model.Nombre_Campania;
                campania.Descripcion = model.Descripcion;
                campania.Tipo = model.Tipo;
                campania.Fecha_Inicio = model.Fecha_Inicio;
                campania.Fecha_Programada = model.Fecha_Programada;
                campania.UsuarioModificador = User.Identity.Name;
                campania.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                db.Entry(campania).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                TempData["Mensaje"] = "✅ Campaña actualizada correctamente.";
                return RedirectToAction("Historial");
            }

            return View("EditarHistorial", model);
        }

        public ActionResult Reporte(int id)
        {
            var campania = db.CampaniaMarketing.Find(id);
            if (campania == null)
                return HttpNotFound();

            var total = db.CampaniaCliente.Count(c => c.Id_Campania == id);
            var abiertos = db.CampaniaMetrica.Count(m => m.Id_Campania == id && m.Abierto == true);
            var clicks = db.CampaniaMetrica.Count(m => m.Id_Campania == id && m.Click == true);

            var model = new CampaniaReporteViewModel
            {
                Id_Campania = id,
                Nombre_Campania = campania.Nombre_Campania,
                Descripcion = campania.Descripcion,
                TotalDestinatarios = total,
                TotalAbiertos = abiertos,
                TotalClicks = clicks,
                PorcentajeApertura = total > 0 ? Math.Round((decimal)abiertos / total * 100, 2) : 0,
                PorcentajeClicks = total > 0 ? Math.Round((decimal)clicks / total * 100, 2) : 0
            };

            return View(model);
        }

        public ActionResult ExportarExcel(int id)
        {
            var data = db.CampaniaMetrica
                .Where(m => m.Id_Campania == id)
                .Select(m => new
                {
                    m.Cedula_Cliente,
                    Cliente = m.Cliente.Usuario.Nombre + " " + m.Cliente.Usuario.Apellido1,
                    Abierto = m.Abierto == true ? "Sí" : "No",
                    Click = m.Click == true ? "Sí" : "No",
                    m.FechaRegistro
                }).ToList();

            var grid = new System.Web.UI.WebControls.GridView();
            grid.DataSource = data;
            grid.DataBind();

            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment; filename=ReporteCampania_" + id + ".xls");
            Response.ContentType = "application/excel";
            System.IO.StringWriter sw = new System.IO.StringWriter();
            System.Web.UI.HtmlTextWriter htw = new System.Web.UI.HtmlTextWriter(sw);
            grid.RenderControl(htw);
            Response.Write(sw.ToString());
            Response.End();

            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult Finalizar(int id)
        {
            var campania = db.CampaniaMarketing.Find(id);
            if (campania == null)
                return HttpNotFound();

            campania.Estado = "I";
            campania.Fecha_Fin = DateTime.Today;
            campania.UsuarioModificador = User.Identity.Name;
            campania.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            db.Entry(campania).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            TempData["Mensaje"] = "✅ Campaña finalizada correctamente.";
            return RedirectToAction("Historial");
        }

        [HttpPost]
        public ActionResult Eliminar(int id)
        {
            var campania = db.CampaniaMarketing.Find(id);
            if (campania == null)
                return HttpNotFound();

            var relacionadosCliente = db.CampaniaCliente.Where(c => c.Id_Campania == id).ToList();
            db.CampaniaCliente.RemoveRange(relacionadosCliente);

            var relacionadosMetrica = db.CampaniaMetrica.Where(m => m.Id_Campania == id).ToList();
            db.CampaniaMetrica.RemoveRange(relacionadosMetrica);

            db.CampaniaMarketing.Remove(campania);
            db.SaveChanges();

            TempData["Mensaje"] = "✅ Campaña eliminada correctamente.";
            return RedirectToAction("Historial");
        }
        [HttpPost]
        public ActionResult EnviarCampaniaManual(int id)
        {
            try
            {
                var campania = db.CampaniaMarketing.Find(id);
                if (campania == null)
                    return HttpNotFound();

                HDOpticasJAVS.Helpers.CampaniaHelper.ProcesarCampaniaPorId(id);

                TempData["Mensaje"] = "✅ Correos enviados manualmente para la campaña.";
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"⚠️ Error al enviar la campaña manualmente: {ex.Message}";
            }

            return RedirectToAction("Historial");
        }
        [HttpGet]
        public ActionResult ContarApertura(int idCampania, string cedulaCliente)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var metrica = db.CampaniaMetrica
                    .FirstOrDefault(m => m.Id_Campania == idCampania && m.Cedula_Cliente == cedulaCliente);

                if (metrica != null && metrica.Abierto != true)
                {
                    metrica.Abierto = true;
                    metrica.FechaRegistro = DateTime.Now;
                    db.SaveChanges();
                }

                // Devolver imagen invisible 1x1
                byte[] imageBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00,
                                         0x01, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00,
                                         0xFF, 0xFF, 0xFF, 0x21, 0xF9, 0x04, 0x01, 0x00,
                                         0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00,
                                         0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44,
                                         0x01, 0x00, 0x3B };

                return File(imageBytes, "image/gif");
            }
           

        }
            
        [HttpGet]
        public ActionResult ContarClick(int IdCampania, string CedulaCliente, string redirect)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var metrica = db.CampaniaMetrica
                    .FirstOrDefault(m => m.Id_Campania == IdCampania && m.Cedula_Cliente == CedulaCliente);

                if (metrica != null)
                {
                    metrica.Click = true;
                    db.SaveChanges();
                }

                return Redirect(redirect ?? "https://www.hdopticas.com");
            }
        }


    }
}
