using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Services;
using System.Xml.Serialization;
using GWLogger.Model;

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
        public string Login(string username, string password)
        {
            var token = TokenManager.CreateToken(username, password, LdapHelper.GetUserEmail(username, password), Context.Request.UserHostAddress);
            return token.Id;
        }

        [WebMethod]
        public void Logout(string tokenId)
        {
            TokenManager.DestroyToken(tokenId);
        }

        [WebMethod]
        public void RenewToken(string tokenId)
        {
            TokenManager.RenewToken(tokenId, Context.Request.UserHostAddress);
        }

        [WebMethod]
        public string CurrentUser(string tokenId)
        {
            return TokenManager.GetToken(tokenId, Context.Request.UserHostAddress).Login;
        }

        [WebMethod]
        public string CurrentUserEmail(string tokenId)
        {
            return TokenManager.GetToken(tokenId, Context.Request.UserHostAddress).Email;
        }

        [WebMethod]
        public void Unsubscribe(string tokenId)
        {
            AuthService.DeleteSubscription(CurrentUserEmail(tokenId));
        }

        [WebMethod]
        public void Subscribe(List<string> gateways, string tokenId)
        {
            AuthService.DeleteSubscription(CurrentUserEmail(tokenId));
            AuthService.AddSubscription(new AlertSubscription
            {
                EMail = CurrentUserEmail(tokenId),
                Gateways = gateways
            });
        }

        [WebMethod]
        public List<string> GetCurrentSubscription(string tokenId)
        {
            var subs = AuthService.GetAllSubscriptions().FirstOrDefault(row => row.EMail == CurrentUserEmail(tokenId));
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
        public string GatewayCommand(string gatewayName, string command, string tokenId)
        {
            /*if (!Global.Inventory.GetRolesForUser(CurrentUser(tokenId).Split('\\').Last()).Any(row => row.Access == "Administrator" || row.Access == "EPICS Gateway Manager"))
                throw new System.Security.SecurityException("You don't have the right to issue such commands.");*/
            //var serverMon = new ServerMon.CaesarApiSoapClient();
            var token = TokenManager.GetToken(tokenId, Context.Request.UserHostAddress);
            return string.Join("\n", Global.ServerMon.StartPackage(token.Login, token.Password, gatewayName, (ServerMon.CaesarPackage)Enum.Parse(typeof(ServerMon.CaesarPackage), command)));

            /*if (!Global.Inventory.GetRolesForUser(CurrentUser(tokenId).Split('\\').Last()).Any(row => row.Access == "Administrator" || row.Access == "EPICS Gateway Manager"))
                throw new System.Security.SecurityException("You don't have the right to issue such commands.");
            var allowedCommands = new string[] { "UpdateGateway", "UpdateGateway3", "RestartGateway", "RestartGateway3" };
            if (!allowedCommands.Contains(command))
                throw new System.Security.SecurityException("Command not allowed.");
            return Global.DirectCommands.StartTask(gatewayName, CurrentUser(tokenId), command);*/
        }

        [WebMethod]
        public void SaveGatewayConfiguration(string json, string tokenId)
        {
            var user = CurrentUser(tokenId);
            var config = Backend.Controllers.ConfigController.JsonToConfig(json);
            if (!HasEditConfigRole(config.Name, tokenId))
                throw new UnauthorizedAccessException();
            Backend.Controllers.ConfigController.SetConfiguration(json);
        }

        [WebMethod]
        public void CreateNewGateway(string gatewayName, string tokenId)
        {
            if (!HasAdminRole(tokenId))
                throw new UnauthorizedAccessException();
            var regExp = "^[A-Z\\-0-9]+$";
            if (!Regex.IsMatch(gatewayName, regExp))
                throw new Exception("Wrong gateway name");
            using (var ctx = new Model.CaesarContext())
            {
                ctx.Gateways.Add(new GatewayEntry { GatewayName = gatewayName, IsMain = false });
                ctx.SaveChanges();
            }
        }

        [WebMethod]
        public bool HasEditConfigRole(string gatewayName, string tokenId)
        {
            var username = TokenManager.GetToken(tokenId, Context.Request.UserHostAddress).Login;
            using (var ctx = new CaesarContext())
            {
                var user = ctx.Users.First(row => row.Username == username);
                return (user.Roles.Any(row => row.RoleType.Name == "Administrator")
                    || user.Roles.Any(row => row.RoleType.Name == "Super Configurator")
                    || user.Roles.Any(row => row.RoleType.Name == "Configurator" && row.ParamValue1 == gatewayName));
            }
        }

        [WebMethod]
        public bool HasRestartRole(string gatewayName, string tokenId)
        {
            var username = TokenManager.GetToken(tokenId, Context.Request.UserHostAddress).Login;
            using (var ctx = new CaesarContext())
            {
                var user = ctx.Users.First(row => row.Username == username);
                return (user.Roles.Any(row => row.RoleType.Name == "Administrator")
                    || user.Roles.Any(row => row.RoleType.Name == "Super Configurator")
                    || user.Roles.Any(row => row.RoleType.Name == "Piquet")
                    || user.Roles.Any(row => row.RoleType.Name == "Restarter" && row.ParamValue1 == gatewayName)
                    || user.Roles.Any(row => row.RoleType.Name == "Configurator" && row.ParamValue1 == gatewayName));
            }
        }

        [WebMethod]
        public bool HasAdminRole(string tokenId)
        {
            var username = TokenManager.GetToken(tokenId, Context.Request.UserHostAddress).Login;
            using (var ctx = new CaesarContext())
            {
                return ctx.Users.First(row => row.Username == username)
                    .Roles.Any(row => row.RoleType.Name == "Administrator");
            }
        }
    }
}