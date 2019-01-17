import { Component, OnInit } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from 'src/environments/environment';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { FacilityService } from '../services/facility.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  private returnUrl: string;

  constructor(
    private formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private adalSvc: AdalService,
    private facilityService: FacilityService) { }

  public useSimpleAuth = false;
  public submitted = false;
  public loginForm: FormGroup;
  public authenticating = false;

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    if (this.adalSvc.userInfo.authenticated) {
      this.router.navigate([this.returnUrl]);
    } else {
      this.useSimpleAuth = environment.useSimpleAuth;

      if (this.useSimpleAuth) {
        this.loginForm = this.formBuilder.group({
          username: ['', Validators.required],
          password: ['', Validators.required]
        });
      }
    }
  }

  // convenience getter for easy access to form fields
  get f() { return this.loginForm.controls; }

  login() {
    if (this.useSimpleAuth) {
      this.submitted = true;
      if (this.loginForm.invalid) {
        return;
      }

      this.authenticating = true;
      this.facilityService.simpleAuthLogin(this.f.username.value, this.f.password.value)
        .subscribe(data => {
          this.router.navigate([this.returnUrl]);
        },
          error => {
            this.authenticating = false;
          });
    } else {
      this.adalSvc.login();
    }
  }
}
