import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { UserMessage } from '../_models/user-message';
import { UserChatHistory } from '../_models/user-chat-history';
import { AgentMessage } from '../_models/agent-message';

@Injectable({
  providedIn: 'root'
})
export class AgentService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);
  
  loadMessages() {
    return this.http.get<UserChatHistory[]>(this.baseUrl + 'aiagent')
  }
  
  Chat(message: UserMessage) {
    return this.http.post<AgentMessage>(this.baseUrl + 'aiagent', message)
  }
}
