using System;

namespace HDOpticasJAVS.Models
{
    public class PuntoVenta
    {
        public int Id_Venta { get; set; }
        public int Id_Producto { get; set; }
        public string Cedula_Cliente { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }
        public int Id_MetodoPago { get; set; }
        public DateTime Fecha_Venta { get; set; }
        public string Hora_Venta { get; set; }
        public string Estado { get; set; }
        public string UsuarioCreador { get; set; }
        public string FechaCreacion { get; set; }
        public string UsuarioModificador { get; set; }
        public string FechaModificacion { get; set; }
        public string NombreEmpleado { get; set; }
    }
}
