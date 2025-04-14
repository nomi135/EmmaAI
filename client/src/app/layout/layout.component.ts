import { Component, inject, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { AccountService } from '../_services/account.service';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, BsDropdownModule, RouterLink, NgIf],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.css'
})
export class LayoutComponent implements OnInit {
  supportEmail: string = "support@emmaai.com";
  supportPhone: string = "+1 714 458 0459";
  currentYear: number = new Date().getFullYear();
  accountService = inject(AccountService);
  private router = inject(Router);
  currentRoute: string = '';

  ngOnInit(): void {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.currentRoute = event.urlAfterRedirects;
      }
    });
  }
  
  logout(){
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}