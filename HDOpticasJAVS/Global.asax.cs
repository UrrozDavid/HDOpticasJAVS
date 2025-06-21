using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Hangfire;
using Hangfire.SqlServer;

namespace HDOpticasJAVS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Configuración de Hangfire
            GlobalConfiguration.Configuration
                .UseSqlServerStorage("HangfireConnection");

            var server = new BackgroundJobServer();

            // ⏰ Tarea programada: promociones recurrentes
            RecurringJob.AddOrUpdate(
                "promociones-recurrentes",
                () => HDOpticasJAVS.Helpers.PromocionHelper.ProcesarPromocionesRecurrentes(),
                Cron.Daily
            );
        }
        
    }
}

