using HDOpticasJAVS.Helpers;
using HDOpticasJAVS.Models;
using HDOpticasJAVS.Models.ViewModels;
using HDOpticasJAVS.ViewModels;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web.Mvc;


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

            var clientesConNombres = db.Cliente
                // usa el campo que tengas de estado/activo; dejo ambas opciones para que no pierdas a nadie
                .Where(c => c.Activo || c.Estado == "A" || c.Estado == null)
                .Select(c => new ClienteSeleccionado
                {
                    Cedula = c.Cedula,
                    NombreCompleto = ((c.Nombre ?? "") + " " + (c.Apellido1 ?? "")).Trim(),
                    Correo = (c.Correo != null && c.Correo != "") ? c.Correo : null,
                    Seleccionado = false
                })
                .OrderBy(x => x.NombreCompleto)
                .ToList();

            var model = new CampaniaMarketingViewModel
            {
                Fecha_Inicio = DateTime.Today,
                ClientesSeleccionados = clientesConNombres
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
                Fecha_Fin = model.Fecha_Fin,
                Estado = model.Fecha_Programada.HasValue ? "P" : "A",
                UsuarioCreador = User.Identity.Name,
                FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            db.CampaniaMarketing.Add(campania);
            db.SaveChanges();

            // >>> ÚNICO CAMBIO: solo con correo y sin duplicados por cédula
            var todos = model.ClientesSeleccionados ?? new List<ClienteSeleccionado>();
            var seleccionadosValidos = todos
                .Where(c => c.Seleccionado && !string.IsNullOrWhiteSpace(c.Correo))
                .GroupBy(c => c.Cedula)           // evita duplicados
                .Select(g => g.First())
                .ToList();

            var omitidosSinCorreo = todos.Count(c => c.Seleccionado && string.IsNullOrWhiteSpace(c.Correo));

            foreach (var cliente in seleccionadosValidos)
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

            TempData["Mensaje"] = omitidosSinCorreo > 0
                ? $"✅ Campaña creada. {omitidosSinCorreo} cliente(s) seleccionados no tenían correo y fueron omitidos."
                : "✅ Campaña creada correctamente.";
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
                campania.Fecha_Fin = model.Fecha_Fin;


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
            var datos = (
                from m in db.CampaniaMetrica.AsNoTracking()
                where m.Id_Campania == id
                join c in db.Cliente on m.Cedula_Cliente equals c.Cedula into jc
                from c in jc.DefaultIfEmpty()
                join u in db.Usuario on c.Cedula equals u.Cedula into ju
                from u in ju.DefaultIfEmpty()
                select new
                {
                    m.Cedula_Cliente,
                    Nombre = u.Nombre,
                    Apellido1 = u.Apellido1,
                    m.Abierto,
                    m.Click,
                    m.FechaRegistro
                }
            ).ToList();

            var data = datos.Select(x => new
            {
                Cedula = x.Cedula_Cliente,
                Cliente = ((x.Nombre ?? "") + " " + (x.Apellido1 ?? "")).Trim(),
                Abierto = x.Abierto == true ? "Sí" : "No",
                Click = x.Click == true ? "Sí" : "No",
                FechaRegistro = x.FechaRegistro?.ToString("yyyy-MM-dd HH:mm") ?? ""
            }).ToList();

            var grid = new System.Web.UI.WebControls.GridView();
            grid.DataSource = data;
            grid.DataBind();

            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment; filename=ReporteCampania_" + id + ".xls");
            Response.ContentType = "application/excel";
            using (var sw = new System.IO.StringWriter())
            using (var htw = new System.Web.UI.HtmlTextWriter(sw))
            {
                grid.RenderControl(htw);
                Response.Write(sw.ToString());
            }
            Response.End();
            return new EmptyResult();
        }
        public ActionResult ExportarPdf(int id)
        {
            var campania = db.CampaniaMarketing.Find(id);

            var datos = (
                from m in db.CampaniaMetrica.AsNoTracking()
                where m.Id_Campania == id
                join c in db.Cliente on m.Cedula_Cliente equals c.Cedula into jc
                from c in jc.DefaultIfEmpty()
                join u in db.Usuario on c.Cedula equals u.Cedula into ju
                from u in ju.DefaultIfEmpty()
                select new
                {
                    m.Cedula_Cliente,
                    Nombre = u.Nombre,
                    Apellido1 = u.Apellido1,
                    m.Abierto,
                    m.Click,
                    m.FechaRegistro
                }
            ).ToList();

            var data = datos.Select(x => new
            {
                Cedula = x.Cedula_Cliente,
                Cliente = ((x.Nombre ?? "") + " " + (x.Apellido1 ?? "")).Trim(),
                Abierto = x.Abierto == true ? "Sí" : "No",
                Click = x.Click == true ? "Sí" : "No",
                FechaRegistro = x.FechaRegistro?.ToString("yyyy-MM-dd HH:mm") ?? ""
            }).ToList();

            using (var ms = new System.IO.MemoryStream())
            {
                var doc = new iTextSharp.text.Document();
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD);
                var tableFont = iTextSharp.text.FontFactory.GetFont("Arial", 10);

                doc.Add(new iTextSharp.text.Paragraph("Reporte de campaña", titleFont));
                doc.Add(new iTextSharp.text.Paragraph("Nombre: " + campania?.Nombre_Campania));
                doc.Add(new iTextSharp.text.Paragraph("Descripción: " + campania?.Descripcion));
                doc.Add(new iTextSharp.text.Paragraph(" "));

                var table = new iTextSharp.text.pdf.PdfPTable(5) { WidthPercentage = 100 };
                table.AddCell("Cédula");
                table.AddCell("Cliente");
                table.AddCell("Abierto");
                table.AddCell("Click");
                table.AddCell("Fecha Registro");

                foreach (var item in data)
                {
                    table.AddCell(item.Cedula);
                    table.AddCell(item.Cliente);
                    table.AddCell(item.Abierto);
                    table.AddCell(item.Click);
                    table.AddCell(item.FechaRegistro);
                }

                doc.Add(table);
                doc.Close();

                return File(ms.ToArray(), "application/pdf", "ReporteCampania_" + id + ".pdf");
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
        [ValidateAntiForgeryToken]
        public ActionResult EnviarCampaniaManual(int id)
        {
            try
            {
                var campania = db.CampaniaMarketing.Find(id);
                if (campania == null) return HttpNotFound();

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
        public ActionResult AplicarPromocionCliente(int idCampania, string cedulaCliente)
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


            var q = from c in db.Cliente
                    join u in db.Usuario on c.Cedula equals u.Cedula into gj
                    from u in gj.DefaultIfEmpty()
                    where (c.Activo || c.Estado == "A" || c.Estado == null)
                    select new { c, u };


            if (!string.IsNullOrEmpty(filtro.Nombre))
            {
                string nombre = filtro.Nombre;
                q = q.Where(x =>
                       (x.u != null && (
                            (x.u.Nombre ?? "").Contains(nombre) ||
                            (x.u.Apellido1 ?? "").Contains(nombre) ||
                            (x.u.Apellido2 ?? "").Contains(nombre)))
                    || ((x.c.Nombre ?? "").Contains(nombre) ||
                        (x.c.Apellido1 ?? "").Contains(nombre) ||
                        (x.c.Apellido2 ?? "").Contains(nombre) ||
                        (x.c.Cedula ?? "").Contains(nombre)));
            }

            if (filtro.EdadMinima.HasValue)
                q = q.Where(x => x.c.Edad >= filtro.EdadMinima.Value);

            if (filtro.EdadMaxima.HasValue)
                q = q.Where(x => x.c.Edad <= filtro.EdadMaxima.Value);


            if (!string.IsNullOrEmpty(filtro.Tratamiento))
            {
                var patron = "%" + filtro.Tratamiento + "%";
                q = q.Where(x => x.c.Padecimiento != null &&
                                 DbFunctions.Like(x.c.Padecimiento, patron));
            }


            q = q.Where(x =>

                (x.u != null &&
                 x.u.Correo != null &&
                 SqlFunctions.DataLength(x.u.Correo) > 0 &&
                 !DbFunctions.Like(x.u.Correo, "%@example.com") &&
                 !DbFunctions.Like(x.u.Correo, "%@dominio.com"))
                ||

                ((x.u == null || x.u.Correo == null || SqlFunctions.DataLength(x.u.Correo) == 0) &&
                 x.c.Correo != null &&
                 SqlFunctions.DataLength(x.c.Correo) > 0 &&
                 !DbFunctions.Like(x.c.Correo, "%@example.com") &&
                 !DbFunctions.Like(x.c.Correo, "%@dominio.com"))
            );

            var pares = q.ToList();


            var lista = pares
                .OrderBy(p =>
                {
                    var nombreEf = ((p.u?.Nombre ?? p.c.Nombre) + " " + (p.u?.Apellido1 ?? p.c.Apellido1)).Trim();
                    return string.IsNullOrWhiteSpace(nombreEf) ? p.c.Cedula : nombreEf;
                })
                .Select(p => p.c)
                .ToList();

            filtro.Resultados = lista;
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
        public ActionResult Tendencias(DateTime? desde = null, DateTime? hasta = null)
        {
            var fechaInicio = desde ?? DateTime.MinValue;
            var fechaFin = hasta ?? DateTime.MaxValue;

            var campañas = db.CampaniaMarketing
                .Where(c => c.Fecha_Inicio >= fechaInicio && c.Fecha_Inicio <= fechaFin)
                .ToList();

            var metricas = db.CampaniaMetrica
                .Where(m => m.FechaRegistro >= fechaInicio && m.FechaRegistro <= fechaFin)
                .ToList();

            var lista = campañas.Select(c => new CampaniaTendenciaViewModel
            {
                NombreCampania = c.Nombre_Campania,
                FechaInicio = c.Fecha_Inicio ?? DateTime.MinValue,
                FechaFin = c.Fecha_Fin,
                TotalEnviados = db.CampaniaCliente.Count(x => x.Id_Campania == c.Id_Campania),
                TotalAbiertos = metricas.Count(x => x.Id_Campania == c.Id_Campania && x.Abierto == true),
                TotalClicks = metricas.Count(x => x.Id_Campania == c.Id_Campania && x.Click == true)
            }).ToList();

            foreach (var item in lista)
            {
                item.PorcentajeApertura = item.TotalEnviados > 0 ? Math.Round((double)item.TotalAbiertos / item.TotalEnviados * 100, 2) : 0;
                item.PorcentajeClick = item.TotalEnviados > 0 ? Math.Round((double)item.TotalClicks / item.TotalEnviados * 100, 2) : 0;
            }

            if (!lista.Any())
            {
                ViewBag.Mensaje = "No hay datos para mostrar en el periodo seleccionado.";
            }

            return View(lista);
        }

        [HttpPost]
        public ActionResult ExportarTendencias(DateTime? desde, DateTime? hasta)
        {
            var fechaInicio = desde ?? DateTime.MinValue;
            var fechaFin = hasta ?? DateTime.MaxValue;

            var campañas = db.CampaniaMarketing
                .Where(c => c.Fecha_Inicio >= fechaInicio && c.Fecha_Inicio <= fechaFin)
                .ToList();

            var metricas = db.CampaniaMetrica
                .Where(m => m.FechaRegistro >= fechaInicio && m.FechaRegistro <= fechaFin)
                .ToList();

            var lista = campañas.Select(c => new CampaniaTendenciaViewModel
            {
                NombreCampania = c.Nombre_Campania,
                FechaInicio = c.Fecha_Inicio ?? DateTime.MinValue,
                FechaFin = c.Fecha_Fin ?? DateTime.MinValue,
                TotalEnviados = db.CampaniaCliente.Count(x => x.Id_Campania == c.Id_Campania),
                TotalAbiertos = metricas.Count(x => x.Id_Campania == c.Id_Campania && x.Abierto == true),
                TotalClicks = metricas.Count(x => x.Id_Campania == c.Id_Campania && x.Click == true)
            }).ToList();

            foreach (var item in lista)
            {
                item.PorcentajeApertura = item.TotalEnviados > 0 ? Math.Round((double)item.TotalAbiertos / item.TotalEnviados * 100, 2) : 0;
                item.PorcentajeClick = item.TotalEnviados > 0 ? Math.Round((double)item.TotalClicks / item.TotalEnviados * 100, 2) : 0;
            }

            if (!lista.Any())
            {
                return Content("No hay datos para exportar.");
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Campaña,Enviados,Abiertos,Clicks,% Apertura,% Click,Desde,Hasta");

            foreach (var item in lista)
            {
                sb.AppendLine($"{item.NombreCampania},{item.TotalEnviados},{item.TotalAbiertos},{item.TotalClicks}," +
                              $"{item.PorcentajeApertura:F2},{item.PorcentajeClick:F2},{item.FechaInicio:yyyy-MM-dd},{item.FechaFin:yyyy-MM-dd}");
            }

            return File(
                new System.Text.UTF8Encoding().GetBytes(sb.ToString()),
                "text/csv",
                "TendenciasMarketing.csv"
            );
        }
        [HttpGet]
        public ActionResult AplicarPromocion(int? idVenta, [Bind(Prefix = "id")] int? id)
        {
            var ventaId = idVenta ?? id;
            if (!ventaId.HasValue)
            {
                TempData["Error"] = "No se recibió la venta a aplicar.";
                return RedirectToAction("Index", "PuntoVenta");
            }

            var venta = db.PuntoVenta.AsNoTracking().FirstOrDefault(v => v.Id_Venta == ventaId.Value);
            if (venta == null)
            {
                TempData["Error"] = "La venta no existe.";
                return RedirectToAction("Index", "PuntoVenta");
            }

            var descAcum = db.Set<VentaPromocion>()
                             .Where(x => x.Id_Venta == ventaId.Value)
                             .Select(x => (decimal?)x.MontoDescuento)
                             .Sum() ?? 0m;

            ViewBag.Campanias = new SelectList(db.CampaniaMarketing.AsNoTracking().ToList(), "Id_Campania", "Nombre_Campania");
            ViewBag.PromosAplicadas = (from vp in db.Set<VentaPromocion>().AsNoTracking()
                                       join cm in db.CampaniaMarketing.AsNoTracking() on vp.Id_Campania equals cm.Id_Campania
                                       where vp.Id_Venta == ventaId.Value
                                       orderby vp.FechaAplicacion descending
                                       select new { vp.Id_VentaPromocion, NombreCampania = cm.Nombre_Campania, vp.MontoDescuento, vp.CodigoPromo, vp.FechaAplicacion }
                                      ).ToList();

            ViewBag.IdVenta = ventaId.Value;
            ViewBag.Subtotal = venta.Subtotal ?? 0m;
            ViewBag.IVA = venta.IVA ?? 0m;
            ViewBag.TotalBruto = venta.Total ?? 0m;
            ViewBag.DescAcum = descAcum;
            ViewBag.TotalNeto = (venta.Total ?? 0m) - descAcum;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AplicarPromocion([Bind(Prefix = "id")] int? idVenta, int idCampania, decimal montoDescuento, string codigoPromo)
        {
            if (!idVenta.HasValue)
            {
                TempData["Error"] = "No se recibió la venta a aplicar.";
                return RedirectToAction("Index", "PuntoVenta");
            }

            // Normaliza el número por si viene con coma/punto
            var raw = Request.Form["montoDescuento"];
            if (!string.IsNullOrWhiteSpace(raw))
            {
                decimal parsed;
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.CurrentCulture, out parsed))
                    montoDescuento = parsed;
                else if (decimal.TryParse(raw.Replace(',', '.'),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out parsed))
                    montoDescuento = parsed;
            }
            montoDescuento = Math.Abs(montoDescuento);

            var venta = db.PuntoVenta.FirstOrDefault(v => v.Id_Venta == idVenta.Value);
            if (venta == null)
            {
                TempData["Error"] = "No se encontró la venta.";
                return RedirectToAction("Index", "PuntoVenta");
            }

            var totalBruto = venta.Total ?? 0m;
            var descAcum = db.Set<VentaPromocion>()
                             .Where(x => x.Id_Venta == idVenta.Value)
                             .Select(x => (decimal?)x.MontoDescuento)
                             .Sum() ?? 0m;

            var disponible = totalBruto - descAcum;
            if (montoDescuento <= 0m)
            {
                TempData["Error"] = "El monto de descuento debe ser mayor a 0.";
                return RedirectToAction("AplicarPromocion", new { idVenta = idVenta.Value });
            }
            if (montoDescuento > disponible)
            {
                TempData["Error"] = $"El descuento ({montoDescuento:C}) supera el disponible ({disponible:C}).";
                return RedirectToAction("AplicarPromocion", new { idVenta = idVenta.Value });
            }

            var promo = new VentaPromocion
            {
                Id_Venta = idVenta.Value,
                Id_Campania = idCampania,
                MontoDescuento = montoDescuento,
                CodigoPromo = string.IsNullOrWhiteSpace(codigoPromo) ? null : codigoPromo,
                FechaAplicacion = DateTime.Now,
                UsuarioAplicacion = (Session["Usuario"] ?? Session["Cedula"] ?? "Sistema").ToString()
            };
            db.Set<VentaPromocion>().Add(promo);
            db.SaveChanges();

            TempData["Mensaje"] = "✅ Promoción aplicada.";
            return RedirectToAction("AplicarPromocion", new { idVenta = idVenta.Value });
        }


        public PartialViewResult HistorialPromociones(string cedula)
        {
            // 1) Si no viene la cédula, intenta sacarla de la sesión
            if (string.IsNullOrWhiteSpace(cedula))
            {
                string usuarioSesion = (Session["Usuario"] ?? Session["Cedula"])?.ToString();
                var usuario = db.Usuario.FirstOrDefault(u => u.Correo == usuarioSesion || u.Cedula == usuarioSesion);
                cedula = usuario?.Cedula;
            }

            // Si aún no hay cédula, devuelve lista vacía para no romper la vista
            if (string.IsNullOrWhiteSpace(cedula))
                return PartialView("_HistorialPromociones", new List<PromocionHistorialItemViewModel>());

            // 2) Promociones aplicadas en ventas (usa Set<VentaPromocion>() en vez de db.VentaPromocion)
            var porVenta =
                from vp in db.Set<VentaPromocion>()
                join pv in db.PuntoVenta on vp.Id_Venta equals pv.Id_Venta
                join cm in db.CampaniaMarketing on vp.Id_Campania equals cm.Id_Campania
                where pv.Cedula_Cliente == cedula
                select new PromocionHistorialItemViewModel
                {
                    Fecha = vp.FechaAplicacion,
                    Tipo = "Aplicada en venta",
                    Campania = cm.Nombre_Campania,
                    CodigoPromo = vp.CodigoPromo,
                    MontoDescuento = vp.MontoDescuento,
                    IdVenta = vp.Id_Venta,
                    TotalVenta = pv.Total
                };

            // 3) Interacciones de marketing (aperturas / clicks)
            var interacciones =
                from m in db.CampaniaMetrica
                join cm in db.CampaniaMarketing on m.Id_Campania equals cm.Id_Campania
                where m.Cedula_Cliente == cedula
                select new PromocionHistorialItemViewModel
                {
                    Fecha = m.FechaRegistro,
                    Tipo = (m.Click == true) ? "Click" : (m.Abierto == true ? "Apertura" : "Registro"),
                    Campania = cm.Nombre_Campania,
                    CodigoPromo = null,
                    MontoDescuento = null,
                    IdVenta = null,
                    TotalVenta = null
                };

            var data = porVenta
                .Concat(interacciones)
                .OrderByDescending(x => x.Fecha)
                .ToList();

            return PartialView("_HistorialPromociones", data);
        }
    }
}

    


