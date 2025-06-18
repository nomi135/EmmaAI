import { Component, HostListener, inject, OnInit, signal, ViewChild } from '@angular/core';
import { Member } from '../../_models/member';
import { AccountService } from '../../_services/account.service';
import { MembersService } from '../../_services/members.service';
import { FormsModule, NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TimeagoModule } from 'ngx-timeago';
import { DatePipe, NgFor } from '@angular/common';

@Component({
  selector: 'app-member-edit',
  standalone: true,
  imports: [FormsModule, TimeagoModule, DatePipe, NgFor],
  templateUrl: './member-edit.component.html',
  styleUrl: './member-edit.component.css'
})
export class MemberEditComponent implements OnInit {
  @ViewChild('editForm') editForm?: NgForm;
  @HostListener('window:beforeunload', ['$event']) notify($event:any) {
    $event.returnValue = true;
  }

  member?: Member;
  private accountService = inject(AccountService);
  private memberService = inject(MembersService);
  private toastr = inject(ToastrService);
  validateErrors: string[] | undefined;

  languageOptions = signal<any[]>([
    { code: 'en-US', name: 'English' },
    { code: 'es-ES', name: 'Spanish' },
    { code: 'fr-FR', name: 'French' },
    { code: 'de-DE', name: 'German' },
    { code: 'pt-BR', name: 'Portuguese' },
    { code: 'zh-CN', name: 'Chinese' },
    { code: 'ja-JP', name: 'Japanese' },
    { code: 'ar-EG', name: 'Arabic' },
    { code: 'ru-RU', name: 'Russian' },
    { code: 'hi-IN', name: 'Hindi' }
  ]);
  
  timeZoneOptions = signal<any[]>([
    'UTC-12:00', 'UTC-11:00', 'UTC-10:00', 'UTC-09:30',
    'UTC-09:00', 'UTC-08:00', 'UTC-07:00', 'UTC-06:00',
    'UTC-05:00', 'UTC-04:00', 'UTC-03:30', 'UTC-03:00',
    'UTC-02:00', 'UTC-01:00', 'UTC+00:00', 'UTC+01:00',
    'UTC+02:00', 'UTC+03:00', 'UTC+03:30', 'UTC+04:00',
    'UTC+04:30', 'UTC+05:00', 'UTC+05:30', 'UTC+05:45',
    'UTC+06:00', 'UTC+06:30', 'UTC+07:00', 'UTC+08:00',
    'UTC+08:45', 'UTC+09:00', 'UTC+09:30', 'UTC+10:00',
    'UTC+10:30', 'UTC+11:00', 'UTC+12:00', 'UTC+12:45',
    'UTC+13:00', 'UTC+14:00'
  ]);

  ngOnInit(): void {
    this.loadMember();
  }

  loadMember() {
    const user = this.accountService.currentUser();
    if(!user) return;
    this.memberService.getMember(user.userName).subscribe({
      next: member => this.member = member
    });
  }

  updateMember() {
    this.memberService.updateMember(this.editForm?.value).subscribe({
      next: _ => {
        this.toastr.success('Profile updated successfully');
      },
      error: error => this.validateErrors = error
    });
  }

}