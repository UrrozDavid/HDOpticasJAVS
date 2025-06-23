using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDOpticasJAVS
{
    public partial class Cliente
    {
        public string NombreCompleto => Usuario != null
            ? $"{Usuario.Nombre} {Usuario.Apellido1} {Usuario.Apellido2}"
            : "(Sin nombre)";
    }
}