using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace CouchDBMembershipProvider
{
    /// <summary>
    /// A hook into the application initialization like Global.asax APPLICATION_START to allow Castle.Windsor
    /// to initialize through configuration
    /// </summary>
    /// <example>
    /// To enable this module, place the following in the web.config file.
    /// <httpModules>
    ///     <add name="WindosorHookHttpModule" type="CouchDBMembershipProvider.WindsorHookHttpModule, CouchDBMembershipProvider" />
    /// </httpModules>
    /// </example>
    public class WindsorHookHttpModule : IHttpModule
    {
        public static IWindsorContainer IOCContainer;

        public void Dispose()
        {
            IOCContainer.Dispose();
            IOCContainer = null;
        }

        public void Init(HttpApplication context)
        {            
            IOCContainer = new WindsorContainer()
            .Install(FromAssembly.This());            
        }
    }
}
