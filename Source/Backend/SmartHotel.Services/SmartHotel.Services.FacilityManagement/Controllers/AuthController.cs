using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using SmartHotel.Services.FacilityManagement.Models;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
	[Route( "api/auth" )]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly SimpleAuthOptions _simpleAuthOptions;

		public AuthController( IOptions<SimpleAuthOptions> simpleAuthOptions )
		{
			_simpleAuthOptions = simpleAuthOptions.Value;
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetDTToken()
		{
			try
			{
				var authContext = new AuthenticationContext( $"{_simpleAuthOptions.Authority}{_simpleAuthOptions.TenantId}" );
				AuthenticationResult result = await authContext.AcquireTokenAsync( "0b07f429-9f4b-4714-9392-cc5e8e80c8b0",
					new ClientCredential( _simpleAuthOptions.ApplicationId, _simpleAuthOptions.ApplicationSecret ) );

				return Ok( result.AccessToken );
			}
			catch ( Exception e )
			{
				return StatusCode( 500, e.Message );
			}
		}
	}
}