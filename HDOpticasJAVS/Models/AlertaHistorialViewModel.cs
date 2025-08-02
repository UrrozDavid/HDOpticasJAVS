using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.ViewModels
{
    public class AlertaHistorialViewModel
    {
        public string Cedula_Cliente { get; set; }
        public DateTime? FechaAlerta { get; set; }
        public string Mensaje { get; set; }
        public bool? Enviada { get; set; }
        public string MedioEnvio { get; set; }
   
    }
}
