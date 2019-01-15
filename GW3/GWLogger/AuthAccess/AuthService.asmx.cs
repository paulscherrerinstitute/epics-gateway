using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Xml.Serialization;

namespace GWLogger.AuthAccess
{
    /// <summary>
    /// Summary description for AuthService
    /// </summary>
    [WebService(Namespace = "http://caesar.psi.ch/AuthAccess/AuthService.asmx")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class AuthService : System.Web.Services.WebService
    {
        private static List<AlertSubscription> Subscriptions { get; set; }

        static AuthService()
        {
            try
            {
                using (var stream = File.OpenRead(Global.StorageDirectory + "\\AlertSubscriptions.xml"))
                {
                    var ser = new XmlSerializer(typeof(List<AlertSubscription>));
                    var data = (List<AlertSubscription>)ser.Deserialize(stream);
                    Subscriptions = data;
                }
            }
            catch
            {
                Subscriptions = new List<AlertSubscription>();
            }
        }

        private static void StoreSubscriptions()
        {
            var subs = GetAllSubscriptions();
            using (var stream = File.Create(Global.StorageDirectory + "\\AlertSubscriptions.xml"))
            {
                var ser = new XmlSerializer(typeof(List<AlertSubscription>));
                ser.Serialize(stream, subs);
            }
        }

        internal static List<AlertSubscription> GetAllSubscriptions()
        {
            lock (Subscriptions)
            {
                return Subscriptions.ToList();
            }
        }

        internal static void DeleteSubscription(string email)
        {
            lock (Subscriptions)
            {
                Subscriptions.RemoveAll(row => row.EMail.ToLower() == email.ToLower());
            }
            StoreSubscriptions();
        }

        internal static void AddSubscription(AlertSubscription subscription)
        {
            lock (Subscriptions)
            {
                Subscriptions.Add(subscription);
            }
            StoreSubscriptions();
        }

        internal static List<string> SubscribedEmailsForGateway(string gateway)
        {
            var subs = GetAllSubscriptions();
            return subs.Where(row => row.Gateways.Any(r2 => r2.ToLower() == gateway.ToLower())).Select(row => row.EMail).ToList();
        }

        [WebMethod]
        public string CurrentUser()
        {
            return HttpContext.Current.Request.LogonUserIdentity.Name;
        }

        [WebMethod]
        public string CurrentUserEmail()
        {
            // get a DirectorySearcher object
            using (var ldap = new LdapConnection(ConfigurationManager.AppSettings["ldapServer"]))
            {

                // specify the search filter
                var filter = string.Format("(&(objectClass=user)(objectCategory=person)(samAccountName={0}))", CurrentUser().Split('\\').Last());
                //var searchAttributes = new string[] { "samAccountName", "sn", "givenName", "mail", "telephoneNumber", "physicalDeliveryOfficeName" };
                var searchAttributes = new string[] { "mail" };
                var request = new SearchRequest(ConfigurationManager.AppSettings["ldapRoot"], filter, System.DirectoryServices.Protocols.SearchScope.Subtree, searchAttributes);
                var response = (SearchResponse)ldap.SendRequest(request);
                return (string)(response.Entries.Cast<SearchResultEntry>().First().Attributes["mail"][0]);
            }
        }

        [WebMethod]
        public void Unsubscribe()
        {
            AuthService.DeleteSubscription(CurrentUserEmail());
        }

        [WebMethod]
        public void Subscribe(List<string> gateways)
        {
            AuthService.DeleteSubscription(CurrentUserEmail());
            AuthService.AddSubscription(new AlertSubscription
            {
                EMail = CurrentUserEmail(),
                Gateways = gateways
            });
        }

        [WebMethod]
        public List<string> GetCurrentSubscription()
        {
            var subs = AuthService.GetAllSubscriptions().FirstOrDefault(row => row.EMail == CurrentUserEmail());
            if (subs == null)
                return new List<string>();
            return subs.Gateways;
        }

        [WebMethod]
        public void TestEmail(string destination)
        {
            Live.LiveInformation.SendEmail(destination, "Test email", "If you receive this email, everything is fine.");
        }

        [WebMethod]
        public string GatewayCommand(string gatewayName, string command)
        {
            if (!Global.Inventory.GetRolesForUser(CurrentUser().Split('\\').Last()).Any(row => row.Access == "Administrator" || row.Access == "EPICS Gateway Manager"))
                throw new System.Security.SecurityException("You don't have the right to issue such commands.");
            var allowedCommands = new string[] { "UpdateGateway", "UpdateGateway3", "RestartGateway", "RestartGateway3" };
            if (!allowedCommands.Contains(command))
                throw new System.Security.SecurityException("Command not allowed.");
            return Global.DirectCommands.StartTask(gatewayName, CurrentUser(), command);
        }
    }
}
