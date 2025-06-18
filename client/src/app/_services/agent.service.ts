import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { UserMessage } from '../_models/user-message';
import { UserChatHistory } from '../_models/user-chat-history';
import { AgentMessage } from '../_models/agent-message';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AgentService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);
  
  loadMessages() {
    return this.http.get<UserChatHistory[]>(this.baseUrl + 'aiagent');
  }
  
  Chat(message: UserMessage) {
    return this.http.post<AgentMessage>(this.baseUrl + 'aiagent/chat', message);
  }

  uploadDocument(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<AgentMessage>(this.baseUrl + 'aiagent/uploaddocument', formData);
  }
}
