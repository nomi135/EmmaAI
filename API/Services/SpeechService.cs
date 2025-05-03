using API.Interfaces;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Text.RegularExpressions;

namespace API.Services
{
    public class SpeechService(IConfiguration config) : ISpeechService
    {
        private readonly string subscriptionKey = config.GetValue<string>("AzureOpenAI:ApiKey") ?? throw new Exception("AzureOpenAI API key not found");
        private readonly string region = config.GetValue<string>("AzureOpenAI:Region") ?? throw new Exception("AzureOpenAI region not found");
        public async Task<bool> TextToSpeechAsync(string text, string outputFilePath)
        {
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);
            //config.SpeechSynthesisVoiceName = "en-US-JennyMultilingualNeural"; // You can change voice
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

            using var audioConfig = AudioConfig.FromWavFileOutput(outputFilePath);
            using var synthesizer = new SpeechSynthesizer(config, audioConfig);

            // Sanitize the text to remove emojis and other unsupported characters
            text = SanitizeForTTS(text);
            // Set the SSML text to be synthesized
            var ssml = $@"
            <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis'
                xmlns:mstts='http://www.w3.org/2001/mstts'
                xml:lang='en-US'>
            <voice name='en-US-JennyMultilingualNeural'>
            <mstts:express-as style='affectionate' styledegree='2'>
                {text}
            </mstts:express-as>
            </voice>
            </speak>";

            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"TTS succeeded, saved to: {outputFilePath}");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"TTS failed: {cancellation.Reason}, {cancellation.ErrorDetails}");
                return false;
            }

            return true;
        }

        private static string SanitizeForTTS(string input)
        {
            return Regex.Replace(input, @"[^\u0000-\u007F]+", string.Empty); // Removes emojis
        }
    }
}
