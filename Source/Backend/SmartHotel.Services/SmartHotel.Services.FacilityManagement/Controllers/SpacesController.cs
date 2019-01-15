using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SmartHotel.Services.FacilityManagement.Models;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
	[Route( "api/spaces" )]
	[ApiController]
	public class SpacesController : ControllerBase
	{
		private readonly ITopologyClient _client;
		private readonly AuthFlow _authFlow;

		public SpacesController( ITopologyClient client, AuthFlow authFlow )
		{
			_client = client;
			_authFlow = authFlow;
		}

		// GET: api/spaces
		[HttpGet]
		public async Task<IActionResult> Get()
		{
			try
			{
				UpdateAccessToken();

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
				UpdateAccessToken();

				var spacesWithTemperatureAlerts = await _client.GetRoomSpaceTemperatureAlerts();

				return Ok( spacesWithTemperatureAlerts );
			}
			catch ( Exception e )
			{
				return StatusCode( 500, e.Message );
			}
		}

		private void UpdateAccessToken()
		{
			string accessToken;
			if ( _authFlow.UseAdalAuthFlow )
			{
				accessToken = HttpContext.Request.Headers["azure_token"];
			}
			else
			{
				//TODO: Get the Digital Twins access token for the AAD Application
				accessToken = string.Empty;
			}

			_client.AccessToken = accessToken;
		}
	}
}
