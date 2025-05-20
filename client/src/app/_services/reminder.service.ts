import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Reminder } from '../_models/reminder';

@Injectable({
  providedIn: 'root'
})
export class ReminderService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);
    
    getActiveReminders(username: string) {
      return this.http.get<Reminder[]>(this.baseUrl + 'reminder/' + username)
    }
    
}
