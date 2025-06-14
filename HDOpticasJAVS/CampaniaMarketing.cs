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
    
    public partial class CampaniaMarketing
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CampaniaMarketing()
        {
            this.CampaniaCliente = new HashSet<CampaniaCliente>();
            this.CampaniaMetrica = new HashSet<CampaniaMetrica>();
        }
    
        public int Id_Campania { get; set; }
        public string Nombre_Campania { get; set; }
        public string Descripcion { get; set; }
        public string Tipo { get; set; }
        public Nullable<System.DateTime> Fecha_Inicio { get; set; }
        public Nullable<System.DateTime> Fecha_Fin { get; set; }
        public string Estado { get; set; }
        public string UsuarioCreador { get; set; }
        public string FechaCreacion { get; set; }
        public string UsuarioModificador { get; set; }
        public string FechaModificacion { get; set; }
        public Nullable<System.DateTime> Fecha_Programada { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CampaniaCliente> CampaniaCliente { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CampaniaMetrica> CampaniaMetrica { get; set; }
    }
}
