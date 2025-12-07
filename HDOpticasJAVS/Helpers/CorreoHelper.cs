using System;
using System.Net;
using System.Net.Mail;

namespace HDOpticasJAVS.Helpers
{
    public static class CorreoHelper
    {
        public static bool EnviarCorreo(string para, string asunto, string cuerpoHtml)
        {
            try
            {
                var fromAddress = new MailAddress("pruebas@hdopticas.com", "HD Ópticas JAVS");
                var toAddress = new MailAddress(para);

                var smtp = new SmtpClient
                {
                    Host = "sandbox.smtp.mailtrap.io",
                    Port = 587,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        "61eb468a0ab84a",   // Username de Mailtrap
                        "b3a39e0b4e1c3c"   // 
                    )
                };

                string htmlConLogo = $@"
                    <div style='font-family:Arial, sans-serif;'>
                        {cuerpoHtml}
                    </div>";

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = asunto,
                    Body = htmlConLogo,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }

                return true; // ✅ Enviado correctamente a Mailtrap
            }
            catch (Exception ex)
            {
                // 🔒 Guardamos el error en la BD
                try
                {
                    using (var db = new HD_Opticas_JAVS_BDEntities())
                    {
                        db.LogSistema.Add(new LogSistema
                        {
                            Fecha = DateTime.Now,
                            Modulo = "CorreoHelper.EnviarCorreo",
                            Mensaje = "Error Mailtrap: " + ex.ToString(),
                            Usuario = "Sistema"
                        });

                        db.SaveChanges();
                    }
                }
                catch { }

                return false; // ❌ Falló el envío
            }
        }
    }
}
