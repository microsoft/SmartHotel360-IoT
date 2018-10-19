using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHotel.Services.FacilityManagement.Models;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
    [Route("api/spaces")]
    [ApiController]
    public class SpacesController : ControllerBase
    {
        private ITopologyClient _client;

        public SpacesController(ITopologyClient client)
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
				var hotels = await _client.GetHotels();

				return Ok(hotels);
			}
			catch ( Exception e)
			{
				return StatusCode( 500, e.Message );
			}
        }
    }
}
