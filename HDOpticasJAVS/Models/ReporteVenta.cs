using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class ReporteVenta
    {
        public int Id_Venta { get; set; }
        public DateTime Fecha_Venta { get; set; }
        public string MetodoPago { get; set; }
        public decimal Total { get; set; }
        public string Empleado { get; set; }
    }
}