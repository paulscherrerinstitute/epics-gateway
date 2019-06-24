using GWLogger.Backend;
using GWLogger.Backend.Controllers;
using GWLogger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Services;
using System.Xml.Serialization;

namespace GWLogger
{
    public partial class DataAccess : WebService
    {
        [WebMethod]
        public void ImportInventoryConfiguration(string hostname)
        {
            ConfigController.ImportInventoryConfiguration(hostname);
        }

        [WebMethod]
        public void ImportAllInventoryConfiguration()
        {
            ConfigController.ImportAllConfigurations();
        }

        [WebMethod]
        public XmlGatewayConfig GetGatewayConfiguration(string hostname)
        {
            return ConfigController.GetConfiguration(hostname);
        }
    }
}