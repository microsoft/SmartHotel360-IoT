import { Component } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  constructor(private adalService: AdalService) {
    adalService.init(environment.adalConfig);
  }

  ngOnInit(): void {
    this.adalService.handleWindowCallback();
  }

}
