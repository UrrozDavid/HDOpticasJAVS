using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class PerfilClienteViewModel
    {
        public string NombreUsuario { get; set; }
        public List<CitaViewModel> HistorialCitas { get; set; }
        public List<CompraViewModel> HistorialCompras { get; set; }
        public DateTime UltimaActualizacion { get; set; }
    }

    public class CitaViewModel
    {
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }
    }

    public class CompraViewModel
    {
        public string Producto { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
    }
}