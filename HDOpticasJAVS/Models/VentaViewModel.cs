using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HDOpticasJAVS.Models
{
    public class VentaViewModel
    {
        public string CedulaCliente { get; set; }

        public List<ItemProducto> ProductosDisponibles { get; set; } = new List<ItemProducto>();
        public List<ItemVenta> Carrito { get; set; } = new List<ItemVenta>();

        public decimal Efectivo { get; set; }
        public decimal MontoTarjeta { get; set; }

        // Datos de tarjeta simulada
        public string NumeroTarjeta { get; set; }
        public string NombreTitular { get; set; }
        public string FechaVencimiento { get; set; }
        public string CVV { get; set; }

        public decimal TotalCompra { get; set; }
    }

    public class ItemProducto
    {
        public int Id_Producto { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
    }

    public class ItemVenta
    {
        public int Id_Producto { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }
}