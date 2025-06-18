import { Component } from '@angular/core';
import { TabDirective, TabsModule } from 'ngx-bootstrap/tabs';
import { UserManagementComponent } from "../user-management/user-management.component";
import { SurveyFormManagementComponent } from "../survey-form-management/survey-form-management.component";
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [TabsModule, UserManagementComponent, SurveyFormManagementComponent, NgIf],
  templateUrl: './admin-panel.component.html',
  styleUrl: './admin-panel.component.css'
})
export class AdminPanelComponent {
  activeTab?: TabDirective;

  onTabActivated(data: TabDirective) {
    this.activeTab = data;
  }
}
