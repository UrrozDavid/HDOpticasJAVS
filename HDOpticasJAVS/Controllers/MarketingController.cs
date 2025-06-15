using HDOpticasJAVS.Models;
using HDOpticasJAVS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS.Helpers;
using System.Data.Entity;
using iTextSharp.text;
using iTextSharp.text.pdf;


namespace HDOpticasJAVS.Controllers
{
    public class MarketingController : BaseController
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
                Fecha_Inicio = DateTime.Today,
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
        public ActionResult ExportarPdf(int id)
        {
            var campania = db.CampaniaMarketing.Find(id);
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

            using (var ms = new System.IO.MemoryStream())
            {
                var doc = new iTextSharp.text.Document();
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD);
                var tableFont = iTextSharp.text.FontFactory.GetFont("Arial", 10);

                doc.Add(new iTextSharp.text.Paragraph("Reporte de campaña", titleFont));
                doc.Add(new iTextSharp.text.Paragraph("Nombre: " + campania.Nombre_Campania));
                doc.Add(new iTextSharp.text.Paragraph("Descripción: " + campania.Descripcion));
                doc.Add(new iTextSharp.text.Paragraph(" "));

                var table = new iTextSharp.text.pdf.PdfPTable(5);
                table.WidthPercentage = 100;
                table.AddCell("Cédula");
                table.AddCell("Cliente");
                table.AddCell("Abierto");
                table.AddCell("Click");
                table.AddCell("Fecha Registro");

                foreach (var item in data)
                {
                    table.AddCell(item.Cedula_Cliente);
                    table.AddCell(item.Cliente);
                    table.AddCell(item.Abierto);
                    table.AddCell(item.Click);
                    table.AddCell(item.FechaRegistro?.ToString("yyyy-MM-dd HH:mm") ?? "");
                }

                doc.Add(table);
                doc.Close();

                byte[] pdfBytes = ms.ToArray();
                return File(pdfBytes, "application/pdf", "ReporteCampania_" + id + ".pdf");
            }
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

                CampaniaHelper.ProcesarCampaniaPorId(id);

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
            var metrica = db.CampaniaMetrica
                .FirstOrDefault(m => m.Id_Campania == idCampania && m.Cedula_Cliente == cedulaCliente);

            if (metrica != null && metrica.Abierto != true)
            {
                metrica.Abierto = true;
                metrica.FechaRegistro = DateTime.Now;
                db.SaveChanges();
            }

            byte[] imageBytes = new byte[] {
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00,
                0x01, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xFF, 0xFF, 0xFF, 0x21, 0xF9, 0x04, 0x01, 0x00,
                0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44,
                0x01, 0x00, 0x3B
            };

            return File(imageBytes, "image/gif");
        }

        [HttpGet]
        public ActionResult ContarClick(int IdCampania, string CedulaCliente, string redirect)
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

        public ActionResult PromocionesCliente(int idCampania, string cedulaCliente = null)
        {
            var campania = db.CampaniaMarketing.FirstOrDefault(c => c.Id_Campania == idCampania);
            if (campania == null)
                return HttpNotFound();

            if (string.IsNullOrEmpty(cedulaCliente))
            {
                string usuarioSesion = Session["Usuario"]?.ToString();
                var usuario = db.Usuario.FirstOrDefault(u => u.Correo == usuarioSesion || u.Cedula == usuarioSesion);
                cedulaCliente = usuario?.Cedula;
            }

            if (string.IsNullOrEmpty(cedulaCliente))
            {
                TempData["Mensaje"] = "No se pudo identificar al cliente.";
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });
            }

            var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedulaCliente);
            ViewBag.NombreCliente = cliente?.Usuario?.Nombre + " " + cliente?.Usuario?.Apellido1;

            var yaAplicada = db.CampaniaMetrica.Any(m =>
                m.Id_Campania == idCampania &&
                m.Cedula_Cliente == cedulaCliente &&
                m.Click == true);

            var promocion = new PromocionViewModel
            {
                Titulo = campania.Nombre_Campania,
                Descripcion = campania.Descripcion,
                ImagenUrl = "https://i.postimg.cc/25V3s41w/Logo-hdopticas.png",
                IdCampania = idCampania,
                CedulaCliente = cedulaCliente,
                YaAplicada = yaAplicada
            };

            return View(new List<PromocionViewModel> { promocion });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AplicarPromocion(int idCampania, string cedulaCliente)
        {
            if (string.IsNullOrEmpty(cedulaCliente))
            {
                string usuarioSesion = Session["Usuario"]?.ToString();
                var usuario = db.Usuario.FirstOrDefault(u => u.Correo == usuarioSesion || u.Cedula == usuarioSesion);
                cedulaCliente = usuario?.Cedula;
            }

            if (string.IsNullOrEmpty(cedulaCliente))
            {
                TempData["Mensaje"] = "⚠️ No se pudo aplicar la promoción. Cliente inválido.";
                return RedirectToAction("Login", "Account");
            }

            var yaAplicada = db.CampaniaMetrica.Any(m =>
                m.Id_Campania == idCampania &&
                m.Cedula_Cliente == cedulaCliente &&
                m.Click == true);

            if (!yaAplicada)
            {
                var metrica = db.CampaniaMetrica.FirstOrDefault(m =>
                    m.Id_Campania == idCampania && m.Cedula_Cliente == cedulaCliente);

                if (metrica == null)
                {
                    metrica = new CampaniaMetrica
                    {
                        Id_Campania = idCampania,
                        Cedula_Cliente = cedulaCliente,
                        Click = true,
                        FechaRegistro = DateTime.Now
                    };
                    db.CampaniaMetrica.Add(metrica);
                }
                else
                {
                    metrica.Click = true;
                    metrica.FechaRegistro = DateTime.Now;
                    db.Entry(metrica).State = System.Data.Entity.EntityState.Modified;
                }

                db.SaveChanges();
                TempData["Mensaje"] = "✅ ¡Promoción aplicada con éxito!";
            }

            return RedirectToAction("PromocionesCliente", new { idCampania = idCampania, cedulaCliente = cedulaCliente });
        }
        [HttpGet]
        public ActionResult SegmentarClientes()
        {
            return View(new SegmentacionViewModel());
        }

        [HttpPost]
        public ActionResult SegmentarClientes(SegmentacionViewModel filtro)
        {
            bool sinFiltros = string.IsNullOrEmpty(filtro.Nombre)
                              && !filtro.EdadMinima.HasValue
                              && !filtro.EdadMaxima.HasValue
                              && string.IsNullOrEmpty(filtro.Tratamiento);

            if (sinFiltros)
            {
                ModelState.AddModelError("", "Debe ingresar al menos un criterio de segmentación (nombre, edad o tratamiento).");
                return View(filtro);
            }

            var query = db.Cliente.Include(c => c.Usuario).AsQueryable();

            if (!string.IsNullOrEmpty(filtro.Nombre))
                query = query.Where(c => c.Usuario.Nombre.Contains(filtro.Nombre));

            if (filtro.EdadMinima.HasValue)
                query = query.Where(c => c.Edad >= filtro.EdadMinima.Value);

            if (filtro.EdadMaxima.HasValue)
                query = query.Where(c => c.Edad <= filtro.EdadMaxima.Value);

            if (!string.IsNullOrEmpty(filtro.Tratamiento))
                query = query.Where(c => c.Padecimiento == filtro.Tratamiento);

            filtro.Resultados = query.ToList();
            return View(filtro);
        }

        [HttpPost]
        public ActionResult GuardarLista(string nombreLista, List<string> cedulasClientes)
        {
            if (string.IsNullOrWhiteSpace(nombreLista) || cedulasClientes == null || !cedulasClientes.Any())
            {
                TempData["Mensaje"] = "Debe ingresar un nombre y seleccionar al menos un cliente.";
                return RedirectToAction("SegmentarClientes");
            }

            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                // Crear lista segmentada
                var lista = new ListaSegmentada
                {
                    Nombre = nombreLista,
                    UsuarioCreador = Session["Usuario"]?.ToString() ?? "Sistema",
                    FechaCreacion = DateTime.Now
                };
                db.ListaSegmentada.Add(lista);
                db.SaveChanges();

                // Asociar clientes a la lista
                foreach (var cedula in cedulasClientes)
                {
                    db.ListaSegmentadaCliente.Add(new ListaSegmentadaCliente
                    {
                        Id_Lista = lista.Id_Lista,
                        Cedula_Cliente = cedula
                    });
                }

                db.SaveChanges();
                TempData["Exito"] = "Lista guardada exitosamente.";
            }

            return RedirectToAction("SegmentarClientes");
        }


        [HttpGet]
        public ActionResult EnviarCampaniaSegmentada()
        {
            var viewModel = new CampaniaSegmentadaViewModel
            {
                ListasDisponibles = db.ListaSegmentada
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id_Lista.ToString(),
                        Text = l.NombreLista
                    }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EnviarCampaniaSegmentada(CampaniaSegmentadaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var listaCedulas = db.ListaSegmentadaCliente
                .Where(x => x.Id_Lista == model.Id_Lista)
                .Select(x => x.Cedula_Cliente)
                .ToList();

            var erroresPersonalizacion = new List<string>();

            foreach (var cedula in listaCedulas)
            {
                var cliente = db.Cliente
                    .Include(c => c.Usuario)
                    .FirstOrDefault(c => c.Cedula == cedula);

                if (cliente == null || cliente.Usuario == null || string.IsNullOrEmpty(cliente.Usuario.Correo))
                    continue;

                string mensaje = model.MensajeHtml;
                mensaje = mensaje.Replace("{{Nombre}}", cliente.Usuario.Nombre ?? "")
                                 .Replace("{{Edad}}", cliente.Edad.HasValue ? cliente.Edad.ToString() : "");

                if (mensaje.Contains("{{"))
                {
                    erroresPersonalizacion.Add(cliente.Usuario.Correo);
                    continue;
                }

                CorreoHelper.EnviarCorreo(cliente.Usuario.Correo, model.Asunto, mensaje);
            }

            if (erroresPersonalizacion.Any())
            {
                ModelState.AddModelError("", $"No se pudo enviar a algunos clientes por errores en la personalización: {string.Join(", ", erroresPersonalizacion)}");

                model.ListasDisponibles = db.ListaSegmentada
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id_Lista.ToString(),
                        Text = l.NombreLista
                    }).ToList();

                return View(model);
            }

            TempData["Exito"] = "Campaña enviada correctamente.";
            return RedirectToAction("Historial");
        }
        public ActionResult ConfigurarRecurrencia()
        {
            var regla = db.ConfiguracionRecurrencia.FirstOrDefault();
            var model = new ReglasRecurrenciaViewModel
            {
                UmbralCompras = regla?.UmbralCompras ?? 3 // Valor por defecto
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult GuardarConfiguracionRecurrencia(ReglasRecurrenciaViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ConfigurarRecurrencia", model);

            var regla = db.ConfiguracionRecurrencia.FirstOrDefault();
            if (regla == null)
            {
                regla = new ConfiguracionRecurrencia
                {
                    UmbralCompras = model.UmbralCompras
                };
                db.ConfiguracionRecurrencia.Add(regla);
            }
            else
            {
                regla.UmbralCompras = model.UmbralCompras;
                db.Entry(regla).State = System.Data.Entity.EntityState.Modified;
            }

            db.SaveChanges();

            TempData["Exito"] = "✅ Reglas guardadas correctamente.";
            return RedirectToAction("ConfigurarRecurrencia");
        }
        public void EnviarPromocionesRecurrentes()
        {
            // Obtener umbral de compras
            var regla = db.ConfiguracionRecurrencia.FirstOrDefault();
            int umbral = regla?.UmbralCompras ?? 3;

            // Obtener clientes que superan el umbral
            var clientesFrecuentes = db.PuntoVenta
                .GroupBy(v => v.Cedula_Cliente)
                .Where(g => g.Count() >= umbral)
                .Select(g => g.Key)
                .ToList();

            if (!clientesFrecuentes.Any())
            {
                // Escenario 4: No hay reglas o clientes calificados
                return;
            }

            // Crear campaña automática
            var campania = new CampaniaMarketing
            {
                Nombre_Campania = "🎁 Promo Cliente Frecuente",
                Descripcion = "¡Gracias por tu preferencia! Aquí tienes un beneficio exclusivo.",
                Tipo = "Automática",
                Fecha_Inicio = DateTime.Today,
                Estado = "A",
                UsuarioCreador = "Sistema",
                FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            db.CampaniaMarketing.Add(campania);
            db.SaveChanges();

            foreach (var cedula in clientesFrecuentes)
            {
                var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula);
                if (cliente == null || cliente.Usuario?.Correo == null)
                    continue;

                db.CampaniaCliente.Add(new CampaniaCliente
                {
                    Id_Campania = campania.Id_Campania,
                    Cedula_Cliente = cedula,
                    Correo_Cliente = cliente.Usuario.Correo,
                    Estado = "A",
                    UsuarioCreador = "Sistema",
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            db.SaveChanges();

            // Escenario 1: Procesar envío automáticamente usando lógica ya existente
            CampaniaHelper.ProcesarCampaniaPorId(campania.Id_Campania);
        }
        [HttpGet]
        public ActionResult ConfigurarCriteriosRecurrentes()
        {
            var criterio = db.CriteriosPromocionRecurrente.FirstOrDefault();

            var model = new HDOpticasJAVS.CriteriosPromocionRecurrente

            {
                MinimoCompras = criterio?.MinimoCompras ?? 3,
                DiasRango = criterio?.DiasRango ?? 60
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfigurarCriteriosRecurrentes(CriteriosPromocionRecurrente model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existente = db.CriteriosPromocionRecurrente.FirstOrDefault();

            if (existente == null)
            {
                var nuevo = new HDOpticasJAVS.CriteriosPromocionRecurrente
                {
                    MinimoCompras = model.MinimoCompras,
                    DiasRango = model.DiasRango,
                    UsuarioCreador = User.Identity.Name,
                    FechaCreacion = DateTime.Now
                };

                db.CriteriosPromocionRecurrente.Add(nuevo);
            }
            else
            {
                existente.MinimoCompras = model.MinimoCompras;
                existente.DiasRango = model.DiasRango;
                existente.UsuarioModificador = User.Identity.Name;
                existente.FechaModificacion = DateTime.Now;
                db.Entry(existente).State = System.Data.Entity.EntityState.Modified;
            }

            db.SaveChanges();
            TempData["Exito"] = "✅ Criterios actualizados correctamente.";
            return RedirectToAction("ConfigurarCriteriosRecurrentes");
        }

        [HttpGet]
        public ActionResult LogErrores()
        {
            var logs = db.LogSistema
                .OrderByDescending(l => l.Fecha)
                .Take(100)
                .ToList();

            return View(logs);
        }

    }
}
