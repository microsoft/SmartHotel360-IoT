using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using SmartHotel.Services.FacilityManagement.Models;

namespace SmartHotel.Services.FacilityManagement
{
	public class SimpleFlowAuthorizationFilter : IAuthorizationFilter
	{
		private readonly string _apiKey;

		public const string ApiKeyHeader = "X-API-KEY";

		public SimpleFlowAuthorizationFilter( SimpleAuthOptions simpleAuthOptions )
		{
			_apiKey = simpleAuthOptions.ApiKey;
		}

		public void OnAuthorization( AuthorizationFilterContext context )
		{
			StringValues apiKey = context.HttpContext.Request.Headers[ApiKeyHeader];

			if ( apiKey.Any() )
			{
				if ( apiKey[0] != _apiKey )
				{
					context.Result = new UnauthorizedResult();
				}
			}
			else
			{
				context.Result = new UnauthorizedResult();
			}
		}
	}
}
