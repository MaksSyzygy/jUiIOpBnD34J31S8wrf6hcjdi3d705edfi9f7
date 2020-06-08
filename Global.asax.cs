using Checkitlink.Models.Data;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Checkitlink
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_AuthenticateRequest()
        {
            if(User == null)
            {
                return;
            }

            string login = Context.User.Identity.Name;

            string[] roles = null;

            using(ChekitDB chekitDB = new ChekitDB())
            {
                UsersDTO usersDTO = chekitDB.Users.FirstOrDefault(x=>x.Login == login);

                if(usersDTO == null)
                {
                    return;
                }

                roles = chekitDB.UserRoles.Where(x => x.UserId == usersDTO.UserId).Select(x => x.RoleUser.RoleName).ToArray();
            }

            IIdentity userIdentity = new GenericIdentity(login);
            IPrincipal newUserObject = new GenericPrincipal(userIdentity, roles);

            Context.User = newUserObject;
        }
    }
}
