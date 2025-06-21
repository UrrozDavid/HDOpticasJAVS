using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace HDOpticasJAVS.Models.ViewModels
{
    public class ReglasRecurrenciaViewModel
    {
        [Required]
        [Display(Name = "Número mínimo de compras")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe ser mayor a cero.")]
        public int UmbralCompras { get; set; }
    }
}