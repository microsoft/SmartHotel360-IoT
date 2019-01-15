export interface IPushpinLocation {
  name: string;
  parentName: string;
  geoLocation: [number, number];
}

export function getPushpinLocation(space: ISpace, parentSpaceName?: string): IPushpinLocation {
  const longitudeProperty = space.properties.find(p => p.name === 'Longitude');
  const latitudeProperty = space.properties.find(p => p.name === 'Latitude');
  if (longitudeProperty && latitudeProperty) {
    return {
      name: space.friendlyName,
      parentName: parentSpaceName,
      geoLocation: [+longitudeProperty.value, +latitudeProperty.value]
    };
  }

  return undefined;
}
