import { AfterViewChecked, ChangeDetectorRef, Component, ElementRef, inject, OnInit, QueryList, signal, ViewChild, ViewChildren } from '@angular/core';
import { AgentService } from '../_services/agent.service';
import { UserMessage } from '../_models/user-message';
import { FormsModule, NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { UserChatHistory } from '../_models/user-chat-history';
import { AgentMessage } from '../_models/agent-message';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { VoiceRecognitionService } from '../_services/voice-recognition.service';

@Component({
  selector: 'app-agent',
  standalone: true,
  imports: [FormsModule, NgFor, NgIf, CommonModule],
  templateUrl: './agent.component.html',
  styleUrl: './agent.component.css'
})
export class AgentComponent implements OnInit, AfterViewChecked {
  @ViewChild('editForm') chatForm?: NgForm;
  @ViewChild('scrollMe') scrollContainer?: any;

  agentService = inject(AgentService);
  private toastr = inject(ToastrService);
  userMessage: UserMessage = { Message: '' };
  chatHistory = signal<UserChatHistory[]>([]);
  chatCompleted = true;
  loading = false;
  isListening = false;
  isPlaying = false;
  audioPlayer: HTMLAudioElement | null = null;

  ngOnInit(): void {
    this.loadMesages();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  constructor(private voiceService: VoiceRecognitionService,  private cdRef: ChangeDetectorRef) {}

  toggleListening() {
    this.isListening = !this.isListening;
    if (this.isListening) {
      this.voiceService.start();
    } else {
      this.voiceService.stop().then(transcript => {
        this.cdRef.detectChanges(); // 👈 Force update
        if (transcript != null && transcript.trim() !== '') {
          this.userMessage.Message = transcript;
          setTimeout(() => {
            this.sendMessage();
          }, 100);
        }
      });
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
  
    const file = input.files[0];
    const maxSizeMB = 5;
    const maxSizeBytes = maxSizeMB * 1024 * 1024;
  
    if (file.size > maxSizeBytes) {
      this.toastr.error(`File is too large. Max size allowed is ${maxSizeMB} MB.`);
      return;
    }
  
    this.uploadDocument(file); 
  }
  

  loadMesages() {
    this.agentService.loadMessages().subscribe({
      next: (chatHistoryResult: UserChatHistory[]) => {
        this.chatHistory.update(history =>
          chatHistoryResult.filter(message => message.chatRole !== 'system')
        );
      },
      error: error => this.toastr.error(error.message)
    });
  }
  
  uploadDocument(file: File) {
    this.loading = true;
    this.chatCompleted = false;
    this.agentService.uploadDocument(file).subscribe({
      next: _ => {
        this.loading = false;
        this.chatCompleted = true;
        this.toastr.success('Document uploaded successfully')
      },
      error: error => {
        this.loading = false;
        this.chatCompleted = true;
        this.toastr.error(error.message)
      }
    });
  }

  sendMessage() {
    if (this.audioPlayer) {
      this.audioPlayer.pause();
      this.audioPlayer.currentTime = 0;
      this.isPlaying = false;
      this.chatCompleted = true;
      this.audioPlayer = null;
      return;
    }

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
          this.audioPlayer = new Audio(agentMessage.audioFilePath);
          this.audioPlayer.play().catch(err => {
            console.error('Audio playback failed', err);
          });

          this.audioPlayer.onplay = () => {
            this.loading = false;
            this.isPlaying = true;
          };

          this.audioPlayer.onended = () => {
            this.loading = false;
            this.chatCompleted = true;
            this.isPlaying = false;
          };

          this.audioPlayer.onerror = (err) => {
            console.error('Audio playback error:', err);
            this.loading = false;
            this.isPlaying = false;
            this.audioPlayer = null;
            this.chatCompleted = true;
          };

        } else {
          this.loading = false;
          this.chatCompleted = true;
          this.isPlaying = false;
        }
        // Reset the chat form
        this.userMessage.Message = '';
        this.scrollToBottom();
      },
      error: error => {
        this.toastr.error(error.message);
        this.loading = false;
        // Reset the chat form
        this.userMessage.Message = '';
      }
    })
  }

  formatMessage(message: string): string {
    return message.replace(/\n/g, '<br>');
  }
  
  private scrollToBottom() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    }
  }

}
