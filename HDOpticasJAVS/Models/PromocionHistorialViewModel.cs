using System;

namespace HDOpticasJAVS.Models.ViewModels
{
    public class PromocionHistorialItemViewModel
    {
        public DateTime? Fecha { get; set; }
        public string Tipo { get; set; }
        public string Campania { get; set; }
        public string CodigoPromo { get; set; }
        public decimal? MontoDescuento { get; set; }
        public int? IdVenta { get; set; }
        public decimal? TotalVenta { get; set; }
    }
}
