using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WRKJRNL01.Data.Services
{
    /*
     * 
     Please use this instead: https://9229apps.visualstudio.com/ArtifactLibrary/_git/nuget-sdpbc-core-packages?path=/SDPBC.Svc.Common/ServiceConnectors/ServiceConnectorBase.cs
     See here for a working example: https://9229apps.visualstudio.com/Main/_git/EmployeesSvc?path=/SDPBC.Employees/SDPBC.Employees.ServiceConnector/EmployeeServiceConnector.cs
     *
     */
    public class WebServiceClient
    {
        private readonly HttpClient client = new HttpClient();

        public WebServiceClient(HttpClient httpClient)
        {
            client = httpClient;
        }

        public async Task<string> SendHttpRequestAsync(string url, string method = "GET", string postData = null)
        {
            HttpResponseMessage response;

            if (method.ToUpper() == "POST")
            {
                var content = new StringContent(postData ?? "", Encoding.UTF8, "application/json");
                response = await client.PostAsync(url, content);
            }
            else // Default to GET
            {
                response = await client.GetAsync(url);
            }

            response.EnsureSuccessStatusCode(); // Throws if not 2xx

            string json = await response.Content.ReadAsStringAsync();
            return json;
        }
    }
}
