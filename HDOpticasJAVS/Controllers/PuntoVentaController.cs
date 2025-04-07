using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HDOpticasJAVS.Controllers
{
    public class PuntoVentaController : Controller
    {
        // GET: PuntoVenta
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult IndexAdmin()
        {
            return View();
        }
    }
}