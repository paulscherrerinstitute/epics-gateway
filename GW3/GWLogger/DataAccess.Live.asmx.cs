using GWLogger.Live;
using System.Collections.Generic;
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess : System.Web.Services.WebService
    {
        [WebMethod]
        public List<GatewayShortInformation> GetShortInformation()
        {
            return Global.LiveInformation.GetShortInformation();
        }
    }
}