using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class CreditoAdminViewModel
    {
        public int Id_NotaCredito { get; set; }
        public string Cedula_Cliente { get; set; }
        public string NombreCompleto { get; set; }
        public decimal MontoOtorgado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public DateTime FechaOtorgado { get; set; }
        public string Estado { get; set; }
    }
}