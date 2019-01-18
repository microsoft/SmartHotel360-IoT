import { AdalInterceptor } from 'adal-angular4';
import { HttpEvent, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AppComponent } from '../app.component';

export class AuthenticationInterceptor extends AdalInterceptor {
  public intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (environment.useSimpleAuth) {
      if (!req.headers.has('Authorization')) {
        const basicAuthData = sessionStorage.getItem(AppComponent.BasicAuthDataSessionStorageKey);
        if (basicAuthData) {
          req = req.clone({
            setHeaders: {
              Authorization: basicAuthData
            }
          });
        }
      }
      return next.handle(req);
    }

    return super.intercept(req, next);
  }
}
