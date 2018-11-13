using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SmartHotel.Services.FacilityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
	[Route( "api/sensordata" )]
	[ApiController]
	public class SensorDataController : ControllerBase
	{
		private IConfiguration _config;
		private MongoClient _documentClient;

		public SensorDataController( IConfiguration config )
		{
			_config = config;
			_documentClient = new MongoClient( config["MongoDBConnectionString"] );
		}

		[HttpGet]
		public IActionResult Get( [FromQuery( Name = "roomIds" )]string[] roomIds )
		{
			if ( roomIds == null || roomIds.Length == 0 )
				return NotFound();

			var sensorData = new List<SensorData>();

			try
			{
				var db = _documentClient.GetDatabase( _config["MongoDBName"] );
				var sensorDataTable = db.GetCollection<SensorData>( _config["MongoTableName"] );

				var filter = $"{{roomId: {{'$in': [{ToMongoArrayString( roomIds )}]}}}}";
				Console.WriteLine( filter );

				List<SensorData> results = sensorDataTable.Find( filter ).ToList();
				if ( results != null && results.Count > 0 )
                {
					Dictionary<string, SensorData[]> sensorDatasByRoom = results.GroupBy( sd => sd.RoomId )
						.ToDictionary( g => g.Key, g => g.ToArray() );
					foreach ( KeyValuePair<string, SensorData[]> kvp in sensorDatasByRoom )
					{
			            SensorData[] latestSensorDatas = kvp.Value
				            .GroupBy( sd => sd.SensorDataType )
				            .Select(g => g.OrderByDescending( sd => sd.EventTimestamp ).First()).ToArray();

			            sensorData.AddRange( latestSensorDatas );
					}
                }
			}
			catch ( Exception e )
			{
				System.Diagnostics.Debug.WriteLine( e );
			}

			return Ok( sensorData );
		}



		private string ToMongoArrayString( string[] arr )
		{
			string result = string.Empty;

			for ( int i = 0; i < arr.Length; i++ )
			{
				result += $"\"{arr[i]}\"";

				if ( i < arr.Length - 1 )
					result += ",";
			}

			return result;
		}
	}
}
