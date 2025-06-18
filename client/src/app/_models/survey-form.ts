import { SurveyFormDetail } from "./survey-form-detail";

export interface SurveyForm {
    id: number
    title: string;
    path: string;
    imagePath: string;
    created: Date;
    surveyFormDetails: SurveyFormDetail[];
}
