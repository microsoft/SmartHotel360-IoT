using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class UserAadObjectIdsDescription : Dictionary<string, string>
	{
		public UserAadObjectIdsDescription()
		: base( StringComparer.OrdinalIgnoreCase )
		{
		}

		private static readonly string[] RequiredUserKeys =
		{
			"Head Of Operations",
			"Hotel Brand 1 Manager",
			"Hotel 1 Manager",
			"Hotel 1 Employee"
		};

		public bool AreRequiredValuesFilled()
		{
			return RequiredUserKeys.All(userKey =>
			{
				if (TryGetValue(userKey, out string oid))
				{
					return !string.IsNullOrWhiteSpace(oid);
				}

				return false;
			} );
		}
	}
}
