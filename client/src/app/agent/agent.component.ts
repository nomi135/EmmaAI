import { Component, ElementRef, inject, OnInit, QueryList, signal, ViewChild, ViewChildren } from '@angular/core';
import { AgentService } from '../_services/agent.service';
import { UserMessage } from '../_models/user-message';
import { FormsModule, NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { UserChatHistory } from '../_models/user-chat-history';
import { AgentMessage } from '../_models/agent-message';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-agent',
  standalone: true,
  imports: [FormsModule, NgFor, NgIf, CommonModule],
  templateUrl: './agent.component.html',
  styleUrl: './agent.component.css'
})
export class AgentComponent implements OnInit {
  @ViewChild('editForm') chatForm?: NgForm;
  @ViewChildren('messageDiv') messageDivs!: QueryList<ElementRef>;

  ngAfterViewInit() {
    this.scrollToLastMessage(); // Optional: scroll initially if needed
  }

  agentService = inject(AgentService);
  private toastr = inject(ToastrService);
  baseUrl = environment.apiUrl;
  userMessage: UserMessage = { Message: '' };
  chatHistory = signal<UserChatHistory[]>([]);
  chatCompleted = true;
  loading = false;

  ngOnInit(): void {
    this.loadMesages();
  }

  loadMesages() {
    this.agentService.loadMessages().subscribe({
      next: (chatHistoryResult: UserChatHistory[]) => {
        this.chatHistory.update(history =>
          chatHistoryResult.filter(message => message.chatRole !== 'system')
        );
      },
      error: error => this.toastr.success(error)
    });
  }
  
  sendMessage() {
    this.chatCompleted = false;
    this.loading = true;
    // Create user history object
     const userHistory: UserChatHistory = {
      chatRole: 'user',
      message: this.userMessage.Message,
      messageSent: new Date() // Adding the current timestamp
    };
    // Push user message to chat history
    this.chatHistory.update(history => [...history, userHistory]);  // Add user message

    this.agentService.Chat(this.userMessage).subscribe({
      next: (agentMessage: AgentMessage) => {
        // Create agent history object
        const agentHistory: UserChatHistory = {
          chatRole: 'assistant',
          message: agentMessage.text, // Assuming `text` is the message property
          messageSent: new Date() // Adding the current timestamp
        };
         // Push agent message to chat history
        this.chatHistory.update(history => [...history, agentHistory]);  // Add agent message
        // Autoplay audio and only set loading = false after audio finishes
        if (agentMessage.audioFilePath) {
          agentMessage.audioFilePath = this.baseUrl + agentMessage.audioFilePath;
          const audio = new Audio(agentMessage.audioFilePath);
          audio.play().catch(err => {
            console.error('Audio playback failed', err);
            this.loading = false; // fallback
          });

          audio.onended = () => {
            this.loading = false;
            this.chatCompleted = true;
          };
        } else {
          this.loading = false;
          this.chatCompleted = true;
        }
        // Reset the chat form
        this.userMessage.Message = '';
        this.scrollToLastMessage();
      },
      error: error => {
        this.toastr.success(error);
        this.loading = false;
        // Reset the chat form
        this.userMessage.Message = '';
      }
    })
  }

  formatMessage(message: string): string {
    return message.replace(/\n/g, '<br>');
  }

  private scrollToLastMessage() {
    setTimeout(() => {
      const lastMessage = this.messageDivs?.last;
      if (lastMessage) {
        lastMessage.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
      }
    }, 0);
  }

}
