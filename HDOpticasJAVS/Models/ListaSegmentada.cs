using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class ListaSegmentada
    {
        [Key]
        public int Id_Lista { get; set; }

        public string NombreLista { get; set; }

        public DateTime FechaCreacion { get; set; }

        public virtual ICollection<ListaSegmentadaCliente> Clientes { get; set; }
    }
}
