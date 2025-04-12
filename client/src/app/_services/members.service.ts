import { inject, Injectable } from '@angular/core';
import { Member } from '../_models/member';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class MembersService {
  private http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  memberCache = new Map();

  getMember(username: string) {
    const member: Member = [...this.memberCache.values()]
        .reduce((arr, elem) => arr.concat(elem.body), [])
        .find((m: Member) => m.userName === username);
    
    if (member) return of(member);
  
    return this.http.get<Member>(this.baseUrl + 'users/' + username);
  }

  updateMember(member: Member) {
    return this.http.put(this.baseUrl + 'users', member).pipe(
    )
  }
}
