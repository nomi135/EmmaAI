import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { inject } from '@angular/core';

export const authGuard: CanActivateFn = (route, state) => {
  const accountService = inject(AccountService);
  const router = inject(Router);
  const toastr = inject(ToastrService);

  if(accountService.currentUser()) {
    return true;
  } else{
    router.navigate(['/']);
    toastr.error('You shall not pass!');
    return false;
  }
};
