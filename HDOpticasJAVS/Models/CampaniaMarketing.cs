using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HDOpticasJAVS.Models.ViewModels
{
    public class CampaniaMarketingViewModel
    {
        public int Id_Campania { get; set; }

        [Required]
        public string Nombre_Campania { get; set; }

        public string Descripcion { get; set; }

        public string Tipo { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha_Inicio { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Fecha_Fin { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Fecha_Programada { get; set; }

        public string Estado { get; set; }

        public List<ClienteSeleccionado> ClientesSeleccionados { get; set; } = new List<ClienteSeleccionado>();
    }

    public class ClienteSeleccionado
    {
        public string Cedula { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public bool Seleccionado { get; set; }
    }
}
