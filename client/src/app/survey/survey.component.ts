import { Component, inject, OnInit } from '@angular/core';
import { SurveyService } from '../_services/survey.service';
import { SurveyForm } from '../_models/survey-form';
import { ToastrService } from 'ngx-toastr';
import { NgxSpinnerModule, NgxSpinnerService } from 'ngx-spinner';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { SurveyFormClientModalComponent } from '../modals/survey-form-client-modal/survey-form-client-modal.component';

@Component({
  selector: 'app-survey',
  standalone: true,
  imports: [NgxSpinnerModule, FormsModule, DatePipe],
  templateUrl: './survey.component.html',
  styleUrl: './survey.component.css'
})
export class SurveyComponent implements OnInit {
  private surveyService = inject(SurveyService);
  private toastr = inject(ToastrService);
  surveyForms? : SurveyForm[];
  surveyForm? : SurveyForm;
  private spinnerService = inject(NgxSpinnerService);
  private modalService = inject(BsModalService);
  bsModalRef: BsModalRef<SurveyFormClientModalComponent> = new BsModalRef<SurveyFormClientModalComponent>();

  ngOnInit(): void {
    this.getSurveyForms();
  }
  
  getSurveyForms() {
    this.spinnerService.show(undefined, {
      type: 'line-scale-party',
      bdColor: 'rgba(2555,2555,255,0)',
      color: '#333333'
    });
     this.surveyService.getSurveyForms().subscribe({
      next: (surveyForms: SurveyForm[]) => {
        this.surveyForms = surveyForms;
        this.spinnerService.hide();
      },
       error: error => this.toastr.error(error.message)
     });
   }

   loadSurveyForm(id: number) {
    this.surveyForm = this.surveyForms?.find(s => s.id === id);
    const initialState: ModalOptions = {
      class: 'modal-lg',
      initialState: {
        title: 'Fill Survey Form',
        id: this.surveyForm?.id,
        formTitle: this.surveyForm?.title,
        formPath: this.surveyForm?.path,
        imagePath: this.surveyForm?.imagePath,
        surveyFormDetails: this.surveyForm?.surveyFormDetails,
        formUpdated: false
      }
    }
    this.bsModalRef = this.modalService.show(SurveyFormClientModalComponent, initialState);
   }
}
