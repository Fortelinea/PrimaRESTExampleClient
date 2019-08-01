using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace PrimaRESTExampleClient
{
    public class ApiConfig
    {
        public string AuthenticationUrl { get; set; }

        public CredentialsConfig Credentials { get; set; }

        public string ServerUrl { get; set; }
    }

    public class CredentialsConfig
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Password { get; set; }

        public string Username { get; set; }
    }

    internal class Program
    {
        private static (string, string) GetAccessTokenAndTypeFromTokenResponse(string tokenResponse)
        {
            string tokenType = null;
            string accessToken = null;
            if (string.IsNullOrEmpty(tokenResponse)) return (null, null);

            try
            {
                var jsonObject = JObject.Parse(tokenResponse);
                tokenType = jsonObject["token_type"]
                    .Value<string>();
                accessToken = jsonObject["access_token"]
                    .Value<string>();
            }
            catch
            {
                Console.WriteLine("Failed to parse token response to retrieve access token.");
            }

            return (accessToken, tokenType);
        }

        private static Task<string> GetCaseByIdAsync(Uri serverBaseUri, int id, string tokenType, string accessToken)
        {
            var apiUri = new Uri(serverBaseUri, "api/v1/");

            var getCaseByIdUri = new Uri(apiUri, $"Case({id})");

            Console.WriteLine($"Making a GET request for case information for a case with id {id} (Request URL: {getCaseByIdUri})...");

            return MakeAuthenticatedGetRequest(getCaseByIdUri, tokenType, accessToken);
        }

        private static Task<string> GetPagedStainTestsAsync(Uri serverBaseUri, int skip, string tokenType, string accessToken)
        {
            var apiUri = new Uri(serverBaseUri, "api/v1/");

            var getPagedStainTestsUri = new Uri(apiUri, $"StainTest?$skip={skip}");

            Console.WriteLine($"Making a GET request for page stain tests skipping the first {skip} items (Request URL: {getPagedStainTestsUri})...");

            return MakeAuthenticatedGetRequest(getPagedStainTestsUri, tokenType, accessToken);
        }

        private static string GetRefreshTokenFromRefreshTokenResponse(string getRefreshTokenResponse)
        {
            try
            {
                var jsonObject = JObject.Parse(getRefreshTokenResponse);
                return jsonObject["refresh_token"]
                    .Value<string>();
            }
            catch
            {
                Console.WriteLine("Failed to parse token response to retrieve refresh token.");
            }

            return null;
        }

        private static Task<string> GetStudyByIdAsync(Uri serverBaseUri, int id, string tokenType, string accessToken)
        {
            var apiUri = new Uri(serverBaseUri, "api/v1/");

            var getStudyByIdUri = new Uri(apiUri, $"Study({id})");

            Console.WriteLine($"Making a GET request for study information for a study with id {id} (Request URL: {getStudyByIdUri})...");

            return MakeAuthenticatedGetRequest(getStudyByIdUri, tokenType, accessToken);
        }

        private static async Task<string> GetTokenResponseAsync(Uri authenticationUrl, Dictionary<string, string> authenticationCredentials)
        {
            try
            {
                var client = new HttpClient();

                var content = new FormUrlEncodedContent(authenticationCredentials);

                var response = await client.PostAsync(authenticationUrl, content);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = $"POST failed. Received HTTP {response.StatusCode}";
                    throw new ApplicationException(message);
                }

                var responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
            catch (Exception ex)
            {
                return $"Error sending token request: {ex.Message}";
            }
        }

        private static Task<string> GetTokenResponseAsync_ClientCredentialFlow(Uri authenticationUri, string clientId, string clientSecret)
        {
            return GetTokenResponseAsync(authenticationUri,
                                         new Dictionary<string, string>
                                         {
                                             {"grant_type", "client_credentials"},
                                             {"scope", "api_read_all"},
                                             {"client_id", clientId},
                                             {"client_secret", clientSecret}
                                         });
        }

        private static Task<string> GetTokenResponseAsync_RefreshTokenFlow(Uri authenticationUri, string clientId, string clientSecret, string refreshToken)
        {
            var tokenRequestParameters = new Dictionary<string, string>
                                         {
                                             {"grant_type", "refresh_token"},
                                             {"client_id", clientId},
                                             {"client_secret", clientSecret},
                                             {"refresh_token", refreshToken}
                                         };

            return GetTokenResponseAsync(authenticationUri, tokenRequestParameters);
        }

        private static Task<string> GetTokenResponseAsync_ResourceOwnerPasswordFlow(Uri authenticationUri, string clientId, string clientSecret, string username, string password, bool requestRefreshToken = false)
        {
            var tokenRequestParameters = new Dictionary<string, string>
                                         {
                                             {"grant_type", "password"},
                                             {"scope", "api_read_all"},
                                             {"client_id", clientId},
                                             {"client_secret", clientSecret},
                                             {"username", username},
                                             {"password", password}
                                         };

            if (requestRefreshToken) tokenRequestParameters["scope"] = "api_read_all offline_access";

            return GetTokenResponseAsync(authenticationUri, tokenRequestParameters);
        }

        private static async Task Main(string[] args)
        {
            //read configuration strings from appsettings.json file
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true)
                                                   .Build();

            var apiConfigSection = config.GetSection("ApiConfig");
            var apiConfig = new ApiConfig();
            apiConfigSection.Bind(apiConfig);

            var serverBaseUri = new Uri(apiConfig.ServerUrl);
            var authenticationUri = new Uri(apiConfig.AuthenticationUrl);

            //use client_credentials grant type to retrieve a token
            //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer"}
            Console.WriteLine($"Using client credentials (Client Id: {apiConfig.Credentials.ClientId}) to retrieve an access token...");
            var clientCredentialsTokenResponse = await GetTokenResponseAsync_ClientCredentialFlow(authenticationUri, apiConfig.Credentials.ClientId, apiConfig.Credentials.ClientSecret);
            Console.WriteLine($"Response:{Environment.NewLine}{clientCredentialsTokenResponse.ToPrettyString()}");
            Console.WriteLine();

            //use password grant type to retrieve a token
            //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer"}
            Console.WriteLine($"Using client credentials and resource owner password (Client Id: {apiConfig.Credentials.ClientId}, User: {apiConfig.Credentials.Username}) to retrieve an access token...");
            var resourceOwnerPasswordTokenResponse =
                await GetTokenResponseAsync_ResourceOwnerPasswordFlow(authenticationUri, apiConfig.Credentials.ClientId, apiConfig.Credentials.ClientSecret, apiConfig.Credentials.Username, apiConfig.Credentials.Password);
            Console.WriteLine($"Response:{Environment.NewLine}{resourceOwnerPasswordTokenResponse.ToPrettyString()}");
            Console.WriteLine();

            //use password grant type with offline_access scope to retrieve a token and a refresh token
            //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer","refresh_token":"<refresh_token>"}
            Console.WriteLine($"Using client credentials and resource owner password (Client Id: {apiConfig.Credentials.ClientId}, User: {apiConfig.Credentials.Username}) to retrieve an access token AND refresh token...");
            var getRefreshTokenResponse =
                await GetTokenResponseAsync_ResourceOwnerPasswordFlow(authenticationUri, apiConfig.Credentials.ClientId, apiConfig.Credentials.ClientSecret, apiConfig.Credentials.Username, apiConfig.Credentials.Password, true);
            Console.WriteLine($"Response:{Environment.NewLine}{getRefreshTokenResponse.ToPrettyString()}");
            Console.WriteLine();

            //parse previous response to get refresh token for next call
            var refreshToken = GetRefreshTokenFromRefreshTokenResponse(getRefreshTokenResponse);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                //use refresh token grant type to retrieve new access token/refresh token if access token expires
                //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer"}
                Console.WriteLine($"Using refresh token ({refreshToken}) to retrieve an access token and refresh token...");

                var refreshTokenResponse = await GetTokenResponseAsync_RefreshTokenFlow(authenticationUri, apiConfig.Credentials.ClientId, apiConfig.Credentials.ClientSecret, refreshToken);
                Console.WriteLine($"Response:{Environment.NewLine}{refreshTokenResponse.ToPrettyString()}");
                Console.WriteLine();
            }

            //extract token type and access token from client credentials access token response
            var (accessToken, tokenType) = GetAccessTokenAndTypeFromTokenResponse(clientCredentialsTokenResponse);

            if (!string.IsNullOrEmpty(tokenType) && !string.IsNullOrEmpty(accessToken))
            {
                //use acquired access token to query for some data about slides

                //find slides by barcode content
                //var barcodeContent = "5*2U*UM9*1";
                var barcodeContent = "1>2U>3";
                var client = new PrimaRestODataClient(serverBaseUri, accessToken);
                var slide = await client.FindSlideByBarcodeContentAsync(barcodeContent);
                Console.WriteLine($"Response:{Environment.NewLine}{slide.PrimaryIdentifier}");
                Console.WriteLine();

                //get first slide from the collection
                //var slides = (JArray)JObject.Parse(getSlidesByBarcodeResponse)["value"];
                //var slide = slides[0];

                ////get slide's case id
                //var caseId = slide["caseBaseId"]
                //    .Value<int>();

                ////look up case info using slide's case id
                //var getCaseByIdResponse = await GetCaseByIdAsync(serverBaseUri, caseId, tokenType, accessToken);
                //Console.WriteLine($"Response:{Environment.NewLine}{getCaseByIdResponse.ToPrettyString()}");
                //Console.WriteLine();

                ////check case for study id, may not be present depending on case type
                //var slideCase = JObject.Parse(getCaseByIdResponse);

                //if (slideCase.TryGetValue("studyId", out var studyId))
                //{
                //    var intStudyId = studyId.Value<int?>();
                //    if (intStudyId != null)
                //    {
                //        var studyByIdResponse = await GetStudyByIdAsync(serverBaseUri, intStudyId.Value, tokenType, accessToken);
                //        Console.WriteLine($"Response:{Environment.NewLine}{studyByIdResponse.ToPrettyString()}");
                //        Console.WriteLine();
                //    }
                //}

                //get a page of the stain tests collection, skipping the first 100 items
                var stainsResponse = await GetPagedStainTestsAsync(serverBaseUri, 100, tokenType, accessToken);
                Console.WriteLine($"Response:{Environment.NewLine}{stainsResponse.ToPrettyString()}");
                Console.WriteLine();

                await TestGettingSlidesByListOfBarcodesAsync(serverBaseUri, tokenType, accessToken);
            }
        }

        private static async Task<string> MakeAuthenticatedGetRequest(Uri uri, string tokenType, string accessToken)
        {
            try
            {
                var client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, accessToken);

                var response = await client.GetAsync(uri);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = $"GET failed. Received HTTP {response.StatusCode}";
                    throw new ApplicationException(message);
                }

                var responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
            catch (Exception ex)
            {
                return $"Error sending token request: {ex.Message}";
            }
        }

        private static async Task TestGettingSlidesByListOfBarcodesAsync(Uri serverBaseUri, string tokenType, string accessToken)
        {
            try
            {
                var slideBarcodes = new[]
                                    {
                                        "2*2U*1*1",
                                        "2*2U*2*1",
                                        "2*2U*1Y*1",
                                        "1>2U>1",
                                        "1>2U>2",
                                        "1>2U>3"
                                    };

                var client = new PrimaRestODataClient(serverBaseUri, accessToken);
                var slides = await client.TranslateBarcodesToIdentifiersAsync(slideBarcodes);
                Console.WriteLine("");
                foreach (var slide in slides) Console.WriteLine($"Found Slide: {slide.PrimaryIdentifier} ({slide.AlternateIdentifier}) with barcode '{slide.BarcodeContent}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem with TranslateBarcodesToIdentifiersAsync");
                Console.WriteLine(ex);
            }
        }
    }
}