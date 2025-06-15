using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class SegmentacionViewModel
    {
        public string Nombre { get; set; }
        public int? EdadMinima { get; set; }
        public int? EdadMaxima { get; set; }
        public string Tratamiento { get; set; }

        public List<Cliente> Resultados { get; set; } = new List<Cliente>();
    }

}