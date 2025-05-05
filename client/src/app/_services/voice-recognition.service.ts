import { Injectable, NgZone } from '@angular/core';

declare global {
  interface Window {
    webkitSpeechRecognition: any;
    SpeechRecognition: any;
  }
}


@Injectable({
  providedIn: 'root'
})
export class VoiceRecognitionService {
  private recognition: any;
  private isListening = false;
  private transcript = '';
  private resolveTranscript!: (value: string) => void;
  private transcriptPromise: Promise<string> | null = null;

  constructor(private ngZone: NgZone) {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    this.recognition = new SpeechRecognition();
    this.recognition.lang = 'en-US';
    this.recognition.interimResults = false;
    this.recognition.maxAlternatives = 1;

    this.recognition.onresult = (event: any) => {
      const result = event.results[0][0].transcript;
      this.ngZone.run(() => {
        this.transcript = result;
      });
    };

    this.recognition.onend = () => {
      this.ngZone.run(() => {
        this.isListening = false;
        if (this.transcriptPromise && this.resolveTranscript) {
          this.resolveTranscript(this.transcript);
          this.transcriptPromise = null; // clear the promise
        }
      });
    };
  }

  start(): void {
    this.isListening = true;
    this.transcript = '';
    this.recognition.start();
  }

  stop(): Promise<string> {
    if (!this.isListening) return Promise.resolve('');
    this.transcriptPromise = new Promise<string>((resolve) => {
      this.resolveTranscript = resolve;
    });
    this.recognition.stop();
    return this.transcriptPromise;
  }

}
