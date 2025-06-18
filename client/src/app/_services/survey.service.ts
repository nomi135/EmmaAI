import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { SurveyForm } from '../_models/survey-form';
import { HttpClient } from '@angular/common/http';
import { SurveyFormData } from '../_models/survey-form-data';

@Injectable({
  providedIn: 'root'
})
export class SurveyService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);
  
  getSurveyForms() {
    return this.http.get<SurveyForm[]>(this.baseUrl + 'survey');
  }

  submitSurveyFormData(surveyFormData: SurveyFormData[]) {
    return this.http.post<SurveyFormData[]>(this.baseUrl + 'survey/submit-survey-data', surveyFormData);
  }

  getSurveyFormData(surveyFormId: number) {
    return this.http.get<SurveyFormData[]>(this.baseUrl + 'survey/get-survey-form-data/' + surveyFormId);
  }

  updateSurveyFormData(surveyFormData: SurveyFormData[]) {
    return this.http.put(this.baseUrl + 'survey/update-survey-form-data', surveyFormData);
  }
}
