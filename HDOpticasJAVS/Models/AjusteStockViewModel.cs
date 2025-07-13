using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class AjusteStockViewModel
    {
        public int Id_Producto { get; set; }
        public string Nombre_Producto { get; set; }

        [Required(ErrorMessage = "Debe ingresar la cantidad a ajustar")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "Debe indicar el motivo del ajuste")]
        public string Motivo { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el tipo de ajuste")]
        public string Tipo { get; set; } // "Aumentar" o "Disminuir"

        // Nueva propiedad para mostrar el stock actual
        public int StockActual { get; set; }
    }
}