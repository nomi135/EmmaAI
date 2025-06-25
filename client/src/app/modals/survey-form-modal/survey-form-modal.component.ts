import { Component, inject, Input, OnInit } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SurveyForm } from '../../_models/survey-form';
import { AdminService } from '../../_services/admin.service';
import { ToastrService } from 'ngx-toastr';
import { NgxSpinnerModule, NgxSpinnerService } from 'ngx-spinner';
import { SurveyFormDetail } from '../../_models/survey-form-detail';

@Component({
  selector: 'app-survey-form-modal',
  standalone: true,
  imports: [NgIf, FormsModule, NgxSpinnerModule],
  templateUrl: './survey-form-modal.component.html',
  styleUrl: './survey-form-modal.component.css'
})
export class SurveyFormModalComponent implements OnInit {
  private adminService = inject(AdminService);
  private toastr = inject(ToastrService);
  private spinnerService = inject(NgxSpinnerService);
  bsModalRef = inject(BsModalRef);
  @Input() title: string = '';
  @Input() id: number = 0;
  @Input() formTitle: string = '';
  @Input() formPath: string = '';
  @Input() surveyFormDetails: SurveyFormDetail[] = [];
  @Input() formUpdated: boolean = false;
  file : File | null = null;
  surveyForm : SurveyForm = {
    id : 0,
    title : '',
    created : new Date(),
    path : '',
    imagePath : '',
    clientPath : '',
    surveyFormDetails : []
  }; 
  
  ngOnInit(): void {
    this.surveyForm.id = this.id;
    this.surveyForm.title = this.formTitle;
    this.surveyForm.path = this.formPath;
    this.surveyForm.surveyFormDetails = this.surveyFormDetails;
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
  
    this.file = input.files[0];
    this.surveyForm.title = this.file.name;
    this.extractSurveyFormData();
  }

  extractSurveyFormData() {
    this.spinnerService.show('spinner-survey-form-detail', {
      type: 'line-scale-party',
      bdColor: 'rgba(2555,2555,255,0)',
      color: '#333333'
    });
    this.adminService.extractSurveyFormData(this.file).subscribe({
      next: surveyFormDetail => {
        this.spinnerService.hide('spinner-survey-form-detail');
        this.surveyForm.surveyFormDetails = surveyFormDetail;
      },
      error: error => {
        this.spinnerService.hide('spinner-survey-form-detail');
        let errorMsg = error[0];
        if(errorMsg == null){
          errorMsg = error.message;
        } 
        this.toastr.error(errorMsg);
      }
    })
  }

  getTextCoordinates(filePath: string, pageNo: string, key: string, id: number) {
    let occurrence = 0;
    
    for (const a of this.surveyForm.surveyFormDetails.filter(d => d.key === key)) {
      ++occurrence;
      if (a.id === id) {
        break; 
      }
    }
      
    this.spinnerService.show('spinner-survey-form-detail', {
      type: 'line-scale-party',
      bdColor: 'rgba(2555,2555,255,0)',
      color: '#333333'
    });
    this.adminService.getTextCoordinates(filePath, pageNo, key, occurrence).subscribe({
      next: textCoordinates => {
        this.spinnerService.hide('spinner-survey-form-detail');
        
        this.surveyForm.surveyFormDetails.filter(a => a.key === key && a.id === id).forEach(a => {
          a.left = textCoordinates.x;
          a.top = textCoordinates.y;
          a.width = textCoordinates.width;
          a.height = textCoordinates.height;
          a.fontName = textCoordinates.fontName;
          a.fontSize = textCoordinates.fontSize;
        });
      },
      error: error => {
        this.spinnerService.hide('spinner-survey-form-detail');
        let errorMsg = error[0];
        if(errorMsg == null){
          errorMsg = error.message;
        } 
        this.toastr.error(errorMsg);
      }
    })
  }

  addSurveyFormField() {
    this.surveyForm.surveyFormDetails.push({
      id : 0,
      key : '[NEW FIELD]',
      label : 'New Field',
      type : 'text',
      value : '',
      pageNo : '1',
      left : this.surveyForm.surveyFormDetails[0].left,
      top : this.surveyForm.surveyFormDetails[0].top,
      width : this.surveyForm.surveyFormDetails[0].width,
      height : this.surveyForm.surveyFormDetails[0].height,
      fontName : this.surveyForm.surveyFormDetails[0].fontName,
      fontSize : this.surveyForm.surveyFormDetails[0].fontSize,
    }); 
  }
  
  removeSurveyFormDetail(id: number) {
    this.surveyForm.surveyFormDetails = this.surveyForm.surveyFormDetails.filter(a => a.id !== id);
  }

  submitForm() {
    this.spinnerService.show('spinner-survey-form-detail', {
      type: 'line-scale-party',
      bdColor: 'rgba(2555,2555,255,0)',
      color: '#333333'
    });
    if (this.surveyForm.id == 0) {
      this.adminService.uploadSurveyForm(this.surveyForm, this.file).subscribe({
        next: _ => {
          this.spinnerService.hide('spinner-survey-form-detail');
          this.formUpdated = true;
          this.bsModalRef.hide();
          this.toastr.success('Survey form uploaded successfully');
        },
        error: error => {
          this.spinnerService.hide('spinner-survey-form-detail');
          let errorMsg = error[0];
          if(errorMsg == null){
            errorMsg = error.message;
          } 
          this.toastr.error(errorMsg);
        }
      });
    }
    else{
      this.adminService.updateSurveyForm(this.surveyForm).subscribe({
        next: _ => {
          this.spinnerService.hide('spinner-survey-form-detail');
          this.formUpdated = true;
          this.bsModalRef.hide();
          this.toastr.success('Survey form updated successfully');
        },
        error: error => {
          this.spinnerService.hide('spinner-survey-form-detail');
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
