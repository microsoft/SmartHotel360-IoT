import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TsiChartComponent } from './tsi-chart.component';

describe('TsiChartComponent', () => {
  let component: TsiChartComponent;
  let fixture: ComponentFixture<TsiChartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TsiChartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TsiChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
