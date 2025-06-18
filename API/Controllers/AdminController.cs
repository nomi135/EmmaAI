using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Forms.Fields;
using iText.Forms;
using iText.Kernel.Pdf.Annot;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using API.Helpers;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork, Kernel kernel, IMapper mapper) : BaseApiController
    {
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await userManager.Users
                .OrderBy(x => x.UserName)
                .Select(x => new
                {
                    x.Id,
                    userName = x.UserName,
                    Roles = x.UserRoles.Select(r => r.Role.Name)
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            if (string.IsNullOrEmpty(roles)) return BadRequest("You must select at least one role");

            var selectedRoles = roles.Split(",").ToArray();

            var user = await userManager.FindByNameAsync(username);

            if (user == null) return BadRequest("User not found");

            var userRoles = await userManager.GetRolesAsync(user);

            var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded) return BadRequest("Failed to add to roles");

            result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded) return BadRequest("Failed to remove from roles");

            return Ok(await userManager.GetRolesAsync(user));
        }

        [HttpGet("survey-forms")]
        public async Task<ActionResult<List<SurveyFormDto>>> GetSurveyForms()
        {
            var SurveyForms = await unitOfWork.SurveyFormRepository.GetAllSurveyFormsAsync();

            if (SurveyForms == null || SurveyForms.Count == 0)
                return Ok();

            return Ok(SurveyForms);
        }

        [HttpPost("upload-survey-form")]
        public async Task<ActionResult> AddSurveyForm([FromForm] string suveyFormJson, [FromForm] IFormFile? file)
        {
            if (file == null)
            {
                return BadRequest("No survey form selected to upload.");
            }

            SurveyFormDto surveyFormDto = JsonSerializer.Deserialize<SurveyFormDto>(suveyFormJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            //check if survey form title already exist
            bool titleExist = await unitOfWork.SurveyFormRepository.CheckSurveyFormByTitleAsync(0, surveyFormDto.Title);

            if (titleExist)
            {
                return BadRequest("Survey form with this title already exists.");
            }

            // Ensure folder exists
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SurveyForm");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Generate unique file name
            var uniqueFileName = $"{Path.GetFileName(file.FileName.Replace(" ", "_"))}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Build the relative URL path
            surveyFormDto.Path = $"{Request.Scheme}://{Request.Host}/SurveyForm/{uniqueFileName}";

            /*****************************************/
            //generate images for each page
            var baseUrl = $"{Request.Scheme}://{Request.Host}/SurveyForm";
            var imageUrls = await PdfImageGenerator.GeneratePdfPageImagesAsync(filePath, baseUrl);
            surveyFormDto.ImagePath = string.Join(",", imageUrls);
            /*****************************************/

            var surveyForm = mapper.Map<SurveyForm>(surveyFormDto);

            //set the id column for surveyform details to null
            foreach (var detail in surveyForm.SurveyFormDetails)
            {
                detail.Id = 0; // Reset ID to 0 for new entries
            }

            unitOfWork.SurveyFormRepository.AddSurveyForm(surveyForm);
            if (await unitOfWork.Complete())
                return NoContent();

            return BadRequest("Failed to upload survey form");
        }

        [HttpPut("update-survey-form")]
        public async Task<ActionResult> UpdateSurveyForm([FromBody] SurveyFormDto surveyFormDto)
        {
            //check if survey form title already exist
            bool titleExist = await unitOfWork.SurveyFormRepository.CheckSurveyFormByTitleAsync(surveyFormDto.Id, surveyFormDto.Title);

            if (titleExist)
            {
                return BadRequest("Survey form with this title already exists.");
            }

            var surveyForm = await unitOfWork.SurveyFormRepository.GetSurveyFormAsync(surveyFormDto.Id);

            surveyForm.Title = surveyFormDto.Title;
            surveyForm.SurveyFormDetails.Clear();
            foreach (var dto in surveyFormDto.SurveyFormDetails)
            {
                surveyForm.SurveyFormDetails.Add(mapper.Map<SurveyFormDetail>(dto));
            }

            unitOfWork.SurveyFormRepository.UpdateSurveyForm(surveyForm);
            if (await unitOfWork.Complete())
                return NoContent();

            return BadRequest("Failed to update survey form");
        }

        [HttpPost("extract-survey-form-data")]
        public async Task<ActionResult<List<SurveyFormDetailDto>>> ExtractSurveyFormData([FromForm] IFormFile? file)
        {
            if (file == null)
            {
                return BadRequest("No survey form selected.");
            }

            List<SurveyFormDetailDto> surveyFormDetails = new List<SurveyFormDetailDto>();

            //unique id
            int id = 0;
            // 1. Read raw text from PDF or if pdf have form then extract all form fields
            var extractedText = new StringBuilder();

            using (var stream = file.OpenReadStream())
            {
                using (PdfReader reader = new PdfReader(stream))
                {
                    using (PdfDocument document = new PdfDocument(reader))
                    {
                        // Get the AcroForm
                        var form = PdfAcroForm.GetAcroForm(document, false);

                        if (form != null && form.GetFormFields().Count > 0)
                        {
                            // Extract form fields
                            foreach (var fieldEntry in form.GetFormFields())
                            {
                                var rawKey = fieldEntry.Key;
                                var label = GenerateLabelFromKey(rawKey);
                                var field = fieldEntry.Value;
                                var fieldType = field.GetFormType()?.ToString().Replace("/", "");

                                // Only handle fillable field types
                                if (fieldType is not ("Tx" or "Ch" or "Btn"))
                                    continue;

                                // Get all widgets (could be multiple, especially for radio)
                                IList<PdfWidgetAnnotation> widgets = field.GetWidgets(); // now available in all field types

                                foreach (var widget in widgets)
                                {
                                    var rect = widget.GetRectangle().ToRectangle();
                                    var pageNo = document.GetPageNumber(widget.GetPage());

                                    // Extract font info from Default Appearance string (DA)
                                    string fontName = "Unknown";
                                    float fontSize = 0;
                                    var da = field.GetPdfObject().GetAsString(PdfName.DA);
                                    if (da != null)
                                    {
                                        var daParts = da.ToString().Split(' ');
                                        if (daParts.Length >= 2)
                                        {
                                            fontName = daParts[0].TrimStart('/');
                                            float.TryParse(daParts[1], out fontSize);
                                        }
                                    }

                                    // Determine type
                                    string type = fieldType switch
                                    {
                                        "Tx" => (field is PdfTextFormField t && t.IsMultiline()) ? "textarea" : "text",
                                        "Ch" => "select",
                                        "Btn" => GetButtonType((PdfButtonFormField)field),
                                        _ => "unknown"
                                    };

                                    // Skip buttons (submit/reset/etc)
                                    if (type == "button") continue;

                                    surveyFormDetails.Add(new SurveyFormDetailDto
                                    {
                                        Key = fieldEntry.Key,
                                        Label = label,
                                        Type = type,
                                        Left = rect.GetX().ToString(),
                                        Top = rect.GetY().ToString(),
                                        Width = rect.GetWidth().ToString(),
                                        Height = rect.GetHeight().ToString(),
                                        PageNo = pageNo.ToString(),
                                        FontName = fontName,
                                        FontSize = fontSize.ToString()
                                    });
                                }
                            }
                        }
                        // If no form fields, extract text from each page
                        else
                        {
                            for (int page = 1; page <= document.GetNumberOfPages(); page++)
                            {
                                var pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(page));
                                if (!string.IsNullOrWhiteSpace(pageText))
                                {
                                    extractedText.AppendLine(pageText);
                                }
                            }
                            // 2. extract the prompt template to pass to LLM
                            var promptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", "SurveyFormPrompt.txt");
                            var promptTemplate = await System.IO.File.ReadAllTextAsync(promptFilePath);

                            // Replace {extractedText} placeholder dynamically
                            var prompt = promptTemplate.Replace("{extractedText}", extractedText.ToString());

                            // 3. Use Semantic Kernel to call OpenAI with the prompt
                            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
                            if (chatCompletionService != null)
                            {
                                var chatMessageContent = await chatCompletionService.GetChatMessageContentsAsync(prompt);

                                // Use LINQ to get the first TextContent item
                                var textItem = chatMessageContent
                                    .SelectMany(a => a.Items)
                                    .OfType<TextContent>()  // Ensure we're only selecting TextContent
                                    .FirstOrDefault();
                                if (textItem != null && !string.IsNullOrEmpty(textItem.Text))
                                {
                                    // 4. Deserialize the JSON response into List<KeyValuePair<string, string>>
                                    var parsed = JsonSerializer.Deserialize<List<SurveyFormDetailDto>>(textItem.Text, new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });

                                    if (parsed != null)
                                    {
                                        foreach (var item in parsed)
                                        {
                                            surveyFormDetails.Add(new SurveyFormDetailDto()
                                            {
                                                Id = ++id,
                                                Key = item.Key,
                                                Label = item.Label,
                                                Type = item.Type,
                                                PageNo = item.PageNo,
                                                Top = item.Top,
                                                Left = item.Left,
                                                Width = item.Width,
                                                Height = item.Height,
                                                FontName = item.FontName,
                                                FontSize = item.FontSize
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        document.Close();
                    }
                    reader.Close();
                }
                stream.Close();
            }

            return Ok(surveyFormDetails);
        }

        [HttpDelete("delete-survey-form/{id}")]
        public async Task<ActionResult> DeleteSurveyForm(int id)
        {
            var surveyForm = await unitOfWork.SurveyFormRepository.GetSurveyFormAsync(id);

            if (surveyForm == null) return BadRequest("Cannot delete this survey form");

            unitOfWork.SurveyFormRepository.DeleteSurveyForm(surveyForm);

            if (await unitOfWork.Complete()) return Ok();

            return BadRequest("Problem deleting the survey form");
        }

        [HttpGet("get-completed-survey-forms/{surveyFormId}")]
        public async Task<ActionResult<List<SurveyFormDataDto>>> GetCompletedSurveyForms(int surveyFormId)
        {
            var surveyFormData = await unitOfWork.SurveyFormDataRepository.GetSurveyFormDataAsync(surveyFormId, null);

            if (surveyFormData == null)
                return NotFound("No completed survey found for this survey form");

            if (surveyFormData.Count > 0)
            {
                surveyFormData = surveyFormData.GroupBy(s => s.UserId)
                    .Select(g => g.First())
                    .ToList();
            }

            return Ok(surveyFormData);
        }

        private static string GetButtonType(PdfButtonFormField btn)
        {
            var ff = btn.GetPdfObject().GetAsNumber(PdfName.Ff);
            long fieldFlags = ff != null ? ff.LongValue() : 0;

            const int RADIO_FLAG = 1 << 15;
            const int PUSHBUTTON_FLAG = 1 << 16;

            if ((fieldFlags & PUSHBUTTON_FLAG) != 0)
                return "button";

            if ((fieldFlags & RADIO_FLAG) != 0)
                return "radio";

            return "checkbox";
        }

        private static string GenerateLabelFromKey(string rawKey)
        {
            // Step 1: Split by dot
            var segments = rawKey.Split('.');

            // Step 2: Clean each part by removing [index]
            var cleaned = segments
                .Select(s =>
                {
                    var match = Regex.Match(s, @"^([^\[]+)(?:\[(\d+)\])?$");
                    var name = match.Groups[1].Value;
                    var index = match.Groups[2].Success ? $"_{match.Groups[2].Value}" : "";
                    return (Name: name, Index: index);
                })
                .ToList();

            // Step 3: Take the last two parts
            if (cleaned.Count < 2) return ToLabel(cleaned.Last().Name + cleaned.Last().Index);

            var secondLast = cleaned[^2];
            var last = cleaned[^1];

            // Step 4: Format
            var label = $"{ToLabel(secondLast.Name)}: {ToLabel(last.Name)}{last.Index}";
            return label;
        }

        private static string ToLabel(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            input = input.Replace("_", " ");

            // Add space before capital letters
            input = Regex.Replace(input, @"(?<!^)([A-Z])", " $1");

            // Collapse multiple spaces
            return Regex.Replace(input, @"\s+", " ").Trim();
        }

    }
}
