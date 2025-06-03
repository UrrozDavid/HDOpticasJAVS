using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class NotaCredito
    {
        public int Id_NotaCredito { get; set; }
        public int Id_Venta { get; set; }
        public decimal Monto { get; set; }
        public string Motivo { get; set; }
        public DateTime Fecha_Emision { get; set; }
        public int? AplicadaEnVenta { get; set; }
        public string Estado { get; set; }
        public string UsuarioCreador { get; set; }
        public string FechaCreacion { get; set; }
        public string UsuarioModificador { get; set; }
        public string FechaModificacion { get; set; }
    }

}