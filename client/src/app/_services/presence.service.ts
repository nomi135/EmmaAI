import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { User } from '../_models/user';
import { take } from 'rxjs';
import { ReminderService } from './reminder.service';

@Injectable({
  providedIn: 'root'
})
export class PresenceService {
  hubUrl = environment.hubsUrl;
  private hubConnection?: HubConnection;
  private toastr = inject(ToastrService);
  private reminder = inject(ReminderService);
  onlineUsers = signal<string[]>([]);
  audioPlayer: HTMLAudioElement | null = null;

  CreateHubConnection(user: User) {
    this.hubConnection = new HubConnectionBuilder()
    .withUrl(this.hubUrl + 'presence', {
      accessTokenFactory: () => user.token
    })
    .withAutomaticReconnect()
    .build();

    this.hubConnection.start().catch(error => console.log(error));

    this.hubConnection.on('UserIsOnline', username => {
      this.onlineUsers.update(users => [... users, username]);
    });

    this.hubConnection.on('UserIsOffline', username => {
      this.onlineUsers.update(users => users.filter(x => x !==username));
    });

    this.hubConnection.on('GetOnlineUsers', username => {
      this.onlineUsers.set(username);
    });

    this.hubConnection.on('ReceiveReminder', ({id, task, audioPath}) => {
      if (audioPath) {
        this.audioPlayer = new Audio(audioPath);
        this.audioPlayer.play().catch(err => {
          console.error('Audio playback failed', err);
        });
      }
      this.toastr.info('There is a reminder for you: ' + task)
      .onTap
      .pipe(take(1))
      .subscribe(() => console.log('updated triggered'));
    });
  }

  stopHubConnection(){
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      this.hubConnection.stop().catch(error => console.log(error));
    }
  }
}
