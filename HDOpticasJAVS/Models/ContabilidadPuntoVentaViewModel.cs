using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class ContabilidadPuntoVentaViewModel
    {
        // Campos de Contabilidad
        public int Id_Contabilidad { get; set; }
        public int? Id_Venta { get; set; }
        public int? Id_Producto { get; set; }
        public int? Cantidad { get; set; }
        public decimal? PrecioUnitario { get; set; }
        public decimal? SubtotalContabilidad { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? IVAContabilidad { get; set; }
        public decimal? TotalContabilidad { get; set; }
        public DateTime? Fecha_Registro { get; set; }
        public string Usuario_Registro { get; set; }
        public int? Id_TipoMovimiento { get; set; }
        public string EstadoContabilidad { get; set; }
        public string TipoOperacion { get; set; }
        public string TipoServicio { get; set; }
        public string TipoMovimientoIngresoEgreso { get; set; }

        // Campos de PuntoVenta
        public string Cedula_Cliente { get; set; }
        public decimal? SubtotalPuntoVenta { get; set; }
        public decimal? IVAPuntoVenta { get; set; }
        public decimal? TotalPuntoVenta { get; set; }
        public int? Id_MetodoPago { get; set; }
        public DateTime? Fecha_Venta { get; set; }
        public TimeSpan? Hora_Venta { get; set; }
        public string EstadoPuntoVenta { get; set; }
    }
}