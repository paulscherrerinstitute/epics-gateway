using System;
using System.Collections.Generic; 
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess
    {

        [WebMethod]
        public List<Live.GraphAnomaly> GetGraphAnomalies()
        {
            return Global.LiveInformation.GetGraphAnomalies();
        }

        [WebMethod]
        public void DeleteAnomaly(string name)
        {
            throw new Exception();
        }

    }
}