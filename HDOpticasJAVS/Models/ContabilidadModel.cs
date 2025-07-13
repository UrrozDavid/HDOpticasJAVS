using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS.Models
{
    public class ContabilidadModel
    {
        public int Id_Contabilidad { get; set; }
        public int? Id_Venta { get; set; }
        public int Id_Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal? PrecioUnitario { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? IVA { get; set; }
        public decimal Total { get; set; }
        public DateTime? Fecha_Registro { get; set; }
        public string Usuario_Registro { get; set; }
        public int? Id_TipoMovimiento { get; set; }
        public string Estado { get; set; }
        public string UsuarioCreador { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string UsuarioModificador { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public virtual Inventario Inventario { get; set; }
        // Agrega más relaciones si usas EF y las tienes creadas

        public string TipoOperacion { get; set; }
        public string TipoServicio { get; set; }
        public string TipoMovimientoIngresoEgreso { get; set; }

    }
}