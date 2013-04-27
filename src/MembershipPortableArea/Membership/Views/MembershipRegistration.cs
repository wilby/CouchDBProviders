using MvcContrib.PortableAreas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MembershipPortableArea.Membership
{
    public class MembershipRegistration : PortableAreaRegistration
    {
        public override void RegisterArea(System.Web.Mvc.AreaRegistrationContext context, IApplicationBus bus)
        {
            bus.Send(
        }
        

        public override string AreaName
        {
            get { return "Membership"; }
        }
    }
}
