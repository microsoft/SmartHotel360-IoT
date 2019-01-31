namespace SmartHotel.Services.FacilityManagement.Models
{
	public class AuthFlow
	{
		public bool UseAdalAuthFlow => BasicAuthOptions == null
		                               || string.IsNullOrWhiteSpace(BasicAuthOptions.Username)
		                               || string.IsNullOrWhiteSpace(BasicAuthOptions.Password);
		public BasicAuthOptions BasicAuthOptions { get; set; }
	}
}
