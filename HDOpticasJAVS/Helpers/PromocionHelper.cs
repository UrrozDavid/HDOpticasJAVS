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
                // 🔍 Obtener criterios configurados
                var criterios = db.CriteriosPromocionRecurrente.FirstOrDefault();

                if (criterios == null)
                {
                    // ⛔ No se han configurado criterios, registrar el error
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

                var clientesFrecuentes = db.PuntoVenta
                    .Where(v => v.Fecha_Venta >= fechaLimite)
                    .GroupBy(v => v.Cedula_Cliente)
                    .Where(g => g.Count() >= minimoCompras)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var cedula in clientesFrecuentes)
                {
                    var cliente = db.Cliente.FirstOrDefault(c => c.Cedula == cedula && c.Usuario.Correo != null);
                    if (cliente == null)
                        continue;

                    // Verifica si ya se envió recientemente
                    bool yaEnviado = db.CampaniaMetrica.Any(m =>
                        m.Cedula_Cliente == cedula &&
                        m.FechaRegistro >= DateTime.Today.AddDays(-30) &&
                        m.Automatica == true);

                    if (yaEnviado)
                        continue;

                    // Crear campaña automática
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

                    // Relacionar cliente con la campaña
                    db.CampaniaCliente.Add(new CampaniaCliente
                    {
                        Id_Campania = nueva.Id_Campania,
                        Cedula_Cliente = cliente.Cedula,
                        Correo_Cliente = cliente.Usuario.Correo,
                        Estado = "A",
                        UsuarioCreador = "Sistema",
                        FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });

                    // Registrar métrica
                    db.CampaniaMetrica.Add(new CampaniaMetrica
                    {
                        Id_Campania = nueva.Id_Campania,
                        Cedula_Cliente = cliente.Cedula,
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

                    // 🔄 Reemplazo dinámico
                    cuerpo = cuerpo.Replace("{{Nombre}}", cliente.Usuario.Nombre ?? "")
                                   .Replace("{{Edad}}", cliente.Edad.ToString());

                    // 🛑 Validación: Si quedó alguna variable sin reemplazar
                    if (cuerpo.Contains("{{") || string.IsNullOrWhiteSpace(asunto))
                    {
                        db.LogSistema.Add(new LogSistema
                        {
                            Fecha = DateTime.Now,
                            Modulo = "Promociones Recurrentes",
                            Mensaje = $"Correo no enviado a {cliente.Usuario.Correo}. Error en plantilla de personalización.",
                            Usuario = "Sistema"
                        });
                        db.SaveChanges();
                        continue; // ❌ No se envía este correo
                    }

                    // ✅ Enviar correo si está bien
                    CorreoHelper.EnviarCorreo(cliente.Usuario.Correo, asunto, cuerpo);

                }
            }
        }
    }
}