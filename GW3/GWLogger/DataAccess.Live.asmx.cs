﻿using GraphAnomalies.Types;
using GWLogger.Live;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess : WebService
    {
        [WebMethod]
        public string JsHash()
        {
            using (HashAlgorithm algorithm = SHA256.Create())
            {
                return string.Join("", algorithm.ComputeHash(Encoding.UTF8.GetBytes(File.ReadAllText(Context.Server.MapPath("~/main.js"))
                    + File.ReadAllText(Context.Server.MapPath("~/index.html"))
                    + File.ReadAllText(Context.Server.MapPath("~/Less/main.css"))
                    + Newtonsoft.Json.JsonConvert.SerializeObject((new DataAccess()).GatewaysToDisplay())))
                    .Select(c => c.ToString("X2")));
            }
        }

        [WebMethod]
        public List<GatewayToDisplay> GatewaysToDisplay()
        {
            using (var ctx = new Model.CaesarContext())
            {
                var inDebugger = (System.Diagnostics.Debugger.IsAttached);
                return ctx.Gateways.Select(row => new GatewayToDisplay
                {
                    Name = row.GatewayName,
                    IsMain = row.IsMain
                }).OrderBy(row => row.Name)
                .Where(row => inDebugger == true || row.Name != "PBGW")
                .ToList();
            }
        }

        [WebMethod]
        public List<GatewayShortInformation> GetShortInformation()
        {
            return Global.LiveInformation.GetShortInformation();
        }

        [WebMethod]
        public GatewayInformation GetGatewayInformation(string gatewayName)
        {
            return Global.LiveInformation.GetGatewayInformation(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> CpuHistory(string gatewayName)
        {
            return Global.LiveInformation.CpuHistory(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> SearchHistory(string gatewayName)
        {
            return Global.LiveInformation.SearchHistory(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> PVsHistory(string gatewayName)
        {
            return Global.LiveInformation.PVsHistory(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> NetworkHistory(string gatewayName)
        {
            return Global.LiveInformation.NetworkHistory(gatewayName);
        }

        [WebMethod]
        public GatewayHistoricData GetHistoricData(string gatewayName)
        {
            return new GatewayHistoricData()
            {
                CPU = CpuHistory(gatewayName),
                Searches = SearchHistory(gatewayName),
                PVs = PVsHistory(gatewayName),
                Network = NetworkHistory(gatewayName),
            };
        }
    }
}