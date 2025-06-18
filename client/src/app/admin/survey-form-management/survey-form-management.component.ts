import { Component, inject, OnInit } from '@angular/core';
import { NgxSpinnerModule, NgxSpinnerService } from 'ngx-spinner';
import { AdminService } from '../../_services/admin.service';
import { ToastrService } from 'ngx-toastr';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { SurveyForm } from '../../_models/survey-form';
import { DatePipe } from '@angular/common';
import { SurveyFormModalComponent } from '../../modals/survey-form-modal/survey-form-modal.component';
import { SurveyFormCompletedModalComponent } from '../../modals/survey-form-completed-modal/survey-form-completed-modal.component';

@Component({
  selector: 'app-survey-form-management',
  standalone: true,
  imports: [NgxSpinnerModule, DatePipe],
  templateUrl: './survey-form-management.component.html',
  styleUrl: './survey-form-management.component.css'
})
export class SurveyFormManagementComponent implements OnInit {
  private adminService = inject(AdminService);
  private toastr = inject(ToastrService);
  private spinnerService = inject(NgxSpinnerService);
  surveyForms: SurveyForm[] = [];
  private modalService = inject(BsModalService);
  bsModalRef: BsModalRef<SurveyFormModalComponent> = new BsModalRef<SurveyFormModalComponent>();
  completedBsModalRef: BsModalRef<SurveyFormCompletedModalComponent> = new BsModalRef<SurveyFormCompletedModalComponent>();

  ngOnInit(): void {
      this.getSurveyForms();
    }
  
    openCompletedFormModal(id: number, title: string) {
      const initialState: ModalOptions = {
        class: 'modal-lg',
        initialState: {
          title: 'Completed survey forms : ' + title,
          id: id
        }
      }
  
      this.completedBsModalRef = this.modalService.show(SurveyFormCompletedModalComponent, initialState);
    }

    openSurveyFormModal(surveyForm: SurveyForm) {
      const initialState: ModalOptions = {
        class: 'modal-lg',
        initialState: {
          title: 'Update Survey Form',
          id: surveyForm.id,
          formTitle: surveyForm.title,
          formPath: surveyForm.path,
          surveyFormDetails: surveyForm.surveyFormDetails,
          formUpdated: false
        }
      }
  
      this.bsModalRef = this.modalService.show(SurveyFormModalComponent, initialState);
      this.bsModalRef.onHide?.subscribe({
        next: () => {
          if (this.bsModalRef.content && this.bsModalRef.content.formUpdated) {
            const selectedRoles = this.bsModalRef.content.formUpdated;
            this.getSurveyForms();
          }
        }
      })
    }
  
    getSurveyForms() {
      this.spinnerService.show('spinner-survey-form', {
        type: 'line-scale-party',
        bdColor: 'rgba(2555,2555,255,0)',
        color: '#333333'
      });
      this.adminService.getSurveyForms().subscribe({
        next: surveyForms => {
          this.surveyForms = surveyForms;
          this.spinnerService.hide('spinner-survey-form');
        },
        error: error => {
          this.spinnerService.hide('spinner-survey-form');
          this.toastr.error(error.message);
        }
      })
    }

    uploadNewForm() {
      const initialState: ModalOptions = {
        class: 'modal-lg',
        initialState: {
          title: 'Upload New Survey Form',
          formUpdated: false
        }
      }
  
      this.bsModalRef = this.modalService.show(SurveyFormModalComponent, initialState);
      this.bsModalRef.onHide?.subscribe({
        next: () => {
          if (this.bsModalRef.content && this.bsModalRef.content.formUpdated) {
            const selectedRoles = this.bsModalRef.content.formUpdated;
            this.getSurveyForms();
          }
        }
      })
    }

    deleteSurveyForm(id: number) {
      const confirm = window.confirm('Are you sure you want to delete this survey form?');
      if (confirm) {
        this.spinnerService.show('spinner-survey-form', {
          type: 'line-scale-party',
          bdColor: 'rgba(2555,2555,255,0)',
          color: '#333333'
        });
        this.adminService.deleteSurveyForm(id).subscribe({
          next: _ => {
            this.spinnerService.hide('spinner-survey-form');
            this.toastr.success('Survey form deleted successfully');
            this.getSurveyForms();
          },
          error: error => {
            this.spinnerService.hide('spinner-survey-form');
            this.toastr.error(error.message);
          }
        })
      }
    }
  }
