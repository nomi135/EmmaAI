import { Component, inject, OnInit } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TextInputComponent } from '../_forms/text-input/text-input.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, TextInputComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
   accountService = inject(AccountService);
   private router = inject(Router);
   private toaster = inject(ToastrService);
   private fb = inject(FormBuilder);
   loginForm: FormGroup = new FormGroup({});
 
   ngOnInit(): void {
    if (this.accountService.currentUser()) {
      // Already logged in, bounce back to home
      this.router.navigateByUrl('/');
    }
     this.initializeForm();
   }
  
   initializeForm() {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', [Validators.required]],
    });
   }

   login(){
    const loginData: any = this.loginForm.value;
     this.accountService.login(loginData).subscribe({
       next: _ => {
         this.router.navigateByUrl('');
       },
       error: (error: any) => this.toaster.error(error.error)
     })
   }
}
