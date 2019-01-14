import { Component, OnInit } from '@angular/core';
import { atlas } from 'azure-maps-control';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.css']
})
export class MapComponent implements OnInit {

  private map: atlas.Map;
  constructor() { }

  ngOnInit() {
    atlas.setSubscriptionKey(environment.azureMapsKey);
    this.map = new atlas.Map('map', {});
  }

}
