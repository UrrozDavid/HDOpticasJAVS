using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS;
using HDOpticasJAVS.Models;

public class BaseController : Controller
{
    // 1. Control de permisos
    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        /*if (Session["Usuario"] == null)
        {
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.Result = new JsonResult
                {
                    Data = new { redirectTo = Url.Action("Login", "Account", new { motivo = "timeout" }) },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else
            {
                filterContext.Result = new RedirectResult("~/Account/Login?motivo=timeout");
            }
            return;
        }*/

        var rolId = Session["Rol"] as int?;
        string controlador = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
        string accion = filterContext.ActionDescriptor.ActionName;

        var accionesPermitidasSinPermiso = new List<(string controlador, string accion)>
        {
            ("Home", "Index"),
            ("Home", "SinPermiso"),
            ("Account", "Logout")
        };

        if (accionesPermitidasSinPermiso.Any(a => a.controlador == controlador && a.accion == accion))
        {
            base.OnActionExecuting(filterContext);
            return;
        }

        if (rolId != null)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var modulo = (from m in db.Modulo
                              where m.Controlador == controlador
                                    && m.Accion == accion
                                    && m.Estado == "A"
                              select m).FirstOrDefault();

                if (modulo == null)
                {
                    base.OnActionExecuting(filterContext);
                    return;
                }

                var tienePermiso = (from pr in db.PermisoRol
                                    where pr.Id_Rol == rolId && pr.Id_Modulo == modulo.Id_Modulo
                                    select pr).Any();

                if (!tienePermiso)
                {
                    filterContext.Result = new RedirectResult("~/Home/SinPermiso");
                    return;
                }
            }
        }

        base.OnActionExecuting(filterContext);
    }

    // 2. Carga de módulos justo antes de renderizar la vista
    protected override void OnResultExecuting(ResultExecutingContext filterContext)
    {
        var rolId = Session["Rol"] as int?;

        if (rolId != null)
        {
            using (var db = new HD_Opticas_JAVS_BDEntities())
            {
                var modulosUsuario = (from pr in db.PermisoRol
                                      join m in db.Modulo on pr.Id_Modulo equals m.Id_Modulo
                                      where pr.Id_Rol == rolId && m.Estado == "A"
                                      select m).ToList();

                ViewBag.Modulos = modulosUsuario;
            }
        }

        base.OnResultExecuting(filterContext);
    }


}