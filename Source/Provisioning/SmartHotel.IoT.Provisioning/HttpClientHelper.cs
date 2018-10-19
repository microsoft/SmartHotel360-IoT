using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace SmartHotel.IoT.Provisioning
{
    public static class HttpClientHelper
    {
	    public static async Task<HttpClient> GetHttpClientAsync(string digitalTwinsApiEndpoint, string aadInstance,
		    string tenant, string digitalTwinsResourceId, string clientId, string clientSecret)
	    {
		    string accessToken =
			    await AuthenticateAndGetAccessTokenAsync(aadInstance, tenant, digitalTwinsResourceId, clientId, clientSecret);
		    if (string.IsNullOrWhiteSpace(accessToken))
		    {
			    return null;
		    }

		    string baseAddress = digitalTwinsApiEndpoint;
		    if (!baseAddress.EndsWith('/'))
		    {
			    baseAddress = $"{baseAddress}/";
		    }

		    var handler = new TimeoutHandler
		    {
				DefaultTimeout = TimeSpan.FromSeconds(180),
			    InnerHandler = new HttpClientHandler()
		    };

		    var httpClient = new HttpClient(handler)
		    {
			    BaseAddress = new Uri(baseAddress),
				Timeout = Timeout.InfiniteTimeSpan
		    };
		    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
		    return httpClient;
	    }

	    public static async Task<string> AuthenticateAndGetAccessTokenAsync(string aadInstance, string tenant,
		    string digitalTwinsResourceId, string clientId, string clientSecret)
	    {
		    var authContext = new AuthenticationContext($"{aadInstance}{tenant}");
		    try
		    {
			    AuthenticationResult result = await authContext.AcquireTokenAsync(digitalTwinsResourceId,
				    new ClientCredential(clientId, clientSecret));
			    return result.AccessToken;
		    }
		    catch (AdalServiceException ex)
		    {
			    Console.WriteLine($"Error Authenticating: {ex}");
			    return null;
		    }
	    }
    }
}
