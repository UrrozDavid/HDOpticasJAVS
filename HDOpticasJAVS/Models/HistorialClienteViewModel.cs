using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models.ViewModels
{
    public class HistorialClienteViewModel
    {
        public string CedulaCliente { get; set; }
        public DateTime? FechaRegistro { get; set; }

        [Required(ErrorMessage = "Los antecedentes son obligatorios.")]
        public string Antecedentes { get; set; }

        [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
        public string Diagnostico { get; set; }

        [Required(ErrorMessage = "El tratamiento es obligatorio.")]
        public string Tratamiento { get; set; }

        [Required(ErrorMessage = "Las observaciones son obligatorias.")]
        public string Observaciones { get; set; }
        public string UsuarioRegistro { get; set; }
        public DateTime? FechaProximoSeguimiento { get; set; }


    }
}
