using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using SmartHotel.Services.FacilityManagement.Models;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
	[Route( "api/spaces" )]
	[ApiController]
	public class SpacesController : ControllerBase
	{
		private readonly ITopologyClient _client;
		private readonly AuthFlow _authFlow;
		private readonly SimpleAuthOptions _simpleAuthOptions;

		public SpacesController( ITopologyClient client, AuthFlow authFlow, IOptions<SimpleAuthOptions> simpleAuthOptions )
		{
			_client = client;
			_authFlow = authFlow;
			_simpleAuthOptions = simpleAuthOptions.Value;
		}

		// GET: api/spaces
		[HttpGet]
		public async Task<IActionResult> Get()
		{
			try
			{
				await UpdateAccessTokenAsync();

				var hotels = await _client.GetSpaces();

				return Ok( hotels );
			}
			catch ( Exception e )
			{
				return StatusCode( 500, e.Message );
			}
		}

		[HttpGet( "[action]" )]
		public async Task<IActionResult> TemperatureAlerts()
		{
			try
			{
				await UpdateAccessTokenAsync();

				var spacesWithTemperatureAlerts = await _client.GetRoomSpaceTemperatureAlerts();

				return Ok( spacesWithTemperatureAlerts );
			}
			catch ( Exception e )
			{
				return StatusCode( 500, e.Message );
			}
		}

		private async Task UpdateAccessTokenAsync()
		{
			string accessToken;
			if ( _authFlow.UseAdalAuthFlow )
			{
				accessToken = HttpContext.Request.Headers["azure_token"];
			}
			else
			{
				var authContext = new AuthenticationContext( $"{_simpleAuthOptions.Authority}{_simpleAuthOptions.TenantId}" );
				AuthenticationResult result = await authContext.AcquireTokenAsync( "0b07f429-9f4b-4714-9392-cc5e8e80c8b0",
					new ClientCredential( _simpleAuthOptions.ApplicationId, _simpleAuthOptions.ApplicationSecret ) );
				accessToken = result.AccessToken;
			}

			_client.AccessToken = accessToken;
		}
	}
}
