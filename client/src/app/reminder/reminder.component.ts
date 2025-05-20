import { Component, inject } from '@angular/core';
import { ReminderService } from '../_services/reminder.service';
import { ToastrService } from 'ngx-toastr';
import { Reminder } from '../_models/reminder';
import { AccountService } from '../_services/account.service';
import { NgxSpinnerModule, NgxSpinnerService } from 'ngx-spinner';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-reminder',
  standalone: true,
  imports: [DatePipe, NgxSpinnerModule],
  templateUrl: './reminder.component.html',
  styleUrl: './reminder.component.css'
})
export class ReminderComponent {
  reminderService = inject(ReminderService);
  accountService = inject(AccountService);
  private toastr = inject(ToastrService);
  reminders?: Reminder[];
  private spinnerService = inject(NgxSpinnerService);
  

  ngOnInit(): void {
    this.getActiveReminders();
  }

  getActiveReminders() {
    const user = this.accountService.currentUser();
    if(!user) return;
    this.spinnerService.show(undefined, {
      type: 'line-scale-party',
      bdColor: 'rgba(2555,2555,255,0)',
      color: '#333333'
    });
    this.reminderService.getActiveReminders(user.userName).subscribe({
      next: (reminders: Reminder[]) => {
        this.reminders = reminders;
        this.spinnerService.hide();
      },
      error: error => this.toastr.error(error.message)
    });
  }
}
