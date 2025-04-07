using System;
using System.ComponentModel.DataAnnotations;

namespace HDOpticasJAVS.Models
{
    public class UsuarioViewModel
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
        public string Contrasenia { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime Fecha_Nacimiento { get; set; }

        public string Rol { get; set; }
    }
}