import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Contact } from '../_models/contact';

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);

  sendMessage(contact: Contact) {
    return this.http.post<Contact>(this.baseUrl + 'contact', contact);
  }
}
