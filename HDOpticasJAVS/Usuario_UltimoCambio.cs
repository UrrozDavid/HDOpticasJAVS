//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HDOpticasJAVS
{
    using System;
    using System.Collections.Generic;
    
    public partial class Usuario_UltimoCambio
    {
        public int Id { get; set; }
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Apellido1 { get; set; }
        public string Apellido2 { get; set; }
        public string Contrasenia { get; set; }
        public Nullable<System.DateTime> FechaNacimiento { get; set; }
        public string Correo { get; set; }
        public Nullable<int> Id_Rol { get; set; }
        public Nullable<System.DateTime> FechaBloqueoHasta { get; set; }
        public Nullable<System.DateTime> FechaCambio { get; set; }
        public Nullable<bool> Revertido { get; set; }
    }
}
