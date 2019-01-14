using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
	[Route( "api/spaces" )]
	[ApiController]
	public class SpacesController : ControllerBase
	{
		private readonly ITopologyClient _client;

		public SpacesController( ITopologyClient client )
		{
			_client = client;
		}

		// GET: api/spaces
		[HttpGet]
		public async Task<IActionResult> Get()
		{
			_client.AccessToken = HttpContext.Request.Headers["azure_token"];

			try
			{
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
			_client.AccessToken = HttpContext.Request.Headers["azure_token"];

			try
			{
				var spacesWithTemperatureAlerts = await _client.GetRoomSpaceTemperatureAlerts();

				return Ok( spacesWithTemperatureAlerts );
			}
			catch ( Exception e )
			{
				return StatusCode( 500, e.Message );
			}
		}
	}
}
