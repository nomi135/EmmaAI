import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AccountService } from '../_services/account.service';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, BsDropdownModule, RouterLink],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.css'
})
export class LayoutComponent {
  supportEmail: string = "support@emmaai.com";
  supportPhone: string = "+1 714 458 0459";
  currentYear: number = new Date().getFullYear();
  accountService = inject(AccountService);
  private router = inject(Router);

  logout(){
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}