import { Component, inject, OnInit } from '@angular/core';
import { TextInputComponent } from "../_forms/text-input/text-input.component";
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ContactService } from '../_services/contact.service';
import { ToastrService } from 'ngx-toastr';
import { Contact } from '../_models/contact';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [ReactiveFormsModule, TextInputComponent],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.css'
})
export class ContactComponent implements OnInit {
  private contactService = inject(ContactService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  contactForm: FormGroup = new FormGroup({});
  validateErrors: string[] | undefined;
  private toastr = inject(ToastrService);

  ngOnInit(): void {
    this.initializeForm();
  }

  initializeForm() {
    this.contactForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      subject: ['', Validators.required],
      message: ['', Validators.required]
    });
  }

  sendMessage() {
    const contactData: Contact = this.contactForm.value;

    this.contactService.sendMessage(contactData).subscribe({
      next: _ => {
        this.toastr.success('Your message has been sent. Thank you!');
      },
      error: error => {
        this.validateErrors = error;
      }
    })
  }
}
