using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using iText.Forms.Fields;
using iText.Forms;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf.Canvas.Parser;
using API.Helpers;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Font;

namespace API.Controllers
{
    [Authorize]
    public class SurveyController(IUnitOfWork unitOfWork, IMapper mapper) : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<List<SurveyFormDto>>> Index()
        {
            var SurveyForms = await unitOfWork.SurveyFormRepository.GetAllSurveyFormsAsync();

            if (SurveyForms == null || SurveyForms.Count == 0)
                return Ok();

            var uploadPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SurveyForm");
            string username = User.GetUsername()?.Replace(" ", "_") ?? "";

            foreach (var form in SurveyForms)
            {
                var filePath = form.Path;
                string originalFilePath = System.IO.Path.Combine(uploadPath, System.IO.Path.GetFileName(filePath));
                //create user folder
                var userFolder = System.IO.Path.Combine(uploadPath, username);
                // Generate user specific filled file path
                string filledFilePath = System.IO.Path.Combine(userFolder, System.IO.Path.GetFileName(filePath));
                if (System.IO.File.Exists(filledFilePath))
                {
                    var uri = new Uri(filePath);
                    var segments = uri.Segments.ToList();

                    // segments will be: ["/", "api/", "SurveyForm/", "ud100.pdf"]

                    // Insert username before the file name
                    segments.Insert(segments.Count - 1, $"{username}/");

                    // Rebuild the new path
                    string newPath = string.Join("", segments);

                    // Combine with original scheme and host
                    string newUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}{newPath}";
                    form.ClientPath = newUrl;
                }
            }

            return Ok(SurveyForms);
        }

        [HttpPost("submit-survey-data")]
        public async Task<ActionResult> SubmitSurveyData([FromBody] List<SurveyFormDataDto> surveyFormDataDto)
        {
            if (surveyFormDataDto == null || surveyFormDataDto.Count == 0)
                return BadRequest("Survey form data is required");

            foreach (var data in surveyFormDataDto)
            {
                data.UserName = User.GetUsername();
                data.UserId = User.GetUserId();
            }

            //get surveyForm
            var surveyForms = await unitOfWork.SurveyFormRepository.GetAllSurveyFormsAsync();
            var surveyForm = surveyForms.Where(s => s.SurveyFormDetails.Any(d => d.Id == surveyFormDataDto.FirstOrDefault().SurveyFormDetailId)).FirstOrDefault();

            //fill survey form pdf
            foreach (var data in surveyFormDataDto)
            {
                var surveyFormDetail = surveyForm.SurveyFormDetails.FirstOrDefault(s => s.Id == data.SurveyFormDetailId);
                if (surveyFormDetail != null)
                    data.SurveyFormDetail = surveyFormDetail;
            }

            string path = FillSurveyFormData(surveyForm.Path, surveyFormDataDto);

            // Update the path in the DTOs
            foreach (var data in surveyFormDataDto)
            {
                data.Path = path;
            }

            var surveyFormData = mapper.Map<List<SurveyFormData>>(surveyFormDataDto);
            unitOfWork.SurveyFormDataRepository.AddSurveyFormData(surveyFormData);
            if (await unitOfWork.Complete())
                return NoContent();

            return BadRequest("Failed to submit survey form data");
        }

        [HttpGet("get-survey-form-data/{surveyFormId}")]
        public async Task<ActionResult<List<SurveyFormDataDto>>> GetSurveyFormData(int surveyFormId)
        {
            var surveyFormData = await unitOfWork.SurveyFormDataRepository.GetSurveyFormDataAsync(surveyFormId, User.GetUserId());

            if (surveyFormData == null)
                return NotFound("No survey form data found for the specified survey form ID");

            return Ok(surveyFormData);
        }

        [HttpPut("update-survey-form-data")]
        public async Task<ActionResult> UpdateSurveyFormData([FromBody] List<SurveyFormDataDto> surveyFormDataDto)
        {
            if (surveyFormDataDto == null || surveyFormDataDto.Count == 0)
                return BadRequest("Survey form data is required");

            foreach (var data in surveyFormDataDto)
            {
                data.UserName = User.GetUsername();
                data.UserId = User.GetUserId();
            }

            //get surveyForm
            var surveyForms = await unitOfWork.SurveyFormRepository.GetAllSurveyFormsAsync();
            var surveyForm = surveyForms.Where(s => s.SurveyFormDetails.Any(d => d.Id == surveyFormDataDto.FirstOrDefault().SurveyFormDetailId)).FirstOrDefault();

            //fill survey form pdf
            foreach (var data in surveyFormDataDto)
            {
                var surveyFormDetail = surveyForm.SurveyFormDetails.FirstOrDefault(s => s.Id == data.SurveyFormDetailId);
                if (surveyFormDetail != null)
                    data.SurveyFormDetail = surveyFormDetail;
            }
            string path = FillSurveyFormData(surveyForm.Path, surveyFormDataDto);

            // Update the path in the DTOs
            foreach (var data in surveyFormDataDto)
            {
                data.Path = path;
            }

            var surveyFormData = mapper.Map<List<SurveyFormData>>(surveyFormDataDto);
            unitOfWork.SurveyFormDataRepository.UpdateSurveyFormData(surveyFormData);
            if (await unitOfWork.Complete())
                return NoContent();

            return BadRequest("Failed to update survey form data");
        }

        private static string FillSurveyFormData(string filePath, List<SurveyFormDataDto> formDataDto)
        {
            var uploadPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SurveyForm");
            string originalFilePath = System.IO.Path.Combine(uploadPath, System.IO.Path.GetFileName(filePath));
            string username = formDataDto.FirstOrDefault().UserName.Replace(" ", "_");
            //create user folder
            var userFolder = System.IO.Path.Combine(uploadPath, username);
            if (!Directory.Exists(userFolder))
                Directory.CreateDirectory(userFolder);

            // Generate user specific filled file path
            string filledFilePath = System.IO.Path.Combine(userFolder, System.IO.Path.GetFileName(filePath));
            // delete file if already exists
            if (System.IO.File.Exists(filledFilePath))
                System.IO.File.Delete(filledFilePath);

            // Use FileStreams manually
            using var input = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read);
            using var output = new FileStream(filledFilePath, FileMode.Create, FileAccess.Write);
            using var reader = new PdfReader(input);
            using var writer = new PdfWriter(output);
            using (PdfDocument document = new PdfDocument(reader, writer))
            {
                // Get the AcroForm from the document
                var form = PdfAcroForm.GetAcroForm(document, false); // create if not found

                if (form != null && form.GetFormFields().Count > 0)
                {
                    // Extract form fields
                    foreach (var fieldEntry in form.GetFormFields())
                    {
                        var field = fieldEntry.Value;
                        var fieldType = field.GetFormType()?.ToString().Replace("/", "");

                        // Only handle fillable field types
                        if (fieldType is not ("Tx" or "Ch" or "Btn"))
                            continue;

                        // Determine type
                        string type = fieldType switch
                        {
                            "Tx" => (field is PdfTextFormField t && t.IsMultiline()) ? "textarea" : "text",
                            "Ch" => "select",
                            "Btn" => PdfUtilityFunction.GetButtonType((PdfButtonFormField)field),
                            _ => "unknown"
                        };

                        // Skip buttons (submit/reset/etc)
                        if (type == "button") continue;

                        //get survey form data based on field key
                        var data = formDataDto.Where(f => f.SurveyFormDetail.Key == fieldEntry.Key);
                        if (data.Any())
                        {
                            string value = data.FirstOrDefault().Value ?? string.Empty;
                            if (type == "select")
                            {
                                // For select fields, set the value directly
                                ((PdfChoiceFormField)field).SetValue(value);
                            }
                            else if (type == "textarea" || type == "text")
                            {
                                // For text and textarea fields, set the value directly
                                ((PdfTextFormField)field).SetValue(value);
                            }
                            else if (type == "radio" || type == "checkbox")
                            {
                                // For radio and checkbox fields, set the value as checked
                                if (value.Equals("checked", StringComparison.OrdinalIgnoreCase))
                                    fieldEntry.Value.SetValue("Yes"); // Assuming "Yes" is the value for checked state
                                else
                                    fieldEntry.Value.SetValue("Off"); // Assuming "Off" is the value for unchecked state
                            }
                        }
                    }
                }
                // If no form fields, extract text from each page
                else
                {
                    int pageNo = 0;
                    for (int i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        pageNo = i;
                        var page = document.GetPage(i);
                        var strategy = new ChunkTrackingStrategy();
                        PdfTextExtractor.GetTextFromPage(page, strategy);

                        // Find and replace the tag
                        foreach (var data in formDataDto.OrderBy(a => a.SurveyFormDetailId))
                        {
                            var surveyFormDetail = data.SurveyFormDetail;
                            var canvas = new PdfCanvas(page);
                            int occurance = 0;
                            foreach (var item in formDataDto.OrderBy(a=>a.SurveyFormDetailId).Where(a => a.SurveyFormDetail.Key == surveyFormDetail.Key && pageNo.ToString() == surveyFormDetail.PageNo && a.SurveyFormDetail.PageNo == surveyFormDetail.PageNo))
                            {
                                ++occurance;
                                if (item.SurveyFormDetailId == surveyFormDetail.Id)
                                    break;
                            }
                            
                            PdfFont? fontName;
                            float? fontSize;
                            iText.Kernel.Geom.Rectangle rect = PdfUtilityFunction.GetTextCoordinates(page, surveyFormDetail.Key ?? string.Empty, occurance,
                                out fontName, out fontSize);
                            canvas.BeginMarkedContent(PdfName.Span);
                            if (!surveyFormDetail.Key.Contains("__"))
                                canvas.SaveState()
                                      .SetFillColorRgb(1f, 1f, 1f)
                                      .Rectangle(rect)
                                      .Fill()
                                      .RestoreState();
                            canvas.EndMarkedContent();
                            if (fontName != null)
                            {
                                // Write replacement
                                canvas.SaveState()
                                      .BeginText()
                                      .SetFontAndSize(fontName, fontSize ?? 0)
                                      .MoveText(rect.GetX(), rect.GetY() + 2)
                                      .ShowText(data.Value)
                                      .EndText()
                                      .RestoreState();
                            }
                        }
                    }
                }
                //form.FlattenFields(); // Optional: make fields non-editable
                document.Close();
            }
            writer.Close();
            reader.Close();
            output.Close();
            input.Close();
            //construct filled file path as absolute path from file path
            var uri = new Uri(filePath);
            var segments = uri.Segments.ToList();

            // segments will be: ["/", "api/", "SurveyForm/", "ud100.pdf"]

            // Insert username before the file name
            segments.Insert(segments.Count - 1, $"{username}/");

            // Rebuild the new path
            string newPath = string.Join("", segments);

            // Combine with original scheme and host
            string newUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}{newPath}";
            return newUrl;
        }
    }
}
