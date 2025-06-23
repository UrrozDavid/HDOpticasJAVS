using System;

namespace HDOpticasJAVS.ViewModels
{
    public class CampaniaTendenciaViewModel
    {
        public string NombreCampania { get; set; }
        public int TotalEnviados { get; set; }
        public int TotalAbiertos { get; set; }
        public int TotalClicks { get; set; }

        public double PorcentajeApertura { get; set; }
        public double PorcentajeClick { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public bool MetricaInvalida => (TotalAbiertos + TotalClicks) > TotalEnviados;
    }
}
