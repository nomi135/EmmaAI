import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { User } from '../_models/user';
import { SurveyForm } from '../_models/survey-form';
import { SurveyFormDetail } from '../_models/survey-form-detail';
import { SurveyFormData } from '../_models/survey-form-data';
import { TextCoordinates } from '../_models/text-coordinates';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);

  getUserWithRoles() {
    return this.http.get<User[]>(this.baseUrl + 'admin/users-with-roles');
  }

  updateUserRoles(username: string, roles: string[]) {
    return this.http.post<string[]>(this.baseUrl + 'admin/edit-roles/' + username + '?roles=' + roles, {});
  }

  getSurveyForms() {
    return this.http.get<SurveyForm[]>(this.baseUrl + 'admin/survey-forms');
  }

  uploadSurveyForm(surveyForm: SurveyForm, file: File | null) {
    const formData = new FormData();
    formData.append('file', file!);
    formData.append('suveyFormJson', JSON.stringify(surveyForm));
    return this.http.post<SurveyForm>(this.baseUrl + 'admin/upload-survey-form', formData);
  }

  extractSurveyFormData(file: File | null) {
    const formData = new FormData();
    formData.append('file', file!);
    return this.http.post<SurveyFormDetail[]>(this.baseUrl + 'admin/extract-survey-form-data', formData);
  }

   updateSurveyForm(surveyForm: SurveyForm) {
    return this.http.put(this.baseUrl + 'admin/update-survey-form', surveyForm);
  }

  deleteSurveyForm(id: number) {
    return this.http.delete(this.baseUrl + 'admin/delete-survey-form/' + id);
  }

  getCompletedSurveyForms(id: number) {
    return this.http.get<SurveyFormData[]>(this.baseUrl + 'admin/get-completed-survey-forms/' + id);
  }

  getTextCoordinates(filePath: string, pageNo: string, key: string, occurrence: number) {
    return this.http.get<TextCoordinates>(this.baseUrl + 'admin/get-text-coordinates?filePath=' + filePath + '&pageNo=' + pageNo + 
      '&key=' + key + '&occurrence=' + occurrence);
  }
}

