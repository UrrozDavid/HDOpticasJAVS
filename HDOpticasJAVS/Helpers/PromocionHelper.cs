using System;
using System.Linq;
using HDOpticasJAVS.Models;

namespace HDOpticasJAVS.Helpers
{
    public static class PromocionHelper
    {
        public static void ProcesarPromocionesRecurrentes()
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var criterios = db.CriteriosPromocionRecurrente.FirstOrDefault();

                if (criterios == null)
                {
                    db.LogSistema.Add(new LogSistema
                    {
                        Fecha = DateTime.Now,
                        Modulo = "Promociones Recurrentes",
                        Mensaje = "No se encontraron criterios configurados para promociones automáticas.",
                        Usuario = "Sistema"
                    });
                    db.SaveChanges();
                    return;
                }

                int minimoCompras = criterios.MinimoCompras;
                int diasRango = criterios.DiasRango;
                DateTime fechaLimite = DateTime.Today.AddDays(-diasRango);
                DateTime fechaCorte = DateTime.Today.AddDays(-30); // CORRECCIÓN

                var clientesFrecuentes = db.PuntoVenta
                    .Where(v => v.Fecha_Venta >= fechaLimite)
                    .GroupBy(v => v.Cedula_Cliente)
                    .Where(g => g.Count() >= minimoCompras)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var cedula in clientesFrecuentes)
                {
                    var cliente = (from c in db.Cliente
                                   join u in db.Usuario on c.Cedula equals u.Cedula
                                   where c.Cedula == cedula && u.Correo != null
                                   select new
                                   {
                                       Cliente = c,
                                       Usuario = u
                                   }).FirstOrDefault();

                    if (cliente == null || cliente.Usuario == null || string.IsNullOrWhiteSpace(cliente.Usuario.Correo))
                        continue;

                    // Validar si ya tiene campaña hoy
                    bool yaTieneCampaniaHoy = db.CampaniaMarketing.Any(c =>
                        c.UsuarioCreador == "Sistema" &&
                        c.Fecha_Inicio == DateTime.Today &&
                        db.CampaniaCliente.Any(cc => cc.Id_Campania == c.Id_Campania && cc.Cedula_Cliente == cliente.Cliente.Cedula));

                    if (yaTieneCampaniaHoy)
                        continue;

                    // Verificar si recibió una automática en los últimos 30 días (CORREGIDO)
                    bool yaEnviado = db.CampaniaMetrica.Any(m =>
                        m.Cedula_Cliente == cedula &&
                        m.FechaRegistro >= fechaCorte &&
                        m.Automatica == true);

                    if (yaEnviado)
                        continue;

                    try
                    {
                        var nueva = new CampaniaMarketing
                        {
                            Nombre_Campania = "🎁 Promoción exclusiva para nuestros mejores clientes",
                            Descripcion = "Gracias por tu preferencia. Disfruta un 10% de descuento en tu próxima compra.",
                            Tipo = "Automática",
                            Estado = "A",
                            Fecha_Inicio = DateTime.Today,
                            UsuarioCreador = "Sistema",
                            FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };

                        db.CampaniaMarketing.Add(nueva);
                        db.SaveChanges();

                        db.CampaniaCliente.Add(new CampaniaCliente
                        {
                            Id_Campania = nueva.Id_Campania,
                            Cedula_Cliente = cliente.Cliente.Cedula,
                            Correo_Cliente = cliente.Usuario.Correo,
                            Estado = "A",
                            UsuarioCreador = "Sistema",
                            FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });

                        db.CampaniaMetrica.Add(new CampaniaMetrica
                        {
                            Id_Campania = nueva.Id_Campania,
                            Cedula_Cliente = cliente.Cliente.Cedula,
                            Click = false,
                            Abierto = false,
                            FechaRegistro = DateTime.Now,
                            Automatica = true
                        });

                        db.SaveChanges();

                        string asunto = "🎁 ¡Gracias por ser parte de nuestra familia!";
                        string cuerpo = $@"
                            <h2>Hola {{Nombre}}</h2>
                            <p>Queremos agradecer tu lealtad con una promoción exclusiva.</p>
                            <p><strong>10% de descuento en tu próxima compra</strong>.</p>
                            <p><a href='https://www.hdopticas.com'>Haz clic aquí para redimirla</a></p>";

                        cuerpo = cuerpo.Replace("{{Nombre}}", cliente.Usuario.Nombre ?? "")
                                       .Replace("{{Edad}}", cliente.Cliente.Edad.ToString());

                        if (string.IsNullOrWhiteSpace(cliente.Usuario.Nombre) || cuerpo.Contains("{{"))
                        {
                            db.LogSistema.Add(new LogSistema
                            {
                                Fecha = DateTime.Now,
                                Modulo = "Promociones Recurrentes",
                                Mensaje = $"Correo no enviado a {cliente.Usuario.Correo ?? "sin correo"}. Error en plantilla personalizada.",
                                Usuario = "Sistema"
                            });
                            db.SaveChanges();
                            continue;
                        }

                        CorreoHelper.EnviarCorreo(cliente.Usuario.Correo, asunto, cuerpo);
                    }
                    catch (Exception ex)
                    {
                        db.LogSistema.Add(new LogSistema
                        {
                            Fecha = DateTime.Now,
                            Modulo = "Promociones Recurrentes",
                            Mensaje = $"Error al procesar cliente {cliente.Cliente.Cedula}: {ex.Message}",
                            Usuario = "Sistema"
                        });
                        db.SaveChanges();
                    }
                }
            }
        }
    }
}
