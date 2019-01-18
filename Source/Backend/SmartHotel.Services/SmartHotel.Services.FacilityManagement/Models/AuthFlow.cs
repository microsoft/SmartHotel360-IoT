namespace SmartHotel.Services.FacilityManagement.Models
{
	public class AuthFlow
	{
		public bool UseAdalAuthFlow => BasicAuthOptions == null
									   || string.IsNullOrWhiteSpace( BasicAuthOptions.Username )
		                               || string.IsNullOrWhiteSpace( BasicAuthOptions.Password )
									   || string.IsNullOrWhiteSpace( BasicAuthOptions.ApplicationId )
									   || string.IsNullOrWhiteSpace( BasicAuthOptions.ApplicationSecret )
									   || string.IsNullOrWhiteSpace( BasicAuthOptions.TenantId );
		public BasicAuthOptions BasicAuthOptions { get; set; }
	}
}
