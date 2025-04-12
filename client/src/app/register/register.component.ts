import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { TextInputComponent } from '../_forms/text-input/text-input.component';
import { AccountService } from '../_services/account.service';
import { Router } from '@angular/router';
import { NgFor } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, TextInputComponent, NgFor],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private accountService = inject(AccountService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  registerForm: FormGroup = new FormGroup({});
  validateErrors: string[] | undefined;

  languageOptions = signal<any[]>([
    { code: 'en-US', name: 'English' },
    { code: 'es-ES', name: 'Spanish' },
    { code: 'fr-FR', name: 'French' },
    { code: 'de-DE', name: 'German' },
    { code: 'pt-BR', name: 'Portuguese' },
    { code: 'zh-CN', name: 'Chinese' },
    { code: 'ja-JP', name: 'Japanese' },
    { code: 'ar-EG', name: 'Arabic' },
    { code: 'ru-RU', name: 'Russian' },
    { code: 'hi-IN', name: 'Hindi' }
  ]);

  timeZoneOptions = signal<any[]>([
    'UTC-12:00', 'UTC-11:00', 'UTC-10:00', 'UTC-09:30',
    'UTC-09:00', 'UTC-08:00', 'UTC-07:00', 'UTC-06:00',
    'UTC-05:00', 'UTC-04:00', 'UTC-03:30', 'UTC-03:00',
    'UTC-02:00', 'UTC-01:00', 'UTC+00:00', 'UTC+01:00',
    'UTC+02:00', 'UTC+03:00', 'UTC+03:30', 'UTC+04:00',
    'UTC+04:30', 'UTC+05:00', 'UTC+05:30', 'UTC+05:45',
    'UTC+06:00', 'UTC+06:30', 'UTC+07:00', 'UTC+08:00',
    'UTC+08:45', 'UTC+09:00', 'UTC+09:30', 'UTC+10:00',
    'UTC+10:30', 'UTC+11:00', 'UTC+12:00', 'UTC+12:45',
    'UTC+13:00', 'UTC+14:00'
  ]);

  async ngOnInit(): Promise<void> {
    if (this.accountService.currentUser()) {
      // Already logged in, bounce back to home
      this.router.navigateByUrl('/');
    }
    this.initializeForm();
    const position: any =  await this.getCurrentLocation();
    this.registerForm.patchValue({latitude: position.lat, longitude: position.lng})
  }

  initializeForm() {
    this.registerForm = this.fb.group({
      userName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      fullName: ['', Validators.required],
      country: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(2)]],
      city: ['', Validators.required],
      latitude: [''],
      longitude: [''],
      prefferedLanguage: ['', Validators.required],
      timeZone: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(8)]],
      confirmPassword: ['', [Validators.required, this.matchValues('password')]]
    });
    this.registerForm.controls['password'].valueChanges.subscribe({
      next: () => this.registerForm.controls['confirmPassword'].updateValueAndValidity()
    })
  }

  private getCurrentLocation() {
    return new Promise((resolve, reject) => {
      if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
          (position) => {
            if (position) {
              let lat = position.coords.latitude.toString();
              let lng = position.coords.longitude.toString();

              const location = {
                lat,
                lng,
              };
              resolve(location);
            }
          },
          (error) => console.log(error)
        );
      } else {
        reject('Geolocation is not supported by this browser.');
      }
    });
  }

  matchValues(matchTo: string): ValidatorFn {
    return (control: AbstractControl) => {
      return control.value === control.parent?.get(matchTo)?.value ? null: {isMatching: true}
    }
  }

  register() {
    const { confirmPassword, ...register } = this.registerForm.value;
    this.accountService.register(register).subscribe({
      next: _ => this.router.navigateByUrl('/'),
      error: error => this.validateErrors = error
    })
  }

}

