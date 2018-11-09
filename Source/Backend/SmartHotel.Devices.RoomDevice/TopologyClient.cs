// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using SmartHotel.Devices.RoomDevice.Models;

namespace SmartHotel.Devices.RoomDevice
{
	class TopologyClient
	{
		private readonly HttpClient _httpClient = new HttpClient();
		private readonly string ApiPath = "api/v1.0/";
		private readonly string DevicesPath = "Devices";
		private readonly string DevicesIncludeArgument = "includes=Sensors,ConnectionString,Types,SensorsTypes";
		private readonly RetryPolicy _retryPolicy;
		public TopologyClient( string managementBaseUrl, string sasToken )
		{
			_retryPolicy = Policy.Handle<ManagementApiLimitReachedException>()
				.WaitAndRetryAsync( 3, retryAttempt => TimeSpan.FromSeconds( 1 * retryAttempt ),
					( ex, t ) => Console.WriteLine(
						$"Digital Twins management api limit reached, retrying in {t.TotalSeconds} seconds..." ) );

			string protectedManagementBaseUrl = managementBaseUrl.EndsWith( '/' ) ? managementBaseUrl : $"{managementBaseUrl}/";
			_httpClient.BaseAddress = new Uri( protectedManagementBaseUrl );
			_httpClient.DefaultRequestHeaders.Add( "Authorization", sasToken );
		}

		public async Task<Device> GetDeviceForHardwareId( string hardwareId )
		{
			return await _retryPolicy.ExecuteAsync( async () =>
			 {
				 // TODO: Need to handle the possibility of hitting the limit of 100 messages in a second and throw the ManagementApiLimitReachedException, so then Polly can retry
				 Device device = null;
				 var serializer = new DataContractJsonSerializer( typeof( List<Device> ) );
				 try
				 {
					 var response = _httpClient.GetStreamAsync( $"{ApiPath}{DevicesPath}?hardwareIds={hardwareId}&{DevicesIncludeArgument}" );
					 device = ( serializer.ReadObject( await response ) as List<Device> ).FirstOrDefault( x => x.HardwareId == hardwareId );
				 }
				 catch ( Exception e )
				 {
					 Console.WriteLine( e.Message );
				 }

				 return device;
			 } );
		}
	}
}