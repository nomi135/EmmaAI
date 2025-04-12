import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { User } from '../_models/user';
import { map } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  // Initialize signal with whatever is in localStorage
  currentUser = signal<User | null>(this.getUserFromLocalStorage());
  roles = computed(() => {
    const user = this.currentUser();
    if(user && user.token) {
      const role = JSON.parse(atob(user.token.split('.')[1])).role;
      return Array.isArray(role) ? role: [role];
    }
    return [];
  })

  login(model: any){
    return this.http.post<User>(this.baseUrl + 'account/login', model).pipe(
      map(user =>{
        if(user) {
          this.setCurrentUser(user);
        }
      })
    )
  }

  register(model: any){
    return this.http.post<User>(this.baseUrl + 'account/register', model).pipe(
      map(user =>{
        if(user) {
          this.setCurrentUser(user);
        }
        return user;
      })
    )
  }

  setCurrentUser(user: User) {
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  logout(){
      localStorage.removeItem('user');
      this.currentUser.set(null)
    }

  // Utility: Load user from localStorage
  private getUserFromLocalStorage(): User | null {
    const userJson = localStorage.getItem('user');
    if (!userJson) return null;

    try {
      return JSON.parse(userJson) as User;
    } catch (error) {
      console.error('Failed to parse user from localStorage', error);
      return null;
    }
  }
}
