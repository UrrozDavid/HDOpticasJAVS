using System;
using System.Net;
using System.Net.Mail;

namespace HDOpticasJAVS.Helpers
{
    public static class CorreoHelper
    {
        public static void EnviarCorreo(string para, string asunto, string cuerpoHtml)
        {
            try
            {
                var fromAddress = new MailAddress("hdopticasjavs@gmail.com", "HD Ópticas JAVS");
                var toAddress = new MailAddress(para);
                const string fromPassword = "ysuk wivj qivo dacj"; 

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                string htmlConLogo = $@"
                <div style='text-align:center; font-family:Arial, sans-serif;'>
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
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\\temp\\errores_envio.txt", ex.ToString());
            }
        }
    }
}
