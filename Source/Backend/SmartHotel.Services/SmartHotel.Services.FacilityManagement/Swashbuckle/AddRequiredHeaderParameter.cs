using System;
using System.Collections.Generic;
using System.Linq;
using SmartHotel.Services.FacilityManagement.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartHotel.Services.FacilityManagement.Swashbuckle
{
	public class AddRequiredHeaderParameter : IOperationFilter
	{
		public void Apply( Operation operation, OperationFilterContext context )
		{
			if ( operation.Parameters == null )
			{
				operation.Parameters = new List<IParameter>();
			}

			string firstTag = operation.Tags.FirstOrDefault();
			if ( firstTag != null )
			{
				if ( firstTag.Equals( "Spaces", StringComparison.OrdinalIgnoreCase ) )
				{
					operation.Parameters.Add( new NonBodyParameter
					{
						Name = SpacesController.AdtTokenHeader,
						In = "header",
						Type = "string",
						Required = true
					} );
				}
			}
		}
	}
}
