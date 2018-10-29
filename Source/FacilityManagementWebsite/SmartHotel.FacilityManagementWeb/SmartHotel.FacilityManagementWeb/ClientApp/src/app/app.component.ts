import { Component, OnInit } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  constructor(private adalService: AdalService) {
    adalService.init(environment.adalConfig);
  }
  public ngOnInit(): void {
    this.adalService.handleWindowCallback();
  }
}
