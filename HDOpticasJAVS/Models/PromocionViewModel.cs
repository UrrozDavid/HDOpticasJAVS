namespace HDOpticasJAVS.Models.ViewModels
{
    public class PromocionViewModel
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string ImagenUrl { get; set; }
        public int IdCampania { get; set; }
        public string CedulaCliente { get; set; }

        public bool YaAplicada { get; set; } // Nueva propiedad
    }
}

