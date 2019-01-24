import { Component, OnInit, Input, OnChanges } from '@angular/core';
import { AdalService } from 'adal-angular4';
import TsiClient from 'tsiclient';

@Component({
  selector: 'app-tsi-chart',
  templateUrl: './tsi-chart.component.html',
  styleUrls: ['./tsi-chart.component.css']
})
export class TsiChartComponent implements OnInit, OnChanges {

  @Input() public sensorIds: string[];

  private tokenRetrieved = false;
  private token: string;
  private client: any;
  // TODO: This needs to come from the environment
  private environmentFqdn = 'bb6100ba-994c-44e3-84b2-01103a6e0b31.env.timeseries.azure.com';
  private resource = 'https://api.timeseries.azure.com/';

  constructor(private adalService: AdalService) {
  }

  private initializeAvgOccupancy() {
    const filteredSensors: string[] = this.sensorIds;
    // filteredSensors.push(this.sensorIds[0]);
    // filteredSensors.push('e8d17c19-7dff-46a2-a3d0-2576e086ad9b'); // comment this line to see that it works with one sensor id
    // filteredSensors.push(this.sensorIds[1]);

    console.log(filteredSensors);

    // TODO: Fix the name to chart or something other than chart2
    const lineChart = this.client.ux.LineChart(document.getElementById('chart2'));

    const startDate = new Date('2019-01-23T00:00:00Z'); // Fix the date ranges to not be hardcoded. Should probably be a few days earlier
    const endDate = new Date('2019-01-24T00:00:00Z'); // This should probably be end of "Today" whatever that day is.

    const predicate = this.buildPredicateString(filteredSensors);
    console.log(predicate);
    const aggregateExpressions = [];

    aggregateExpressions.push(
      new this.client.ux.AggregateExpression(
        { predicateString: predicate }, // predicate
        { property: 'Occupied', type: 'Double' }, // measure column
        ['avg'], // measure type,
        { from: startDate, to: endDate, bucketSize: '30m' },  // time range
        null, // split by value, for you probably just null
        '#60B9AE', // color
        'Occupied Avg')  // display name
    );

    aggregateExpressions.push(
      new this.client.ux.AggregateExpression(
        { predicateString: predicate }, // predicate
        { property: 'Occupied', type: 'Double' }, // measure column
        ['min'], // measure type
        { from: startDate, to: endDate, bucketSize: '30m' },  // time range
        null, // split by value, for you probably just null
        'Green', // color
        'Occupied Min')  // display name
    );

    aggregateExpressions.push(
      new this.client.ux.AggregateExpression(
        { predicateString: predicate }, // predicate
        { property: 'Occupied', type: 'Double' }, // measure column
        ['max'], // measure type
        { from: startDate, to: endDate, bucketSize: '30m' },  // time range
        null, // split by value, for you probably just null
        'Red', // color
        'Occupied Max')  // display name
    );

    const currentThis = this;
    this.client.server.getAggregates(this.token, this.environmentFqdn,
      aggregateExpressions.map(function (ae) { return ae.toTsx(); }))
      .then(function (result) {
        const transformedResult = currentThis.client.ux.transformAggregatesForVisualization(result, aggregateExpressions);
        lineChart.render(transformedResult, { legend: 'compact' }, aggregateExpressions);
      });
  }

  ngOnInit() {
    this.client = new TsiClient();

    this.token = this.adalService.getCachedToken(this.resource);
    if (!this.token) {
      this.adalService.acquireToken('https://api.timeseries.azure.com/')
        .subscribe(result => {
          this.token = result;
          console.log(`TSI Token retrieved: ${this.token}`);
          this.tokenRetrieved = true;
          this.tryUpdateChart();
        });
    } else {
      this.tokenRetrieved = true;
      this.tryUpdateChart();
    }
  }

  ngOnChanges() {
    this.tryUpdateChart();
  }

  private tryUpdateChart() {
    if (this.tokenRetrieved && this.sensorIds != null && this.sensorIds.length > 0) {
      console.log(`Sensor Ids loaded: ${this.sensorIds.length > 0}`);

      this.initializeAvgOccupancy();
    }
  }

  private buildPredicateString(sensorIds: string[]) {
    const result = sensorIds.join(`', '`);
    return `SensorId in ('${result}')`;
  }
}
