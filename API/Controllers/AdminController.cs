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
using iText.Kernel.Geom;
using iText.Kernel.Font;

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
            var uploadPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SurveyForm");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Generate unique file name
            var uniqueFileName = $"{System.IO.Path.GetFileName(file.FileName.Replace(" ", "_"))}";
            var filePath = System.IO.Path.Combine(uploadPath, uniqueFileName);

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
                                var label = PdfUtilityFunction.GenerateLabelFromKey(rawKey);
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
                                        "Btn" => PdfUtilityFunction.GetButtonType((PdfButtonFormField)field),
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
                            var promptFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Prompts", "SurveyFormPrompt.txt");
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
                                            PdfPage page = document.GetPage(int.Parse(item.PageNo ?? "0"));
                                            int occurance = 0;
                                            foreach (var a in surveyFormDetails.Where(a => a.Key == item.Key && a.PageNo == item.PageNo))
                                            {
                                                ++occurance;
                                                if (a.Id == item.Id)
                                                    break;
                                            }
                                            if (occurance == 0)
                                                occurance = 1;
                                            PdfFont? fontName;
                                            float? fontSize;
                                            iText.Kernel.Geom.Rectangle rect = PdfUtilityFunction.GetTextCoordinates(page, item.Key ?? "", occurance, out fontName, out fontSize);
                                            surveyFormDetails.Add(new SurveyFormDetailDto()
                                            {
                                                Id = ++id,
                                                Key = item.Key,
                                                Label = item.Label,
                                                Type = item.Type,
                                                PageNo = item.PageNo,
                                                Top = rect.GetY().ToString(),//item.Top,
                                                Left = rect.GetX().ToString(),//item.Left,
                                                Width = rect.GetWidth().ToString(),//item.Width,
                                                Height = rect.GetHeight().ToString(),//item.Height,
                                                FontName = fontName?.GetFontProgram().GetFontNames().GetFontName() ?? "Unknown",//item.FontName,
                                                FontSize = fontSize.ToString(),//item.FontSize
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

        [HttpGet("get-text-coordinates")]
        public async Task<ActionResult<TextCoorinatesDto>> GetTextCoordinates([FromQuery] string filePath, [FromQuery] string pageNo, [FromQuery] string key, [FromQuery] int occurrence)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(pageNo) || string.IsNullOrEmpty(key))
            {
                return BadRequest("File path, page number, and key are required.");
            }
            TextCoorinatesDto coorinatesDto;
            var uploadPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SurveyForm");
            string originalFilePath = System.IO.Path.Combine(uploadPath, System.IO.Path.GetFileName(filePath));
            using (var stream = System.IO.File.OpenRead(originalFilePath))
            {
                using (PdfReader reader = new PdfReader(stream))
                {
                    using (PdfDocument document = new PdfDocument(reader))
                    {
                        PdfPage page = document.GetPage(int.Parse(pageNo ?? "0"));
                        PdfFont? fontName;
                        float? fontSize;
                        iText.Kernel.Geom.Rectangle rectangle = await Task.FromResult(PdfUtilityFunction.GetTextCoordinates(page, key, occurrence, out fontName, out fontSize));
                        coorinatesDto = new TextCoorinatesDto()
                        {
                            X = rectangle.GetX().ToString(),
                            Y = rectangle.GetY().ToString(),
                            Width = rectangle.GetWidth().ToString(),
                            Height = rectangle.GetHeight().ToString(),
                            FontName = fontName?.GetFontProgram().GetFontNames().GetFontName() ?? "Unknown",
                            FontSize = fontSize?.ToString() ?? "0"
                        };
                        document.Close();
                    }
                    reader.Close();
                }
                stream.Close();
            }
            return Ok(coorinatesDto);
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

    }
}
