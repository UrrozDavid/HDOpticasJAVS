using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HDOpticasJAVS.Models
{
    public class VentaViewModel
    {
        // Datos del cliente
        [Required]
        public string CedulaCliente { get; set; }

        // Productos disponibles y carrito
        public List<ItemProducto> ProductosDisponibles { get; set; } = new List<ItemProducto>();
        public List<ItemVenta> Carrito { get; set; } = new List<ItemVenta>();

        // Métodos de pago
        public decimal Efectivo { get; set; }
        public decimal MontoTarjeta { get; set; }
        public decimal MontoCredito { get; set; }

        // Datos de tarjeta simulada
        public string NumeroTarjeta { get; set; }
        public string NombreTitular { get; set; }
        public string FechaVencimiento { get; set; }
        public string CVV { get; set; }

        // Totales
        public decimal TotalCompra { get; set; }

        // Para asociar con NotaCredito y sus pagos
        public int? IdNotaCredito { get; set; }
        public List<HDOpticasJAVS.NotaCredito> CreditosCliente { get; set; }
            = new List<HDOpticasJAVS.NotaCredito>();
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