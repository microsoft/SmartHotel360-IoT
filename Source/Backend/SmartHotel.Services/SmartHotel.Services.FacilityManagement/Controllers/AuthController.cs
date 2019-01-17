using System;
using Microsoft.AspNetCore.Mvc;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
	[Route( "api/auth" )]
	[ApiController]
	public class AuthController : ControllerBase
	{
		[HttpPost]
		public IActionResult Post()
		{
			try
			{
				return Ok();
			}
			catch ( Exception e )
			{
				return StatusCode( 500, e.Message );
			}
		}
	}
}