using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace PrimaRESTExampleClient
{
    public class ApiConfig
    {
        public string ServerUrl { get; set; }

        public string AuthenticationUrl { get; set; }

        public CredentialsConfig Credentials { get; set; }
    }

    public class CredentialsConfig
    {
        public string Scope { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            //read configuration strings from appsettings.json file
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true)
                                                   .Build();

            var apiConfigSection = config.GetSection("ApiConfig");
            var apiConfig = new ApiConfig();
            apiConfigSection.Bind(apiConfig);

            var authenticationUri = new Uri(apiConfig.AuthenticationUrl);

            //use client_credentials grant type to retrieve a token
            //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer"}
            Console.WriteLine($"Using client credentials (Client Id: {apiConfig.Credentials.ClientId}) to retrieve an access token...");
            var clientCredentialsTokenResponse = await GetTokenResponseAsync(authenticationUri,
                                                                             new Dictionary<string, string>
                                                                             {
                                                                                 {"grant_type", "client_credentials"},
                                                                                 {"scope", "api_read_all"},
                                                                                 {"client_id", apiConfig.Credentials.ClientId},
                                                                                 {"client_secret", apiConfig.Credentials.ClientSecret}
                                                                             });
            Console.WriteLine($"Response: {clientCredentialsTokenResponse}");
            Console.WriteLine();

            //use password grant type to retrieve a token
            //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer"}
            Console.WriteLine($"Using client credentials and resource owner password (Client Id: {apiConfig.Credentials.ClientId}, User: {apiConfig.Credentials.Username}) to retrieve an access token...");
            var resourceOwnerPasswordTokenResponse = await GetTokenResponseAsync(authenticationUri,
                                                                                 new Dictionary<string, string>
                                                                                 {
                                                                                     {"grant_type", "password"},
                                                                                     {"scope", "api_read_all"},
                                                                                     {"client_id", apiConfig.Credentials.ClientId},
                                                                                     {"client_secret", apiConfig.Credentials.ClientSecret},
                                                                                     {"username", apiConfig.Credentials.Username},
                                                                                     {"password", apiConfig.Credentials.Password}
                                                                                 });
            Console.WriteLine($"Response: {resourceOwnerPasswordTokenResponse}");
            Console.WriteLine();

            //use password grant type with offline_access scope to retrieve a token and a refresh token
            //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer","refresh_token":"<refresh_token>"}
            Console.WriteLine($"Using client credentials and resource owner password (Client Id: {apiConfig.Credentials.ClientId}, User: {apiConfig.Credentials.Username}) to retrieve an access token AND refresh token...");
            var getRefreshTokenResponse = await GetTokenResponseAsync(authenticationUri,
                                                                      new Dictionary<string, string>
                                                                      {
                                                                          {"grant_type", "password"},
                                                                          {"scope", "api_read_all offline_access"},
                                                                          {"client_id", apiConfig.Credentials.ClientId},
                                                                          {"client_secret", apiConfig.Credentials.ClientSecret},
                                                                          {"username", apiConfig.Credentials.Username},
                                                                          {"password", apiConfig.Credentials.Password}
                                                                      });
            Console.WriteLine($"Response: {getRefreshTokenResponse}");

            //parse previous response to get refresh token for next call
            string refreshToken = null;
            try
            {
                var jsonObject = JObject.Parse(getRefreshTokenResponse);
                refreshToken = jsonObject["refresh_token"]
                    .Value<string>();
            }
            catch
            {
                Console.WriteLine("Failed to parse token response to retrieve refresh token.");
            }

            if (!string.IsNullOrEmpty(refreshToken))
            {
                //use refresh token grant type to retrieve new access token/refresh token if access token expires
                //Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer"}
                Console.WriteLine($"Using refresh token ({refreshToken}) to retrieve an access token and refresh token...");
                var refreshTokenResponse = await GetTokenResponseAsync(authenticationUri,
                                                                       new Dictionary<string, string>
                                                                       {
                                                                           {"grant_type", "refresh_token"},
                                                                           {"client_id", apiConfig.Credentials.ClientId},
                                                                           {"client_secret", apiConfig.Credentials.ClientSecret},
                                                                           {"refresh_token", refreshToken}
                                                                       });
                Console.WriteLine($"Response: {refreshTokenResponse}");
                Console.WriteLine();
            }
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
    }
}