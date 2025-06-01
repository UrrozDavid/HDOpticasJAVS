using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class RecuperacionPasswordModel
    {
        public string Correo { get; set; }
        public string Token { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}