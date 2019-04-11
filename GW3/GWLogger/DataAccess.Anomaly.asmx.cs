using System;
using System.Collections.Generic; 
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess
    {

        [WebMethod]
        public List<Live.GraphAnomalyInfo> GetGraphAnomalies()
        {
            return Global.LiveInformation.GetGraphAnomalies();
        }

        [WebMethod]
        public Live.GraphAnomaly GetGraphAnomaly(string filename)
        {
            return Global.LiveInformation.GetGraphAnomaly(filename);
        }

        [WebMethod]
        public void DeleteGraphAnomaly(string filename)
        {
            Global.LiveInformation.DeleteGraphAnomaly(filename);
        }

    }
}