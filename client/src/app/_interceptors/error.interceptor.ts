import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NavigationExtras, Router } from '@angular/router';
import { BsModalService } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { catchError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);
  const modalService = inject(BsModalService);

  return next(req).pipe(
    catchError(error => {
      if(error) {
        switch(error.status){
          case 400:
            if(error.error.errors){
              const modalStateErrors = [];
              for (const key in error.error.errors) {
                if(error.error.errors[key]){
                  modalStateErrors.push(error.error.errors[key]);
                }
                throw modalStateErrors.flat();
              }
            }
            else if(Array.isArray(error.error)) {
              // If error is an array like [{code: "", description: ""}, ...]
              const descriptions = error.error.map((err: any) => err.description);
              throw descriptions;
            }
            else if (typeof error.error === "string") {
              const modalStateErrors = [];
              modalStateErrors.push(error.error);
              throw modalStateErrors.flat(); 
            } 
            else{
              toastr.error(error.error, error.status);
            }
            break;
          case 401:
            toastr.error('Unauthorized', error.status);
            break;
          case 404:
            router.navigateByUrl('/not-found');
            break; 
          case 500:
            const navigationExtras: NavigationExtras = {state: {error: error.error}};
            // Close any open modals
            if (modalService.getModalsCount() > 0) {
              modalService.hide();
            }
            router.navigateByUrl('/server-error', navigationExtras);
            break;
          default:
            toastr.error('Something unexpected went wrong');
            break;
        }
      }
      throw error;
    })
  );
};
