using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Web;

namespace GWLogger.AuthAccess
{
    public class LdapHelper
    {
        /// <summary>
        /// The DNS host name (or IP address) of the LDAP server to connect to.
        /// </summary>
        static public string LdapServer { get; set; } = System.Configuration.ConfigurationManager.AppSettings["ldapServer"] ?? "d.psi.ch";

        /// <summary>
        /// Milliseconds of delay after a login failure. This helps slow down brute force attacks.
        /// </summary>
        static public int DelayOnLoginFailure { get; set; } = int.Parse(System.Configuration.ConfigurationManager.AppSettings["AuthLdapServerMsecDelayOnFailure"] ?? "10");

        /// <summary>
        /// Base DN of the LDAP search root. <seealso cref="DnsNameToLdapDn"/>
        /// </summary>
        static public string RootDn => DnsNameToLdapDn(LdapServer);

        public static string DnsNameToLdapDn(string dnsName)
        {
            return string.Join(",", dnsName.Split('.').Select(s => "dc=" + s).ToList());
        }

        static public string GetUserEmail(string userName, string password)
        {
            try
            {
                using (var ldap = new LdapConnection(LdapServer))
                {
                    string filter;
                    if (userName.Contains("@"))
                        filter = string.Format("(&(objectClass=user)(objectCategory=person)(mail={0}))", userName);
                    else
                        filter = string.Format("(&(objectClass=user)(objectCategory=person)(samAccountName={0}))", userName);
                    var searchAttributes = new string[] { "samAccountName", "mail", "givenName", "sn" };
                    var request = new SearchRequest(RootDn, filter, System.DirectoryServices.Protocols.SearchScope.Subtree, searchAttributes);
                    /*if (!string.IsNullOrWhiteSpace(SearchUser))
                    {*/
                    var credentials = new NetworkCredential(userName, password);
                    ldap.Bind(credentials);
                    //}
                    var response = (SearchResponse)ldap.SendRequest(request);
                    if (response.Entries.Count == 0)
                        return null;
                    var attrs = response.Entries[0].Attributes;
                    return attrs.Contains("mail") ? (string)attrs["mail"][0] : "";
                }
            }
            catch (Exception ex)
            {
                throw ex;
                //return null;
            }
        }
    }
}