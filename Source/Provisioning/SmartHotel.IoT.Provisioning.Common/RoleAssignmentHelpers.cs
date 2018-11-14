using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
    public static class RoleAssignmentHelpers
    {
	    public static async Task CreateRoleAssignmentAsync(this RoleAssignment roleAssignment, HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings)
	    {
		    Console.WriteLine( $"Creating RoleAssignment: {JsonConvert.SerializeObject( roleAssignment, Formatting.Indented, jsonSerializerSettings )}" );
		    var request = HttpMethod.Post.CreateRequest( "roleassignments", JsonConvert.SerializeObject( roleAssignment, jsonSerializerSettings ) );
		    var response = await httpClient.SendAsync( request );
		    Console.WriteLine( response.IsSuccessStatusCode ? "succeeded..." : "failed..." );
	    }

	    public static async Task<RoleAssignment> GetUniqueRoleAssignmentAsync(this RoleAssignment desiredRoleAssignment,
		    HttpClient httpClient)
	    {
		    var request =
			    HttpMethod.Get.CreateRequest($"roleassignments?path={desiredRoleAssignment.Path}&objectId={desiredRoleAssignment.ObjectId}");
		    var response = await httpClient.SendAsync(request);
		    if ( response.IsSuccessStatusCode )
		    {
			    var content = await response.Content.ReadAsStringAsync();
			    var roleAssignments = JsonConvert.DeserializeObject<IReadOnlyCollection<RoleAssignment>>( content );
			    var roleAssignment = roleAssignments.SingleOrDefault();
			    if ( roleAssignment != null )
			    {
				    Console.WriteLine(
					    $"Retrieved Unique {nameof(RoleAssignment)} using 'path' and 'objectId': {JsonConvert.SerializeObject( roleAssignment, Formatting.Indented )}" );
				    return roleAssignment;
			    }
		    }

		    return null;
	    }
    }
}
