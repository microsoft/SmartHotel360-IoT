import { Component, OnInit } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adalSvc: AdalService) { }

  ngOnInit() {
    const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    if (this.adalSvc.userInfo.authenticated) {
      this.router.navigate([returnUrl]);
    }
  }

  login() {
    this.adalSvc.login();
  }
}
