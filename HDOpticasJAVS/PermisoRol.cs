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
    
    public partial class PermisoRol
    {
        public int Id_PermisoRol { get; set; }
        public Nullable<int> Id_Rol { get; set; }
        public Nullable<int> Id_Modulo { get; set; }
    
        public virtual Modulo Modulo { get; set; }
        public virtual Parametro Parametro { get; set; }
    }
}
