using System;

namespace HDOpticasJAVS.Models.ViewModels
{
    public class CampaniaReporteViewModel
    {
        public int Id_Campania { get; set; }
        public string Nombre_Campania { get; set; }
        public string Descripcion { get; set; }
        public int TotalDestinatarios { get; set; }
        public int TotalAbiertos { get; set; }
        public int TotalClicks { get; set; }
        public decimal PorcentajeApertura { get; set; }
        public decimal PorcentajeClicks { get; set; }

    }
}
