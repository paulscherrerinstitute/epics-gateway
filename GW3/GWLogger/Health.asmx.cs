using System.ComponentModel;
using System.Web.Services;

namespace GWLogger
{
    [WebService(Namespace = "http://gwlogger.psi.ch/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class Health : WebService
    {
        [WebMethod]
        public HealthResponse IsHealthy()
        {
            // Todo: Add code that verfies if the app is running correctly
            return new HealthResponse() {
                IsHealthy = true,
                Message = "",
            };
        }
    }

    public class HealthResponse
    {
        /// <summary>
        /// Flag indicating if the application is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Message to specify a reason if the application is unhealthy
        /// </summary>
        public string Message { get; set; }
    }
}