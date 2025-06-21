using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class ListaSegmentadaCliente
    {
        [Key]
        public int Id_ListaCliente { get; set; }

        public int Id_Lista { get; set; }
        [ForeignKey("Id_Lista")]
        public virtual ListaSegmentada Lista { get; set; }

        public string Cedula_Cliente { get; set; }
    }

}