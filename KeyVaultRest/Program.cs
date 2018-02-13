using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using System.Net.Http;
using keyvault;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using HttpClientSample;

namespace KeyVaultRest
{
    class Program
    {
        const string resourceGroup = "s15digital-rg";
        const string Location = "southeastasia";
        static string Subscription = CloudConfigurationManager.GetSetting("AzureSubscriptionId");
        static string vaultName = CloudConfigurationManager.GetSetting("vaultName");
        static string tenantId = CloudConfigurationManager.GetSetting("AzureTenantId");
        static string clientId = CloudConfigurationManager.GetSetting("AzureClientId");
        static string clientSecret = CloudConfigurationManager.GetSetting("AzureClientSecret");
        static void Main(string[] args)
        {
            try
            {
                MainASync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetBaseException().Message);
            }
        }

        public static async Task MainASync()
        {
            string token = await AuthHelper.AcquireTokenBySPN(tenantId, clientId, clientSecret);
            using (var client = new HttpClient(new LoggingHandler(new HttpClientHandler())))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.BaseAddress = new Uri("https://management.azure.com");
                await CreateVault(client);
            }
        }

        public static async Task<string> CheckVaultName(HttpClient client)
        {
            string url = $"/subscriptions/{Subscription}/providers/Microsoft.KeyVault/checkNameAvailability?api-version=2016-10-01";
            Dictionary<string, string> requestBody = new Dictionary<string, string>();
            requestBody.Add("Name", vaultName);
            requestBody.Add("Type", "Microsoft.KeyVault/vaults");
            FormUrlEncodedContent httpContent = new FormUrlEncodedContent(requestBody);
            using (var response = await client.PostAsync(url, httpContent))
            {
                response.EnsureSuccessStatusCode();
                JObject responseContent = JObject.Parse(await response.Content.ReadAsStringAsync());
                string avai = responseContent["nameAvailable"].ToString();
                return avai;
            }
        }
        static async Task CreateVault(HttpClient client)
        {
            string url01 = $"/subscriptions/{Subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.KeyVault/vaults/{vaultName}?api-version=2016-10-01";
            var availability = await CheckVaultName(client);
            if (availability == "True")
            {


                var requestBody = new
                {
                    location = Location,
                    properties = new
                    {
                        tenantId = tenantId,
                        sku = new
                        {
                            family = "A",
                            name = "standard",
                        },
                        accessPolicies = new object[]
                        {
                            new
                            {
                                tenantId = tenantId,
                                objectId = clientId,
                                permissions =
                                    new
                                         {
                                             keys = new string[] { "encrypt", "decrypt" },
                                             secrets = new string[] { "get", "list" }
                                          },
                            }
                        },
                        enabledForDeployment =  true,
                        enabledForDiskEncryption = true,
                        enabledForTemplateDeployment = true
                    }
                };

                using (var httpResponse = await client.PutAsJsonAsync(url01, requestBody))
                {
                    httpResponse.EnsureSuccessStatusCode();
                    Console.WriteLine(httpResponse.Content.ReadAsStringAsync());
                }
            }
        }
    }
}





//var requestBodyj = new JObject(
//new JProperty("location", tenantId),
//new JProperty("properties",
//                new JProperty("tenantId", tenantId),
//                new JProperty("sku",
//                new JProperty("family", "A"),
//                new JProperty("name", "standard")),
//    new JProperty("accessPolicies",
//        new JProperty("tenantId", tenantId),
//        new JProperty("objectId", tenantId),
//        new JProperty("permission",
//                new JProperty("key",
//                    new JArray("encrypt", "decrypt", "sign")))),
//        new JProperty("enabledForDeployment", true),
//        new JProperty("enabledForDiskEncryption", true),
//        new JProperty("enabledForTemplateDeployment", true)));

//var stringPayload = JsonConvert.SerializeObject(requestBodyj);
//var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");

//    var responseJ = await client.PutAsJsonAsync($"/subscriptions/{Subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.KeyVault/vaults/thuanvault?api-version=2016-10-01",req);
//        responseJ.EnsureSuccessStatusCode();

//    }
//    }
//}
//using (var response = await client.PutAsJsonAsync(
//    $"/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/Microsoft.Web/sites/{WebApp}/config/appsettings?api-version=2016-03-01",
//    new
//    {
//        properties = new
//        {
//            Foo = "Hello",
//            Bar = "Bye",
//        }
//    }))
//public static async Task CreateVault(HttpClient client)
//{
//    string url = $"/subscriptions/{Subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.KeyVault/vaults/thuanvault?api-version=2016-10-01";
//    var availability = await MainAsync();
//    //if (availability == "true")
//    //{
//    //    var requestBodyj = new JObject(
//    //        new JProperty("location", tenantId),
//    //        new JProperty("properties",
//    //                            new JProperty("tenantId", tenantId),
//    //                            new JProperty("sku",
//    //                                new JProperty("family", "A"),
//    //                                new JProperty("name", "standard")),
//    //        new JProperty("accessPolicies",
//    //            new JProperty("tenantId", tenantId),
//    //            new JProperty("objectId", tenantId),
//    //            new JProperty("permission",
//    //                    new JProperty("key",
//    //                        new JArray("encrypt", "decrypt", "sign")))),
//    //            new JProperty("enabledForDeployment", true),
//    //            new JProperty("enabledForDiskEncryption", true),
//    //            new JProperty("enabledForTemplateDeployment", true)));

//    //    //JObject parent = JObject.Parse(requestBodyj);
//    //    using (var responseJ = await client.PutAsJsonAsync(url, requestBodyj))
//    //    {
//    //        responseJ.EnsureSuccessStatusCode();
//    //        JObject responseContent = JObject.Parse(await responseJ.Content.ReadAsStringAsync());
//    //    }
//    }
//}