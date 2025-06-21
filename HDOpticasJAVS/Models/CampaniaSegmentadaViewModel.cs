using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HDOpticasJAVS.Models.ViewModels
{
    public class CampaniaSegmentadaViewModel
    {
        public int Id_Campania { get; set; }

        [Required]
        public int Id_Lista { get; set; }

        public List<SelectListItem> ListasDisponibles { get; set; }

        public string Asunto { get; set; }
        public string MensajeHtml { get; set; }
    }

}