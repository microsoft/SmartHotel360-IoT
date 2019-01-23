import { Component, OnInit, Input, OnChanges } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { EnvironmentService } from '../services/environment.service';
import TsiClient from 'tsiclient';

@Component({
  selector: 'app-tsi-chart',
  templateUrl: './tsi-chart.component.html',
  styleUrls: ['./tsi-chart.component.css']
})
export class TsiChartComponent implements OnInit, OnChanges {

  @Input() public sensorIds: string[];

  private token: string;
  private client: any;
  private environmentFqdn = '6b193177-b605-4746-83a0-75fbbc885ebf.env.timeseries.azure.com';
  private resource = 'https://api.timeseries.azure.com/';

  constructor(adalService: AdalService) {

    console.log('tsiui-chart-rendering create');

    this.token = adalService.getCachedToken(this.resource);
    this.client = new TsiClient();

    console.log(`tsiui-chart-rendering token loaded: ${this.token != null}`);
  }

  private initializeAvgOccupancy() {
    
    const filteredSensors = [];
    filteredSensors.push(this.sensorIds[0]);
    filteredSensors.push('e8d17c19-7dff-46a2-a3d0-2576e086ad9b'); // comment this line to see that it works with one sensor id
    // filteredSensors.push(this.sensorIds[1]);
    
    console.log(filteredSensors);

    const lineChart = this.client.ux.LineChart(document.getElementById('chart2'));
    const tsqExpressions = [];
    const startDate = new Date('2019-01-14T00:00:00Z');
    const endDate = new Date('2019-01-16T00:00:00Z');
    tsqExpressions.push(new this.client.ux.TsqExpression({ timeSeriesId: filteredSensors }, // instance json
      {
        'Avg Occupancy': {
          kind: 'numeric',
          value: { tsx: '$event.Occupied.Double' },
          filter: null,
          aggregation: { tsx: 'avg($value)' }
        }
      }, // variable json
      { from: startDate, to: endDate, bucketSize: '30m' }, // search span
      '#60B9AE', // color
      'Occupied')); // alias
    tsqExpressions.push(new this.client.ux.TsqExpression({ timeSeriesId: filteredSensors }, // instance json
      {
        'Min Occupancy': {
          kind: 'numeric',
          value: { tsx: '$event.Occupied.Double' },
          filter: null,
          aggregation: { tsx: 'min($value)' }
        }
      }, // variable json
      { from: startDate, to: endDate, bucketSize: '30m' }, // search span
      'red', // color
      'Occupied Min')); // alias
    tsqExpressions.push(new this.client.ux.TsqExpression({ timeSeriesId: filteredSensors }, // instance json
      {
        'Max Occupancy': {
          kind: 'numeric',
          value: { tsx: '$event.Occupied.Double' },
          filter: null,
          aggregation: { tsx: 'max($value)' }
        }
      }, // variable json
      { from: startDate, to: endDate, bucketSize: '30m' }, // search span
      'green', // color
      'Occupied Max')); // alias


    const currentthis = this;
    this.client.server.getTsqResults(this.token, this.environmentFqdn, tsqExpressions.map(function (ae) { return ae.toTsq(); }))
      .then(function (result) {
        const transformedResult = currentthis.client.ux.transformTsqResultsForVisualization(result, tsqExpressions);
        lineChart.render(transformedResult, { theme: 'light', grid: true, tooltip: true, legend: 'compact' }, tsqExpressions);

      });

    return null;
  }

  ngOnInit() {




  }

  ngOnChanges() {

    if (this.sensorIds != null && this.sensorIds.length > 0) {
      console.log(`Sensor Ids loaded: ${this.sensorIds.length > 0}`);

      this.initializeAvgOccupancy();
    }
  }


}
