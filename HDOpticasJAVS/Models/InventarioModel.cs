using System;
using System.ComponentModel.DataAnnotations;

namespace HDOpticasJAVS.Models.ViewModels
{
    public partial class Inventario
    {
        public int Id_Producto { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre del producto no puede exceder 100 caracteres.")]
        public string Nombre_Producto { get; set; }

        [Required(ErrorMessage = "El código del producto es obligatorio.")]
        [StringLength(50, ErrorMessage = "El código del producto no puede exceder 50 caracteres.")]
        public string Codigo_Producto { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número positivo.")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser un valor positivo.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El proveedor es obligatorio.")]
        public int Id_Proveedor { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
        public string Descripcion { get; set; }

        [Required]
        public string Estado { get; set; }

        [Required]
        public string UsuarioCreador { get; set; }

        [Required]
        public string FechaCreacion { get; set; }

        public string UsuarioModificador { get; set; }

        public string FechaModificacion { get; set; }
    }
}