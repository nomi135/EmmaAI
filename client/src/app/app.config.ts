import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { errorInterceptor } from './_interceptors/error.interceptor';
import { jwtInterceptor } from './_interceptors/jwt.interceptor';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TimeagoModule } from 'ngx-timeago';
import { NgxSpinnerModule } from 'ngx-spinner';
import { ModalModule } from 'ngx-bootstrap/modal';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([errorInterceptor, jwtInterceptor])),
    provideAnimations(),
    provideToastr({
      positionClass: 'toast-bottom-right',
      timeOut: 5000
    }),
    importProvidersFrom(NgxSpinnerModule, TimeagoModule.forRoot(), ModalModule.forRoot())
  ]
};
