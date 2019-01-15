namespace SmartHotel.Services.FacilityManagement.Models
{
	public class AuthFlow
	{
		public bool UseAdalAuthFlow => string.IsNullOrWhiteSpace( SimpleAuthApiKey ) ||
									   string.IsNullOrWhiteSpace( SimpleAuthClientSecret );
		public string SimpleAuthApiKey { get; set; }
		public string SimpleAuthClientSecret { get; set; }
	}
}
