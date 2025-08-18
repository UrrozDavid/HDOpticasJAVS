using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HDOpticasJAVS;
using HDOpticasJAVS.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Net.Mail;

namespace HDOpticasJAVS.Controllers
{
    public class PuntoVentaController : BaseController
    {
        private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

        // GET: PuntoVenta
        public ActionResult Index()
        {
            var puntoVenta = db.PuntoVenta
            .Include(p => p.Cliente)
            .Include(p => p.Parametro)
            .Include(p => p.DetalleVenta.Select(d => d.Inventario));

            return View(puntoVenta.ToList());
        }

        // GET: PuntoVenta/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            // Quita el Include anidado y carga lo demás
            var venta = db.PuntoVenta
                .Include(p => p.Cliente)                                    
                .Include(p => p.Parametro)
                .Include(p => p.DetalleVenta.Select(d => d.Inventario))
                .FirstOrDefault(p => p.Id_Venta == id);

            if (venta == null)
                return HttpNotFound();

            // Cargar explícitamente la navegación del Cliente hacia Usuario
            // (sin asignar a la propiedad)
            try
            {
                db.Entry(venta.Cliente).Reference("Usuario").Load();   // nombre típico
            }
            catch
            {
                try { db.Entry(venta.Cliente).Reference("Usuario1").Load(); } catch { /* ignore */ }
            }

            return View(venta);
        }



        // GET: PuntoVenta/Create
        public ActionResult Create()
        {
            ViewBag.Cedula_Cliente = new SelectList(
     (from c in db.Cliente
      join u in db.Usuario on c.Cedula equals u.Cedula
      select new
      {
          Cedula = c.Cedula,
          NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2
      }).ToList(),
     "Cedula", "NombreCompleto"
 );

            ViewBag.Id_MetodoPago = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 3),
                "Id_Parametro", "Nombre_Parametro"
            );
            ViewBag.Productos = db.Inventario.Where(i => i.Estado == "A").ToList();

            return View(new PuntoVenta { Fecha_Venta = DateTime.Now, Hora_Venta = DateTime.Now.TimeOfDay });
        }

        // POST: PuntoVenta/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PuntoVenta puntoVenta, List<DetalleVenta> detalles)
        {
            if (detalles == null || !detalles.Any(d => d.Cantidad > 0))
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto con cantidad válida.");
            }

            if (ModelState.IsValid)
            {
                decimal subtotal = (decimal)detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
                decimal iva = subtotal * 0.13m;
                decimal total = subtotal + iva;

                puntoVenta.Subtotal = subtotal;
                puntoVenta.IVA = iva;
                puntoVenta.Total = total;
                puntoVenta.Estado = "A";
                puntoVenta.UsuarioModificador = User.Identity.Name ?? "Administrador";
                puntoVenta.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                db.PuntoVenta.Add(puntoVenta);
                db.SaveChanges(); // Para obtener el Id_Venta

                foreach (var d in detalles.Where(d => d.Cantidad > 0))
                {
                    d.Id_Venta = puntoVenta.Id_Venta;
                    d.Subtotal = d.Cantidad * d.PrecioUnitario;
                    db.DetalleVenta.Add(d);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Venta registrada correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.Cedula_Cliente = new SelectList(db.Cliente.Include(c => c.Usuario),
                "Cedula", "Usuario.Nombre", puntoVenta.Cedula_Cliente);
            ViewBag.Id_MetodoPago = new SelectList(db.Parametro,
                "Id_Parametro", "Nombre_Parametro", puntoVenta.Id_MetodoPago);
            ViewBag.Productos = db.Inventario.Where(i => i.Estado == "A").ToList();

            TempData["ErrorMessage"] = "Ocurrió un error al registrar la venta.";
            return View(puntoVenta);
        }

        // GET: PuntoVenta/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var puntoVenta = db.PuntoVenta
                .Include(p => p.DetalleVenta.Select(d => d.Inventario))
                .Include(p => p.Cliente)
                .Include(p => p.Parametro)
                .FirstOrDefault(p => p.Id_Venta == id);

            if (puntoVenta == null)
                return HttpNotFound();

            ViewBag.Cedula_Cliente = new SelectList(
                db.Cliente.Include(c => c.Usuario).ToList()
                    .Select(c => new
                    {
                        Cedula = c.Cedula,
                        NombreCompleto = c.Usuario.Nombre + " " + c.Usuario.Apellido1 + " " + c.Usuario.Apellido2
                    }),
                "Cedula", "NombreCompleto", puntoVenta?.Cedula_Cliente
            );
            ViewBag.Id_MetodoPago = new SelectList(
                db.Parametro.Where(p => p.Id_TipoParametro == 3),
                "Id_Parametro", "Nombre_Parametro", puntoVenta?.Id_MetodoPago
            );
            ViewBag.Productos = db.Inventario.Where(p => p.Estado == "A").ToList();

            return View(puntoVenta);
        }

        // POST: PuntoVenta/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PuntoVenta puntoVenta, List<DetalleVenta> detalles)
        {
            if (detalles == null || !detalles.Any(d => d.Cantidad > 0))
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto con cantidad válida.");
            }

            if (ModelState.IsValid)
            {
                // Recalcular totales
                decimal subtotal = (decimal)detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
                decimal iva = subtotal * 0.13m;
                decimal total = subtotal + iva;

                // Buscar venta existente
                var ventaExistente = db.PuntoVenta.FirstOrDefault(p => p.Id_Venta == puntoVenta.Id_Venta);
                if (ventaExistente == null)
                {
                    return HttpNotFound();
                }

                // Actualizar campos editables
                ventaExistente.Cedula_Cliente = puntoVenta.Cedula_Cliente;
                ventaExistente.Id_MetodoPago = puntoVenta.Id_MetodoPago;
                ventaExistente.Fecha_Venta = puntoVenta.Fecha_Venta;
                ventaExistente.Hora_Venta = puntoVenta.Hora_Venta;
                ventaExistente.Estado = puntoVenta.Estado;

                ventaExistente.Subtotal = subtotal;
                ventaExistente.IVA = iva;
                ventaExistente.Total = total;

                ventaExistente.UsuarioModificador = User.Identity.Name ?? "Administrador";
                ventaExistente.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Reemplazar detalles
                var existentes = db.DetalleVenta.Where(d => d.Id_Venta == ventaExistente.Id_Venta).ToList();
                foreach (var d in existentes) db.DetalleVenta.Remove(d);

                foreach (var d in detalles.Where(d => d.Cantidad > 0))
                {
                    d.Id_Venta = ventaExistente.Id_Venta;
                    d.Subtotal = d.Cantidad * d.PrecioUnitario;
                    db.DetalleVenta.Add(d);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Venta actualizada correctamente.";
                return RedirectToAction("Index");
            }

            // Si falla validación, recargar ViewBag y volver a vista
            ViewBag.Cedula_Cliente = new SelectList(db.Cliente.Include(c => c.Usuario),
                "Cedula", "Usuario.Nombre", puntoVenta.Cedula_Cliente);
            ViewBag.Id_MetodoPago = new SelectList(db.Parametro,
                "Id_Parametro", "Nombre_Parametro", puntoVenta.Id_MetodoPago);
            ViewBag.Productos = db.Inventario.Where(p => p.Estado == "A").ToList();

            TempData["ErrorMessage"] = "Ocurrió un error al guardar la venta.";
            return View(puntoVenta);
        }

        // GET: PuntoVenta/Delete/5
        public ActionResult Delete(int? id)
        {
            return RedirectToAction("Index");
        }

        // POST: PuntoVenta/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var venta = db.PuntoVenta.FirstOrDefault(p => p.Id_Venta == id && p.Estado == "A");
            if (venta == null)
            {
                TempData["ErrorMessage"] = "No se encontró la venta o ya estaba inactiva.";
                return RedirectToAction("Index");
            }

            venta.Estado = "I";
            venta.UsuarioModificador = User.Identity.Name ?? "Administrador";
            venta.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            db.Entry(venta).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Venta eliminada correctamente.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        //PUNTO DE VENTA DEL CAJERO

        public ActionResult IndexCajero()
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var model = new VentaViewModel
                {
                    ProductosDisponibles = db.Inventario
                        .Where(p => p.Estado == "A")
                        .Select(p => new ItemProducto
                        {
                            Id_Producto = p.Id_Producto,
                            Nombre = p.Nombre_Producto,
                            Precio = (decimal)p.Precio
                        }).ToList()
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IndexCajero(VentaViewModel model)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                // Recalcular total desde carrito
                decimal subtotal = model.Carrito
                    .Where(p => p.Cantidad > 0)
                    .Sum(p => p.Cantidad * p.Precio);

                decimal iva = subtotal * 0.13m;
                decimal total = subtotal + iva;

                model.TotalCompra = subtotal;

                // Validaciones
                if (total != (model.Efectivo + model.MontoTarjeta))
                {
                    ModelState.AddModelError("", "El total no coincide con la suma de métodos de pago.");
                }

                if (model.MontoTarjeta > 0)
                {
                    var tarjeta = db.TarjetaSimulada.FirstOrDefault(t =>
                        t.NumeroTarjeta == model.NumeroTarjeta &&
                        t.NombreTitular == model.NombreTitular &&
                        t.FechaVencimiento == model.FechaVencimiento &&
                        t.CVV == model.CVV
                    );

                    if (tarjeta == null || tarjeta.Saldo < model.MontoTarjeta)
                    {
                        ModelState.AddModelError("", "Tarjeta inválida o saldo insuficiente.");
                    }
                    else
                    {
                        tarjeta.Saldo -= model.MontoTarjeta;
                    }
                }
                /*var cliente = db.Cliente.Include("Usuario").FirstOrDefault(c => c.Cedula == model.CedulaCliente);*/
                var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == model.CedulaCliente);
                if (cliente == null)
                {
                    ModelState.AddModelError("CedulaCliente", "La cédula ingresada no está registrada como cliente.");
                }
                else if (string.IsNullOrWhiteSpace(cliente.Usuario?.Correo))
                {
                    ModelState.AddModelError("CedulaCliente", "El cliente no tiene un correo asociado.");
                }

                // Validar que haya productos
                if (model.Carrito == null || !model.Carrito.Any(p => p.Cantidad > 0))
                {
                    ModelState.AddModelError("", "Debe seleccionar al menos un producto con cantidad mayor a cero.");
                }

                // Validar método de pago
                if (model.Efectivo <= 0 && model.MontoTarjeta <= 0)
                {
                    ModelState.AddModelError("", "Debe ingresar al menos un método de pago.");
                }

                if (!ModelState.IsValid)
                {
                    model.ProductosDisponibles = db.Inventario
                        .Where(p => p.Estado == "A")
                        .Select(p => new ItemProducto
                        {
                            Id_Producto = p.Id_Producto,
                            Nombre = p.Nombre_Producto,
                            Precio = (decimal)p.Precio
                        }).ToList();

                    return View(model);
                }

                // Determinar método de pago
                int metodoPago;
                if (model.Efectivo > 0 && model.MontoTarjeta > 0)
                    metodoPago = 19; // Múltiple
                else if (model.Efectivo > 0)
                    metodoPago = 6; // Efectivo
                else
                    metodoPago = 7; // Tarjeta

                // Crear venta
                var venta = new PuntoVenta
                {
                    Cedula_Cliente = model.CedulaCliente,
                    Subtotal = subtotal,
                    IVA = iva,
                    Total = total,
                    Id_MetodoPago = metodoPago,
                    Fecha_Venta = DateTime.Today,
                    Hora_Venta = DateTime.Now.TimeOfDay,
                    Estado = "A",
                    UsuarioCreador = User.Identity.Name ?? "Cajero",
                    FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.PuntoVenta.Add(venta);
                db.SaveChanges();

                // Agregar productos seleccionados como detalles
                foreach (var item in model.Carrito.Where(p => p.Cantidad > 0))
                {
                    db.DetalleVenta.Add(new DetalleVenta
                    {
                        Id_Venta = venta.Id_Venta,
                        Id_Producto = item.Id_Producto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio,
                        Subtotal = item.Cantidad * item.Precio
                    });
                }

                // Guardar métodos de pago
                if (model.Efectivo > 0)
                {
                    db.PagoVenta.Add(new PagoVenta
                    {
                        Id_Venta = venta.Id_Venta,
                        Metodo = "Efectivo",
                        Monto = model.Efectivo
                    });
                }

                if (model.MontoTarjeta > 0)
                {
                    db.PagoVenta.Add(new PagoVenta
                    {
                        Id_Venta = venta.Id_Venta,
                        Metodo = "Tarjeta",
                        Monto = model.MontoTarjeta
                    });
                }

                db.SaveChanges();
                TempData["MensajeExito"] = "¡Venta realizada exitosamente!";

                // Obtener detalles de la venta y datos del usuario
                var detallesVenta = db.DetalleVenta
                    .Include("Inventario")
                    .Where(d => d.Id_Venta == venta.Id_Venta)
                    .ToList();

                //MODIFICANDO
                var usuarioCliente = db.Usuario.FirstOrDefault(u => u.Cedula == cliente.Cedula);

                // Generar PDF
                var pdfBytes = GenerarFacturaPDF(venta, detallesVenta, usuarioCliente);

                // Enviar correo
                EnviarFacturaPorCorreo(usuarioCliente.Correo, pdfBytes);

                return RedirectToAction("IndexCajero");
            }
        }

        private byte[] GenerarFacturaPDF(PuntoVenta venta, List<DetalleVenta> detalles, Usuario usuario)
        {
            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var normal = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                doc.Add(new Paragraph("COMPROBANTE DE VENTA", titulo));
                doc.Add(new Paragraph($"Cliente: {usuario.Nombre} {usuario.Apellido1} {usuario.Apellido2}", normal));
                doc.Add(new Paragraph($"Fecha: {venta.Fecha_Venta:yyyy-MM-dd}    Hora: {venta.Hora_Venta}", normal));
                doc.Add(new Paragraph(" "));

                PdfPTable tabla = new PdfPTable(4);
                tabla.WidthPercentage = 100;
                tabla.SetWidths(new float[] { 40f, 20f, 20f, 20f });

                tabla.AddCell("Producto");
                tabla.AddCell("Cantidad");
                tabla.AddCell("Precio Unitario");
                tabla.AddCell("Subtotal");

                foreach (var detalle in detalles)
                {
                    tabla.AddCell(detalle.Inventario.Nombre_Producto);
                    tabla.AddCell(detalle.Cantidad.ToString());
                    tabla.AddCell($"₡{detalle.PrecioUnitario:N2}");
                    tabla.AddCell($"₡{detalle.Subtotal:N2}");
                }

                doc.Add(tabla);
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph($"Subtotal: ₡{venta.Subtotal:N2}", normal));
                doc.Add(new Paragraph($"IVA (13%): ₡{venta.IVA:N2}", normal));
                doc.Add(new Paragraph($"Total: ₡{venta.Total:N2}", normal));

                doc.Close();
                return ms.ToArray();
            }
        }

        private void EnviarFacturaPorCorreo(string correoDestino, byte[] pdfBytes)
        {
            MailMessage mensaje = new MailMessage();
            mensaje.To.Add(correoDestino);
            mensaje.Subject = "Factura de su compra - Ópticas JAVS";
            mensaje.Body = "Adjuntamos el comprobante de su compra. ¡Gracias por confiar en nosotros!";
            mensaje.From = new MailAddress("hdopticasjavs@gmail.com");

            mensaje.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), "Factura.pdf"));

            SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential("hdopticasjavs@gmail.com", "ysuk wivj qivo dacj")
            };

            smtp.Send(mensaje);
        }

    }
}
