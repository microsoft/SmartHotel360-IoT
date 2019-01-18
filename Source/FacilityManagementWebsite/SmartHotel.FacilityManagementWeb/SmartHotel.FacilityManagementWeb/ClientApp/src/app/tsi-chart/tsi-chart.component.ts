import { Component, OnInit } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { EnvironmentService } from '../services/environment.service';

@Component({
  selector: 'app-tsi-chart',
  templateUrl: './tsi-chart.component.html',
  styleUrls: ['./tsi-chart.component.css']
})
export class TsiChartComponent implements OnInit {

  constructor(adalService: AdalService) {
    
    console.log('tsiui-chart-rendering create');


    const token = adalService.getCachedToken('https://api.timeseries.azure.com/');
    
    console.log(`tsiui-chart-rendering token loaded: ${token != null}`);
   }
  ngOnInit() {
  }

}
