using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using System.Configuration;
using System.Collections.Specialized;
using System.Web.Configuration;

namespace CouchDBMembershipProvider.Windsor.Installers
{
    public class CouchInstaller : IWindsorInstaller
    {
        //NameValueCollection membershipSettings = (MembershipSection)ConfigurationSettings.GetConfig("Membership");
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            container.Register(Classes.FromThisAssembly().Where(x => x.Name.StartsWith("CouchDB")).LifestyleTransient()
                );
        }
    
public  NameValueCollection ConfiguationManager { get; set; }}
}
