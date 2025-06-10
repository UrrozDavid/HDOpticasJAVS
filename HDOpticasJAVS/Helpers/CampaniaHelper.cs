using HDOpticasJAVS.Models;
using System;
using System.Linq;

namespace HDOpticasJAVS.Helpers
{
    public static class CampaniaHelper
    {
        public static void ProcesarCampaniaPorId(int idCampania)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var campania = db.CampaniaMarketing.Find(idCampania);
                if (campania == null || campania.Estado != "A") return;

                var destinatarios = db.CampaniaCliente
                    .Where(cc => cc.Id_Campania == idCampania && cc.Estado == "A")
                    .ToList();

                foreach (var cliente in destinatarios)
                {
                    string asunto = $"Promoción: {campania.Nombre_Campania}";

                    string redirectUrl = "https://localhost:44300/Home/About";
                    string urlClick = $"https://localhost:44300/Marketing/ContarClick?idCampania={idCampania}&cedulaCliente={cliente.Cedula_Cliente}&redirect={Uri.EscapeDataString(redirectUrl)}";
                    string urlApertura = $"https://localhost:44300/Marketing/ContarApertura?idCampania={idCampania}&cedulaCliente={cliente.Cedula_Cliente}";

                    string mensaje = $@"
                        <div style='text-align:center; font-family:Arial, sans-serif;'>
                            <img src='https://i.postimg.cc/25V3s41w/Logo-hdopticas.png' style='max-width:200px; margin-bottom:15px;' />
                            <h3>{campania.Nombre_Campania}</h3>
                            <p>{campania.Descripcion}</p>
                            <p><a href='{urlClick}'>Haz clic aquí para más información</a></p>
                            <img src='{urlApertura}' width='1' height='1' style='display:none;' />
                        </div>";

                    CorreoHelper.EnviarCorreo(cliente.Correo_Cliente, asunto, mensaje);

                    db.CampaniaMetrica.Add(new CampaniaMetrica
                    {
                        Id_Campania = idCampania,
                        Cedula_Cliente = cliente.Cedula_Cliente,
                        FechaRegistro = DateTime.Now
                    });
                }

                campania.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                campania.UsuarioModificador = "Sistema";

                db.SaveChanges();
            }
        }

        public static void ProcesarCampaniasProgramadas()
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var hoy = DateTime.Today;

                var campanias = db.CampaniaMarketing
                    .Where(c => c.Estado == "P" && c.Fecha_Programada == hoy)
                    .ToList();

                foreach (var campania in campanias)
                {
                    var destinatarios = db.CampaniaCliente
                        .Where(cc => cc.Id_Campania == campania.Id_Campania && cc.Estado == "A")
                        .ToList();

                    foreach (var cliente in destinatarios)
                    {
                        string asunto = $"Promoción: {campania.Nombre_Campania}";

                        string redirectUrl = "https://localhost:44300/Home/About";
                        string urlClick = $"https://localhost:44300/Marketing/ContarClick?idCampania={campania.Id_Campania}&cedulaCliente={cliente.Cedula_Cliente}&redirect={Uri.EscapeDataString(redirectUrl)}";
                        string urlApertura = $"https://localhost:44300/Marketing/ContarApertura?idCampania={campania.Id_Campania}&cedulaCliente={cliente.Cedula_Cliente}";

                        string mensaje = $@"
                            <div style='text-align:center; font-family:Arial, sans-serif;'>
                                <img src='https://i.postimg.cc/25V3s41w/Logo-hdopticas.png' style='max-width:200px; margin-bottom:15px;' />
                                <h3>{campania.Nombre_Campania}</h3>
                                <p>{campania.Descripcion}</p>
                                <p><a href='{urlClick}'>Haz clic aquí para más información</a></p>
                                <img src='{urlApertura}' width='1' height='1' style='display:none;' />
                            </div>";

                        CorreoHelper.EnviarCorreo(cliente.Correo_Cliente, asunto, mensaje);

                        db.CampaniaMetrica.Add(new CampaniaMetrica
                        {
                            Id_Campania = campania.Id_Campania,
                            Cedula_Cliente = cliente.Cedula_Cliente,
                            FechaRegistro = DateTime.Now
                        });
                    }

                    campania.Estado = "A";
                    campania.Fecha_Inicio = hoy;
                    campania.UsuarioModificador = "Sistema";
                    campania.FechaModificacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }

                db.SaveChanges();
            }
        }
    }
}
