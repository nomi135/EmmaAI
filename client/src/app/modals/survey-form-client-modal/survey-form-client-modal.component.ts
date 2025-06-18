import { Component, inject, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgxSpinnerModule, NgxSpinnerService } from 'ngx-spinner';
import { SurveyService } from '../../_services/survey.service';
import { ToastrService } from 'ngx-toastr';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { SurveyFormDetail } from '../../_models/survey-form-detail';
import { SurveyForm } from '../../_models/survey-form';
import { SurveyFormData } from '../../_models/survey-form-data';
import { SurveyFormClientPreviewModalComponent } from '../survey-form-client-preview-modal/survey-form-client-preview-modal.component';

@Component({
  selector: 'app-survey-form-client-modal',
  standalone: true,
  imports: [FormsModule, NgxSpinnerModule],
  templateUrl: './survey-form-client-modal.component.html',
  styleUrl: './survey-form-client-modal.component.css'
})
export class SurveyFormClientModalComponent implements OnInit {
  private surveyService = inject(SurveyService);
  private toastr = inject(ToastrService);
  private spinnerService = inject(NgxSpinnerService);
  bsModalRef = inject(BsModalRef);
  private modalService = inject(BsModalService);
  @Input() title: string = '';
  @Input() id: number = 0;
  @Input() formTitle: string = '';
  @Input() formPath: string = '';
  @Input() imagePath: string = '';
  @Input() surveyFormDetails: SurveyFormDetail[] = [];
  @Input() formUpdated: boolean = false;
  surveyForm : SurveyForm = {
    id : 0,
    title : '',
    created : new Date(),
    path : '',
    imagePath : '',
    surveyFormDetails : []
  };
  private surveyFormData : SurveyFormData[] = [];
  selectedPagePath : string = '';
  imagePathList : string[] = [];
    
  ngOnInit(): void {
    this.surveyForm.id = this.id;
    this.surveyForm.title = this.formTitle;
    this.surveyForm.path = this.formPath;
    this.surveyForm.imagePath = this.imagePath;
    this.imagePathList = this.imagePath.split(',');
    this.surveyForm.surveyFormDetails = this.surveyFormDetails;
    this.getSurveyFormData(this.id);
  }

  getSurveyFormData(surveyFormId : number) {
    this.spinnerService.show('spinner-survey-form-data', {
      type: 'line-scale-party',
      bdColor: 'rgba(2555,2555,255,0)',
      color: '#333333'
    });
    this.surveyService.getSurveyFormData(surveyFormId).subscribe({
      next: (surveyFormData : SurveyFormData[]) => {
        this.spinnerService.hide('spinner-survey-form-data');
        this.surveyFormData = surveyFormData;
        for (let data of surveyFormData) {
          this.surveyFormDetails.filter(s => s.id === data.surveyFormDetailId)[0].value = data.value;
        }
        if(surveyFormData.length > 0) {
          this.title = 'Update Survey Form Data';
        }
      },
      error: error => {
        this.spinnerService.hide('spinner-survey-form-data');
        let errorMsg = error[0];
        if(errorMsg == null){
          errorMsg = error.message;
        } 
        this.toastr.error(errorMsg);
      }
    });
  }

  checkValue(values: any, detail: any) {
    detail.value = values.currentTarget.checked ? 'checked' : '';
  }

  previewChanges() {
    if(this.selectedPagePath === '') {
      this.toastr.error('Please select page to preview changes');
    }
    else {
      let paths = this.selectedPagePath.split('/');
      let page = paths[paths.length - 1].replace('_','').replace('.png','').replace('page','');
      const initialState: ModalOptions = {
        class: 'modal-lg',
        initialState: {
          title: 'Preview Page ' + page,
          imageUrl: this.selectedPagePath,
          pageNo: page,
          surveyFormDetails: this.surveyFormDetails
        }
      }
      this.bsModalRef = this.modalService.show(SurveyFormClientPreviewModalComponent, initialState);
    }
  }

  submitSurveyFormData() {
    if (this.surveyFormData.length === 0) {
      this.surveyFormData = [];
      for (let detail of this.surveyFormDetails) {
        let data: SurveyFormData = {
          id : 0,
          surveyFormDetailId: detail.id,
          value : detail.value,
          userId : 0,
          userName : '',
          path : '',
          dateCreated : new Date()
        };
        this.surveyFormData.push(data);
      }
      this.spinnerService.show('spinner-survey-form-data', {
        type: 'line-scale-party',
        bdColor: 'rgba(2555,2555,255,0)',
        color: '#333333'
      });
      this.surveyService.submitSurveyFormData(this.surveyFormData).subscribe({
        next: _ => {
          this.formUpdated = true;
          this.bsModalRef.hide();
          this.spinnerService.hide('spinner-survey-form-data');
          this.toastr.success('Survey form data submitted successfully');
        },
        error: error => {
          this.spinnerService.hide('spinner-survey-form-data');
          let errorMsg = error[0];
          if(errorMsg == null){
            errorMsg = error.message;
          } 
          this.toastr.error(errorMsg);
        }
      });
    }  
    else {
      for (let detail of this.surveyFormDetails) {
        this.surveyFormData.filter(d => d.surveyFormDetailId === detail.id)[0].value = detail.value;
      }
      this.surveyService.updateSurveyFormData(this.surveyFormData).subscribe({
        next: _ => {
          this.formUpdated = true;
          this.bsModalRef.hide();
          this.spinnerService.hide('spinner-survey-form-data');
          this.toastr.success('Survey form data updated successfully');
        },
        error: error => {
          this.spinnerService.hide('spinner-survey-form-data');
          let errorMsg = error[0];
          if(errorMsg == null){
            errorMsg = error.message;
          } 
          this.toastr.error(errorMsg);
        }
      });
    }
  }
}