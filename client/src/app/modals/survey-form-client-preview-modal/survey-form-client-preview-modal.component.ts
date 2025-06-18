import { Component, ElementRef, inject, Input, OnInit, ViewChild } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { SurveyFormDetail } from '../../_models/survey-form-detail';

@Component({
  selector: 'app-survey-form-client-preview-modal',
  standalone: true,
  imports: [],
  templateUrl: './survey-form-client-preview-modal.component.html',
  styleUrl: './survey-form-client-preview-modal.component.css'
})
export class SurveyFormClientPreviewModalComponent implements OnInit {
  @Input() title: string = '';
  @Input() imageUrl: string = '';
  @Input() pageNo: string = '';
  @Input() surveyFormDetails: SurveyFormDetail[] = [];
  bsPreviewModalRef = inject(BsModalRef);

  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  ngOnInit(): void {
    if (this.imageUrl) {
      this.loadImageOnCanvas(this.imageUrl);
    }
  }

  loadImageOnCanvas(imageUrl: string) {
    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');

    if (!ctx) return;

    // Step 1: Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Step 2: Load the image
    const image = new Image();
    image.src = imageUrl;
    image.onload = () => {
      // Step 3: Resize canvas to image size
      canvas.width = image.width;
      canvas.height = image.height;

      // Step 4: Draw the original image
      ctx.drawImage(image, 0, 0);

       for (const detail of this.surveyFormDetails.filter(a=>a.pageNo == this.pageNo)) {
        if(detail.value != null) {
          const x = Number(detail.left);
          const y = canvas.height - Number(detail.top) - Number(detail.height);
          const width = Number(detail.width);
          const height = Number(detail.height);
          
          // Step 5: Erase old text (draw white box over it)
          ctx.fillStyle = 'white'; // or match background color
          ctx.fillRect(x, y, width, height);

          // Step 6: Draw new text
          if (detail.type === 'text') {
            ctx.font = `${detail.fontSize} ${detail.fontName}`;
            ctx.fillStyle = 'black';
            ctx.textBaseline = 'top';
            ctx.fillText(detail.value, x, y);
          } 
          else if ((detail.type === 'checkbox' || detail.type === 'radio') && (detail.value === 'checked')) {
            // Use ✓ symbol to draw a checkmark or dot
            //ctx.font = `${Math.min(width, height) * 0.8}px Arial`; // Scale to fit box
            ctx.font = '12px Arial';
            ctx.fillStyle = 'black';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(detail.type === 'checkbox' ? '✓' : '●', x + width / 2, y + height / 2);
          }
        }
      }
    }
  }
}
