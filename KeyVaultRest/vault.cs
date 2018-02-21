using System;
using System.Threading.Tasks;
using Microsoft.Azure;
using System.Net.Http;
using keyvault;
using Newtonsoft.Json.Linq;
using HttpClientSample;
using System.Collections.Generic;

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
                await PurgeDeletedVault(client);
            }
        }

        //Purge a deleted vault permanently
        public static async Task PurgeDeletedVault(HttpClient client)
        {
            string url = $"/subscriptions/{Subscription}/providers/Microsoft.KeyVault/locations/{Location}/deletedVaults/{vaultName}/purge?api-version=2016-10-01";
            using (var httpResponse = await client.PostAsync(url, null))
            {
                httpResponse.EnsureSuccessStatusCode();
                JObject responseContent = JObject.Parse(await httpResponse.Content.ReadAsStringAsync());
                string ms = responseContent.ToString();
            }
        }

        //Update access policy of a vault
        public static async Task UpdateVaultPolicy(HttpClient client)
        {
            string url = $"/subscriptions/{Subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.KeyVault/vaults/{vaultName}/accessPolicies/add?api-version=2016-10-01";
            var requestBody = new
            {
                properties = new
                {
                    accessPolicies = new object[]
                        {
                            new
                            {
                                tenantId = tenantId,
                                objectId = clientId,
                                permissions =
                                    new
                                         {
                                             keys = new string[] { "encrypt", "decrypt","create" },
                                             secrets = new string[] { "get", "list", "delete" }
                                          },
                            }
                        },
                }
            };
            using (var httpResponse = await client.PutAsJsonAsync(url, requestBody))
            {
                httpResponse.EnsureSuccessStatusCode();
                JObject responseContent = JObject.Parse(await httpResponse.Content.ReadAsStringAsync());
                string ms = responseContent.ToString();
            }
        }

        //Get a deleted vault which has soft-delete feature enabled
        public static async Task GetDeletedVault(HttpClient client)
        {
            string url = $"/subscriptions/{Subscription}/providers/Microsoft.KeyVault/locations/{Location}/deletedVaults/{vaultName}?api-version=2016-10-01";
            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                Console.WriteLine(JObject.Parse(await response.Content.ReadAsStringAsync()));
            }
        }

        //List all deleted vaults which have soft-delete feature enabled
        public static async Task ListDeletedVault(HttpClient client)
        {
            string url = $"/subscriptions/{Subscription}/providers/Microsoft.KeyVault/deletedVaults?api-version=2016-10-01";
            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                Console.WriteLine(JObject.Parse(await response.Content.ReadAsStringAsync()));
            }
        }

        //Delete a vault
        public static async Task DeleteVault(HttpClient client)
        {
            string url = $"/subscriptions/{Subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.KeyVault/vaults/{vaultName}?api-version=2016-10-01";
            using (var response = await client.DeleteAsync(url))
            {
                response.EnsureSuccessStatusCode();
                Console.WriteLine(JObject.Parse(await response.Content.ReadAsStringAsync()));
            }
        }

        //Get info of a vault
        public static async Task GetVault(HttpClient client)
        {
            string url = $"/subscriptions/{Subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.KeyVault/vaults/{vaultName}?api-version=2016-10-01";
            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                JObject responseContent = JObject.Parse(await response.Content.ReadAsStringAsync());
                string ms = responseContent.ToString();
            }
        }

        //List all vaults
        public static async Task ListVault(HttpClient client)
        {
            string Subscription = CloudConfigurationManager.GetSetting("AzureSubscriptionId");
            string url = $"/subscriptions/{Subscription}/resources?$filter=resourceType eq 'Microsoft.KeyVault/vaults'&api-version=2018-01-01";
            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                Console.WriteLine(JObject.Parse(await response.Content.ReadAsStringAsync()));
            }
        }

        //Check vault name availability
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

        //Create a new vault
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
                        enabledForDeployment = true,
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
