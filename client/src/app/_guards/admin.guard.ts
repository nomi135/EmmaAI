import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';

export const adminGuard: CanActivateFn = (route, state) => {
  const accountService = inject(AccountService);
  const toastr = inject(ToastrService);
  const router = inject(Router);

  if(accountService.roles().includes('Admin')) {
    return true;
  } else {
    router.navigate(['/']);
    toastr.error('You cannot enter this area');
    return false;
  }
};
