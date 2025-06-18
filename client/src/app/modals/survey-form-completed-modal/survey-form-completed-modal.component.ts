import { Component, inject, Input, OnInit } from '@angular/core';
import { AdminService } from '../../_services/admin.service';
import { ToastrService } from 'ngx-toastr';
import { NgxSpinnerModule, NgxSpinnerService } from 'ngx-spinner';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { SurveyFormData } from '../../_models/survey-form-data';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-survey-form-completed-modal',
  standalone: true,
  imports: [NgxSpinnerModule, DatePipe],
  templateUrl: './survey-form-completed-modal.component.html',
  styleUrl: './survey-form-completed-modal.component.css'
})
export class SurveyFormCompletedModalComponent implements OnInit {
  private adminService = inject(AdminService);
  private toastr = inject(ToastrService);
  private spinnerService = inject(NgxSpinnerService);
  bsModalRef = inject(BsModalRef);
  @Input() title: string = '';
  @Input() id: number = 0;
  surveyFormData : SurveyFormData[] = [];

  ngOnInit(): void {
    this.getCompletedSurveyForms(this.id);
  }

  getCompletedSurveyForms(id : number) {
    this.spinnerService.show('spinner-completed-survey-form-data', {
      type: 'line-scale-party',
      bdColor: 'rgba(2555,2555,255,0)',
      color: '#333333'
    });
    this.adminService.getCompletedSurveyForms(id).subscribe({
      next: (surveyFormData : SurveyFormData[]) => {
        this.spinnerService.hide('spinner-completed-survey-form-data');
        this.surveyFormData = surveyFormData;
      },
      error: error => {
        this.spinnerService.hide('spinner-completed-survey-form-data');
        let errorMsg = error[0];
        if(errorMsg == null){
          errorMsg = error.message;
        } 
        this.toastr.error(errorMsg);
      }
    });
  }
}
