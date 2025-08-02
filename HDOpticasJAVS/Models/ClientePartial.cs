using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS
{
    public partial class Cliente
    {
        public Usuario Usuario
        {
            get
            {
                using (var db = new HD_Opticas_JAVS_BDEntities())
                {
                    return db.Usuario.FirstOrDefault(u => u.Cedula == this.Cedula);
                }
            }
        }
    }
}
