using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Hangfire;
using Hangfire.SqlServer;
using HDOpticasJAVS.Controllers;


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

            // Configurar Hangfire para usar SQL Server como almacenamiento
            GlobalConfiguration.Configuration
                .UseSqlServerStorage("HangfireConnection");


            var server = new BackgroundJobServer();
            RecurringJob.AddOrUpdate(
    "alertas-diarias",
    () => new ClientesController().RevisarAlertasPendientes(),
    Cron.Daily
);
        }
}
}
