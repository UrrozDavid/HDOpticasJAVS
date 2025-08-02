using System;
using System.ComponentModel.DataAnnotations;

namespace HDOpticasJAVS.Models
{
    public class ClienteViewModel
    {
        // Datos de Usuario
        [Required(ErrorMessage = "La cédula es obligatoria.")]
        public string Cedula { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        public string Apellido1 { get; set; }

        [Required(ErrorMessage = "El segundo apellido es obligatorio.")]
        public string Apellido2 { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        public string Correo { get; set; }  // Ya no es obligatorio

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime Fecha_Nacimiento { get; set; }

        // Datos de Cliente
        [Required(ErrorMessage = "La edad es obligatoria.")]
        [Range(0, 120, ErrorMessage = "Ingrese una edad válida.")]
        public int Edad { get; set; }

        [Required(ErrorMessage = "El género es obligatorio.")]
        public string Genero { get; set; }

        public string Padecimiento { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [Phone(ErrorMessage = "Ingrese un número de teléfono válido.")]
        public string Numero_Telefono { get; set; }
    }
}
