using System;
using System.ComponentModel.DataAnnotations;

namespace HDOpticasJAVS.Models
{
    public class EmpleadoViewModel
    {
        [Required]
        public string Cedula { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido1 { get; set; }

        [Required]
        public string Apellido2 { get; set; }

        [Required, EmailAddress]
        public string Correo { get; set; }
        [Required]
        public string Direccion { get; set; }
        [Required]
        public string NumeroTelefono { get; set; }
        [Required]
        public string ContactoEmergencia { get; set; }
        [Required]
        public string Placa_Vehiculo { get; set; }

        [Required]
        public string Contrasenia { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        public string Estado { get; set; }

        public string Rol { get; set; }

        public string Parametro { get; set; }

    }
}