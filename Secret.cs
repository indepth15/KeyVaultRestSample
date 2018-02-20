using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace KeyVaultRestSecret
{
    class Program
    {

        static string tenantId = CloudConfigurationManager.GetSetting("AzureTenantId");
        static string clientId = CloudConfigurationManager.GetSetting("AzureClientId");
        static string clientSecret = CloudConfigurationManager.GetSetting("AzureClientSecret");
        static string vaultName = CloudConfigurationManager.GetSetting("vaultName");
        static string secretName = CloudConfigurationManager.GetSetting("secretName");
        static void Main(string[] args)
        {
            try
            {
                MainAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetBaseException().Message);
            }
        }

        public static async Task MainAsync()
        {
            string token = await AuthHelper.AcquireTokenBySPN(tenantId, clientId, clientSecret);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.BaseAddress = new Uri("https://" + vaultName + ".vault.azure.net");
                await SetSecret(client);
            }
        }

        public static async Task<List<string>> GetSecret(HttpClient client)
        {
            string url = $"/secrets?api-version=2016-10-01";
            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                JObject responseContent = JObject.Parse(await response.Content.ReadAsStringAsync());
                List<string> secretList = new List<string>(responseContent.SelectTokens("value[*].id").Select(s => (string)s).ToList());
                return secretList;
            }
        }

        public static async Task SetSecret(HttpClient client)
        {
            var sc = await GetSecret(client);
            string url01 = $"/secrets/{secretName}?api-version=2016-10-01";
            if (sc.Any(("https://" + vaultName + ".vault.azure.net/" + "secrets/" + secretName).Contains))
            {
                Console.WriteLine("Given secret name exists");
            }
                else
                {
                    var requestBody = new
                    {
                        value = "DefaultEndpointsProtocol=https;AccountName=thisisstorage;AccountKey=p5geb9seXkyWDrjd1V3xhKy4DMap4dQL/0bwY/AAuOY2K8oVKLPf1tPexSinFokIIXbGgntMA==;EndpointSuffix=core.windows.net",
                        contentType = "text/plain",
                        attributes = new
                        {
                            nbf = 1519084800,
                            exp = 1740009600,
                            enable = true,
                        },
                };
                using (var httpResponse = await client.PutAsJsonAsync(url01, requestBody))
                {
                    httpResponse.EnsureSuccessStatusCode();
                    Console.WriteLine(JObject.Parse(await httpResponse.Content.ReadAsStringAsync()));
                }
            }
        }
    }
}
