using System;
using System.ComponentModel.DataAnnotations;

namespace HDOpticasJAVS.Models
{
    public class ClienteViewModel
    {
        // Datos de Usuario
        [Required]
        public string Cedula { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido1 { get; set; }

        [Required]
        public string Apellido2 { get; set; }

        [Required]
        [EmailAddress]
        public string Correo { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Contrasenia { get; set; }

        [Required]
        public DateTime Fecha_Nacimiento { get; set; }

        // Datos de Cliente
        [Required]
        public int Edad { get; set; }

        [Required]
        public string Genero { get; set; }

        public string Padecimiento { get; set; }

        [Required]
        public string Numero_Telefono { get; set; }
    }
}