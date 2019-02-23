import { Component, OnInit, OnDestroy, Input, OnChanges, IterableDiffers, IterableDiffer } from '@angular/core';
import { environment } from 'src/environments/environment';
import { IPushpinLocation } from './IPushPinLocation';

declare var atlas: any;

// tslint:disable:max-line-length
// Clustering example: https://github.com/Azure-Samples/AzureMapsCodeSamples/blob/master/AzureMapsCodeSamples/Bubble%20Layer/Point%20Clusters%20in%20Bubble%20Layer.html
// Popup example: https://docs.microsoft.com/en-us/azure/azure-maps/map-add-popup
// tslint:enable:max-line-length

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.css']
})
export class MapComponent implements OnInit, OnDestroy, OnChanges {

  private map: any;
  private pushPinsDataSource: any;
  private pushpinInputDiffer: IterableDiffer<IPushpinLocation>;

  private popupTemplate = '<div class="customInfobox"><div class="name">{name}</div>'
    + '{parentInfo}<div class="geoLocation">{geoLocation}</div></div>';
  private popupParentInfoTemplate = '<div class="parentName">({parentName})</div>';
  private popup: any;

  constructor(private iterableDiffers: IterableDiffers) {
    this.pushpinInputDiffer = this.iterableDiffers.find([]).create<IPushpinLocation>(null);
  }

  @Input() public pushpinLocations: IPushpinLocation[];

  ngOnInit() {
    atlas.setSubscriptionKey(environment.azureMapsKey);
    this.map = new atlas.Map('map', { minZoom: 0, zoom: 0 });

    this.pushPinsDataSource = new atlas.source.DataSource(null, {
      // Tell the data source to cluster point data.
      cluster: true,
      // The radius in pixels to cluster points together.
      clusterRadius: 45,
      // The maximium zoom level in which clustering occurs.
      // If you zoom in more than this, all points are rendered as symbols.
      clusterMaxZoom: 15
    });

    // Create a popup but leave it closed so we can update it and display it later.
    this.popup = new atlas.Popup({
      position: [0, 0],
      pixelOffset: [0, -18],
      closeButton: false
    });

    this.map.events.add('load', this.mapLoaded.bind(this));
  }

  ngOnDestroy() {
    if (this.map) {
      this.map.dispose();
    }

    if (this.pushPinsDataSource) {
      this.pushPinsDataSource.dispose();
    }

    if (this.popup) {
      this.popup.remove();
    }
  }

  ngOnChanges() {
    const pushpinLocationsChanges = this.pushpinInputDiffer.diff(this.pushpinLocations);
    if (pushpinLocationsChanges) {
      this.updatePushpinsOnMap();
    }
  }

  private mapLoaded() {
    this.map.sources.add(this.pushPinsDataSource);

    const clusterBubbleLayer = new atlas.layer.BubbleLayer(this.pushPinsDataSource, null,
      {
        // Scale the size of the clustered bubble based on the number of points inthe cluster.
        radius: [
          'step',
          ['get', 'point_count'],
          15,         // Default of 20 pixel radius.
          10, 25,    // If point_count >= 10, radius is 25 pixels.
          100, 35     // If point_count >= 100, radius is 35 pixels.
        ],

        // Change the color of the cluster based on the value on the point_cluster property of the cluster.
        color: 'rgba(0,57,99,0.8)',
        strokeWidth: 0,
        filter: ['has', 'point_count'] // Only rendered data points which have a point_count property, which clusters do.
      });

    // Create a layer to render the individual locations.
    const pushpinLayer = new atlas.layer.SymbolLayer(this.pushPinsDataSource, null, {
      filter: ['!', ['has', 'point_count']] // Filter out clustered points from this layer.
    });

    this.map.events.add('click', clusterBubbleLayer, this.clusterClicked.bind(this));
    this.map.events.add('mouseenter', clusterBubbleLayer, () => {
      this.map.getCanvas().style.cursor = 'pointer';
    });
    this.map.events.add('mouseleave', clusterBubbleLayer, () => {
      this.map.getCanvas().style.cursor = '';
    });

    this.map.events.add('mouseover', pushpinLayer, this.showPopup.bind(this));
    this.map.events.add('mouseleave', pushpinLayer, this.hidePopup.bind(this));

    this.map.layers.add([
      clusterBubbleLayer,
      // Create a symbol layer to render the count of locations in a cluster.
      new atlas.layer.SymbolLayer(this.pushPinsDataSource, null, {
        iconOptions: {
          image: 'none' // Hide the icon image.
        },
        textOptions: {
          textField: '{point_count_abbreviated}',
          color: '#FFFFFF',
          offset: [0, 0.4]
        }
      }),
      pushpinLayer
    ]);

    this.updatePushpinsOnMap();
  }

  private updatePushpinsOnMap() {
    if (!this.pushPinsDataSource) {
      return;
    }

    if (!this.pushpinLocations) {
      this.pushPinsDataSource.clear();
    } else {
      const points = this.pushpinLocations
        .map(pushpinLocation => new atlas.data.Feature(new atlas.data.Point(pushpinLocation.geoLocation), {
          name: pushpinLocation.name,
          parentName: pushpinLocation.parentName,
          geoLocation: `${pushpinLocation.geoLocation[1]}, ${pushpinLocation.geoLocation[0]}`
        }));
      this.pushPinsDataSource.setShapes(points);
    }
  }

  private clusterClicked(e: any) {
    if (e && e.shapes && e.shapes.length > 0 && e.shapes[0].properties.cluster) {
      // Get the clustered point from the event.
      const cluster = e.shapes[0];
      // Get the cluster expansion zoom level. This is the zoom level at which the cluster starts to break apart.
      this.pushPinsDataSource.getClusterExpansionZoom(cluster.properties.cluster_id)
        .then(zoom => {
          // Update the map camera to be centered over the cluster.
          this.map.setCamera({
            center: cluster.geometry.coordinates,
            zoom: zoom,
            type: 'ease',
            duration: 200
          });
        });
    }
  }

  private showPopup(e: any) {
    // Make sure that the point exists.
    if (e.shapes && e.shapes.length > 0) {

      const properties = e.shapes[0].getProperties();
      const parentInfoContent = properties.parentName
        ? this.popupParentInfoTemplate.replace(/{parentName}/g, properties.parentName)
        : '';

      const content = this.popupTemplate.replace(/{name}/g, properties.name)
        .replace(/{parentInfo}/g, parentInfoContent)
        .replace(/{geoLocation}/g, properties.geoLocation);

      const coordinate = e.shapes[0].getCoordinates();

      this.popup.setOptions({
        content: content,
        position: coordinate
      });
      this.popup.open(this.map);
    }
  }

  private hidePopup() {
    this.popup.close();
  }
}
