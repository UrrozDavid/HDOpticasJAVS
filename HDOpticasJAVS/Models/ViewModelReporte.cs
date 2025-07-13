using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class ReporteEgresosPorCategoriaViewModel
    {
        public int? Id_TipoMovimiento { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public List<EgresoCategoria> EgresosPorCategoria { get; set; }
    }

    public class EgresoCategoria
    {
        public string Categoria { get; set; }
        public decimal Total { get; set; }
    }
}