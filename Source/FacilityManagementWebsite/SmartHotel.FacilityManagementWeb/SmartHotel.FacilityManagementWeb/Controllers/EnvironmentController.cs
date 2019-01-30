using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SmartHotel.FacilityManagementWeb.Models;

namespace SmartHotel.FacilityManagementWeb.Controllers
{
	[Route( "config/[controller]" )]
	[ApiController]
	public class EnvironmentController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly AdalConfig _adalConfig;

		private static readonly string[] DesiredEnvironmentVariableKeys = { "apiEndpoint", "azureMapsKey", "tsiFqdn", "tsiHowManyDays" };
		private static readonly string[] OptionalEnvironmentVariableKeys = { "useBasicAuth" };

		public EnvironmentController( IConfiguration config, IOptions<AdalConfig> adalConfig )
		{
			_config = config;
			_adalConfig = adalConfig.Value;
		}

		[HttpGet]
		public IDictionary<string, string> EnvironmentVariables()
		{
			var environmentVariables = new Dictionary<string, string>();
			environmentVariables.Add( "adalConfig", JsonConvert.SerializeObject( _adalConfig ) );
			foreach ( string key in DesiredEnvironmentVariableKeys )
			{
				environmentVariables.Add( key, _config[key] );
			}

			foreach ( string key in OptionalEnvironmentVariableKeys )
			{
				string value = _config[key];
				if (!string.IsNullOrWhiteSpace(value))
				{
					environmentVariables.Add( key, value );
				}
			}

			return environmentVariables;
		}
	}
}