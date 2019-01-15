import { Component, OnInit, OnDestroy, Input, OnChanges, IterableDiffers, IterableDiffer } from '@angular/core';
import { environment } from 'src/environments/environment';

declare var atlas: any;

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.css']
})
export class MapComponent implements OnInit, OnDestroy, OnChanges {

  private map: any;
  private pushPinsDataSource: any;
  private pushpinInputDiffer: IterableDiffer<[number, number]>;

  constructor(private iterableDiffers: IterableDiffers) {
    this.pushpinInputDiffer = this.iterableDiffers.find([]).create<[number, number]>(null);
  }

  @Input() public pushpinLocations: [number, number][];

  ngOnInit() {
    atlas.setSubscriptionKey(environment.azureMapsKey);
    this.map = new atlas.Map('map', { minZoom: 0, zoom: 0 });
    this.pushPinsDataSource = new atlas.source.DataSource();

    this.map.events.add('load', this.mapLoaded.bind(this));
  }

  ngOnDestroy() {
    if (this.map) {
      this.map.dispose();
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
    this.map.layers.add(new atlas.layer.SymbolLayer(this.pushPinsDataSource, null));

    this.updatePushpinsOnMap();
  }

  private updatePushpinsOnMap() {
    if (!this.pushPinsDataSource) {
      return;
    }

    if (!this.pushpinLocations) {
      this.pushPinsDataSource.clear();
    } else {
      const points = this.pushpinLocations.map(geoLocation => new atlas.Shape(new atlas.data.Point(geoLocation)));
      this.pushPinsDataSource.setShapes(points);
    }
  }
}
