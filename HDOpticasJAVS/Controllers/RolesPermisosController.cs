using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HDOpticasJAVS;

public class RolesPermisosController : BaseController
{
    private HD_Opticas_JAVS_BDEntities db = new HD_Opticas_JAVS_BDEntities();

    public ActionResult Index()
    {
        ViewBag.Roles = db.Parametro.Where(p => p.Id_TipoParametro == 1 && p.Estado == "A").ToList();
        ViewBag.Modulos = db.Modulo.Where(m => m.Estado == "A").ToList();
        ViewBag.Usuarios = db.Usuario.Where(u => u.Estado == "A").ToList();

        return View();
    }

    [HttpPost]
    public JsonResult CrearRol(string nombre)
    {
        var nuevo = new Parametro
        {
            Nombre_Parametro = nombre,
            Estado = "A",
            Id_TipoParametro = 1
        };
        db.Parametro.Add(nuevo);
        db.SaveChanges();
        return Json(nuevo);
    }

    [HttpPost]
    public JsonResult EditarRol(int id, string nuevoNombre)
    {
        var rol = db.Parametro.Find(id);
        if (rol != null)
        {
            rol.Nombre_Parametro = nuevoNombre;
            db.SaveChanges();
            return Json(true);
        }
        return Json(false);
    }

    [HttpPost]
    public JsonResult EliminarRol(int id)
    {
        var tieneUsuarios = db.Usuario.Any(u => u.Id_Rol == id);
        if (tieneUsuarios)
        {
            return Json(new { success = false, mensaje = "No se puede eliminar un rol asignado a usuarios." });
        }

        var rol = db.Parametro.Find(id);
        if (rol != null)
        {
            rol.Estado = "I";
            db.SaveChanges();
            return Json(new { success = true });
        }

        return Json(new { success = false, mensaje = "Rol no encontrado." });
    }

    [HttpPost]
    public JsonResult AsignarPermiso(int idRol, int idModulo, bool asignar)
    {
        if (asignar)
        {
            if (!db.PermisoRol.Any(p => p.Id_Rol == idRol && p.Id_Modulo == idModulo))
            {
                db.PermisoRol.Add(new PermisoRol { Id_Rol = idRol, Id_Modulo = idModulo });
            }
        }
        else
        {
            var existente = db.PermisoRol.FirstOrDefault(p => p.Id_Rol == idRol && p.Id_Modulo == idModulo);
            if (existente != null)
            {
                db.PermisoRol.Remove(existente);
            }
        }
        db.SaveChanges();
        return Json(true);
    }

    [HttpPost]
    public JsonResult AsignarRolMasivo(List<string> idsUsuarios, int idRol)
    {
        if (idsUsuarios == null || !idsUsuarios.Any())
            return Json(new { success = false, mensaje = "No se recibieron usuarios." });

        var rol = db.Parametro.FirstOrDefault(p => p.Id_Parametro == idRol && p.Id_TipoParametro == 1);
        if (rol == null)
            return Json(new { success = false, mensaje = "Rol inválido." });

        foreach (var idUsuario in idsUsuarios)
        {
            var usuario = db.Usuario.Find(idUsuario);
            if (usuario != null)
            {
                usuario.Id_Rol = idRol;
            }
        }

        db.SaveChanges();
        return Json(new { success = true });
    }

    [HttpGet]
    public JsonResult ObtenerPermisosRol(int idRol)
    {
        var modulosAsignados = db.PermisoRol
            .Where(p => p.Id_Rol == idRol)
            .Select(p => p.Id_Modulo)
            .ToList();

        return Json(modulosAsignados, JsonRequestBehavior.AllowGet);
    }

    [HttpPost]
    public JsonResult ActualizarPermisosRol(int idRol, List<PermisoUpdateModel> permisos)
    {
        var actuales = db.PermisoRol.Where(p => p.Id_Rol == idRol).ToList();
        db.PermisoRol.RemoveRange(actuales);
        db.SaveChanges();

        foreach (var permiso in permisos)
        {
            if (permiso.Asignar)
            {
                db.PermisoRol.Add(new PermisoRol
                {
                    Id_Rol = idRol,
                    Id_Modulo = permiso.Id_Modulo
                });
            }
        }

        db.SaveChanges();
        return Json(new { success = true });
    }

    public class PermisoUpdateModel
    {
        public int Id_Modulo { get; set; }
        public bool Asignar { get; set; }
    }
}