using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using SmartHotel.Services.FacilityManagement.Models;

namespace SmartHotel.Services.FacilityManagement
{
	public class BasicFlowAuthorizationFilter : IAuthorizationFilter
	{
		private readonly string _encodedUserNamePassword;

		public const string AuthorizationHeader = "Authorization";

		public BasicFlowAuthorizationFilter( BasicAuthOptions basicAuthOptions )
		{
			var plainTextBytes = Encoding.UTF8.GetBytes($"{basicAuthOptions.Username}:{basicAuthOptions.Password}");
			_encodedUserNamePassword = Convert.ToBase64String(plainTextBytes);
		}

		public void OnAuthorization( AuthorizationFilterContext context )
		{
			StringValues authorizationHeaders = context.HttpContext.Request.Headers[AuthorizationHeader];

			string authHeader = authorizationHeaders.FirstOrDefault();
			if ( !string.IsNullOrWhiteSpace( authHeader ) && authHeader.StartsWith( "Basic " ) )
			{
				var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
				if ( encodedUsernamePassword != _encodedUserNamePassword )
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
