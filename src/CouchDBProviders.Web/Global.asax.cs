using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CouchDBProviders.Web.Infrastructure;
using Microsoft.Practices.ServiceLocation;
using System.Reflection;
using System.IO;
using Castle.Windsor;
using Castle.MicroKernel.Registration;




namespace CouchDBProviders.Web
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : System.Web.HttpApplication
	{
		public static IWindsorContainer Container;

		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

			routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
			);

		}

		protected void Application_Start()
		{
			Container = new WindsorContainer();

			// Common Service Locator
			ServiceLocator.SetLocatorProvider(() => new WindsorServiceLocator(Container));
            			

			// MVC components
			ControllerBuilder.Current.SetControllerFactory(new WindsorControllerFactory(Container));
			Container.Register(Classes
				.FromAssembly(Assembly.GetExecutingAssembly())
				.BasedOn<IController>().LifestyleTransient()
                
			);

			AreaRegistration.RegisterAllAreas();
			RegisterGlobalFilters(GlobalFilters.Filters);
			RegisterRoutes(RouteTable.Routes);
		}

		protected void Application_End()
		{
		
		}
		
	}
}