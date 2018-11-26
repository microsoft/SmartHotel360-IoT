// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
				.WaitAndRetryAsync( 5, retryAttempt => TimeSpan.FromSeconds( 3 * retryAttempt ),
					( ex, t ) => Console.WriteLine(
						$"Digital Twins management api limit reached, retrying in {t.TotalSeconds} seconds..." ) );

			string protectedManagementBaseUrl = managementBaseUrl.EndsWith( '/' ) ? managementBaseUrl : $"{managementBaseUrl}/";
			if ( !protectedManagementBaseUrl.EndsWith( ApiPath ) )
			{
				protectedManagementBaseUrl = $"{protectedManagementBaseUrl}{ApiPath}";
			}
			_httpClient.BaseAddress = new Uri( protectedManagementBaseUrl );
			_httpClient.DefaultRequestHeaders.Add( "Authorization", sasToken );
		}

		public async Task<Device> GetDeviceForHardwareId( string hardwareId )
		{
			try
			{
				return await _retryPolicy.ExecuteAsync( async () =>
				{
					Device device = null;
					var serializer = new DataContractJsonSerializer( typeof( List<Device> ) );

					HttpResponseMessage response = await _httpClient.GetAsync( $"{DevicesPath}?hardwareIds={hardwareId}&{DevicesIncludeArgument}" );
					Console.WriteLine( $"Retrieving device information: {response.RequestMessage.RequestUri.AbsoluteUri}" );
					if ( response.IsSuccessStatusCode )
					{
						var devices = serializer.ReadObject( await response.Content.ReadAsStreamAsync() ) as List<Device>;
						device = devices?.FirstOrDefault( x => x.HardwareId == hardwareId );
					}
					else
					{
						string responseMessage = response.Content == null
							? string.Empty
							: await response.Content.ReadAsStringAsync();
						if ( response.StatusCode == HttpStatusCode.TooManyRequests
							 || ( !string.IsNullOrWhiteSpace( responseMessage )
								  && responseMessage.Contains( "The request has been throttled", StringComparison.OrdinalIgnoreCase ) ) )
						{
							throw new ManagementApiLimitReachedException( $"Failed to get device for Hardware Id: {hardwareId}" );
						}
					}

					return device;
				} );
			}
			catch ( Exception e )
			{
				Console.WriteLine( e.Message );
			}

			return null;
		}
	}
}