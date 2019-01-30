import { Component, OnInit, Input, OnChanges } from '@angular/core';
import { AdalService } from 'adal-angular4';
import TsiClient from 'tsiclient';
import { environment } from 'src/environments/environment';
import { FacilityService } from '../services/facility.service';

@Component({
  selector: 'app-tsi-chart',
  templateUrl: './tsi-chart.component.html',
  styleUrls: ['./tsi-chart.component.css']
})
export class TsiChartComponent implements OnInit, OnChanges {

  @Input() public motionSensorIds: string[];
  @Input() public lightSensorIds: string[];
  @Input() public tempSensorIds: string[];

  private token: string;
  private client: any;

  constructor(private adalService: AdalService, private facilityService: FacilityService) {
  }

  private initializeChart() {
    const lineChart = this.client.ux.LineChart(document.getElementById('tsichart'));

    const dateTimeNowUTC = new Date();
    const thirtyDaysBack = new Date();
    thirtyDaysBack.setDate(dateTimeNowUTC.getDate() - environment.tsiHowManyDays);

    const startDate = thirtyDaysBack.toISOString();
    const endDate = dateTimeNowUTC.toISOString();

    const motionPredicate = this.buildPredicateString(this.motionSensorIds);
    const lightPredicate = this.buildPredicateString(this.lightSensorIds);
    const tempPredicate = this.buildPredicateString(this.tempSensorIds);

    const aggregateExpressions = [];

    aggregateExpressions.push(
      new this.client.ux.AggregateExpression(
        { predicateString: motionPredicate }, // predicate
        { property: 'Occupied', type: 'Double' }, // measure column
        ['avg'], // measure type,
        { from: startDate, to: endDate, bucketSize: '30m' },  // time range
        null, // split by value
        '#60B9AE', // color
        'Occupied')  // display name
    );

    aggregateExpressions.push(
      new this.client.ux.AggregateExpression(
        { predicateString: lightPredicate }, // predicate
        { property: 'Light', type: 'Double' }, // measure column
        ['avg'], // measure type
        { from: startDate, to: endDate, bucketSize: '30m' },  // time range
        null, // split by value
        'Green', // color
        'Light')  // display name
    );

    aggregateExpressions.push(
      new this.client.ux.AggregateExpression(
        { predicateString: tempPredicate }, // predicate
        { property: 'Temperature', type: 'Double' }, // measure column
        ['avg'], // measure type
        { from: startDate, to: endDate, bucketSize: '30m' },  // time range
        null, // split by value
        'Red', // color
        'Temperature')  // display name
    );

    const currentThis = this;
    this.client.server.getAggregates(this.token, environment.tsiFqdn,
      aggregateExpressions.map(function (ae) { return ae.toTsx(); }))
      .then(function (result) {
        const transformedResult = currentThis.client.ux.transformAggregatesForVisualization(result, aggregateExpressions);
        lineChart.render(transformedResult, { legend: 'compact' }, aggregateExpressions);
      });
  }

  ngOnInit() {
    this.facilityService.executeWhenInitialized(this, this.initialize);
  }

  private initialize(self: TsiChartComponent) {

    self.client = new TsiClient();
    self.token = self.facilityService.getTimeSeriesInsightsToken();
    self.tryUpdateChart();
  }

  ngOnChanges() {
    this.tryUpdateChart();
  }

  private tryUpdateChart() {
    if (this.motionSensorIds && this.lightSensorIds && this.tempSensorIds
      && this.motionSensorIds.length > 0
      && this.lightSensorIds.length > 0
      && this.tempSensorIds.length > 0) {

      this.initializeChart();
    }
  }

  private buildPredicateString(sensorIds: string[]) {
    const result = sensorIds.join(`', '`);
    return `SensorId in ('${result}')`;
  }
}
