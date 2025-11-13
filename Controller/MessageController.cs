using cliq.Api.Models.Messages;
using Cliq.Api.Attributes;
using Cliq.Api.Interface;
using Microsoft.AspNetCore.Mvc;
using Models.Account;
using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using PuppeteerSharp.Media;

namespace Cliq.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessageInterface _IMessageInterface;

        // Cache the Chromium browser download to avoid re-downloading on every request
        private static InstalledBrowser _cachedInstalledBrowser;

        public MessageController(IMessageInterface IMessageInterface)
        {
            _IMessageInterface = IMessageInterface;
        }

        // Get users
        [HttpGet("get-users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var result = await _IMessageInterface.GetUsersAsync();
                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpPost("send-text-message-bot")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var result = await _IMessageInterface.SendMessageAsync(request);
                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new { Message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpPost("send-file-to-user-by-zuid")]
        public async Task<IActionResult> UploadFile([FromForm] string zuid, IFormFile file, [FromForm] string comments)
        {
            try
            {

                if (string.IsNullOrEmpty(zuid))
                    return BadRequest(new { Error = "ZUID is required." });

                if (file == null || file.Length == 0)
                    return BadRequest(new { Error = "File is required." });

                var result = await _IMessageInterface.SendFileToUserByZuidAsync(file, zuid, comments);

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = "File uploaded successfully",
                    FileId = result.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }



        [HttpPost("send-text-message-to-user-by-zuid")]
        public async Task<IActionResult> SendTextMessage([FromQuery] string zuid, [FromBody] string message)
        {
            try
            {
                if (string.IsNullOrEmpty(zuid))
                    return BadRequest(new { Error = "ZUID is required." });

                if (string.IsNullOrEmpty(message))
                    return BadRequest(new { Error = "Message is required." });

                var result = await _IMessageInterface.SendTextMessageToUserByZuidAsync(message, zuid);

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = "Text message sent successfully",
                    Response = result.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpPost("send-html-to-file-by-zuid")]
        public async Task<IActionResult> SendHtmlToFile([FromForm] string zuid, [FromForm] string htmlCode, [FromForm] string format = "pdf", [FromForm] string comments = "")
        {
            try
            {
                if (string.IsNullOrEmpty(zuid))
                    return BadRequest(new { Error = "ZUID is required." });

                if (string.IsNullOrEmpty(htmlCode))
                    return BadRequest(new { Error = "HTML code is required." });

                if (string.IsNullOrEmpty(format) || !new[] { "pdf", "png", "jpg", "jpeg" }.Contains(format.ToLower()))
                    return BadRequest(new { Error = "Format must be 'pdf', 'png', 'jpg', or 'jpeg'." });

                byte[] fileBytes;
                string fileName;
                string contentType;

                if (format.ToLower() == "pdf")
                {
                    // Convert HTML to PDF
                    fileBytes = await ConvertHtmlToPdfAsync(htmlCode);
                    fileName = "html_document.pdf";
                    contentType = "application/pdf";
                }
                else
                {
                    // Convert HTML to image (PNG, JPG, JPEG)
                    fileBytes = await ConvertHtmlToImageAsync(htmlCode, format);
                    fileName = $"html_image.{format}";
                    contentType = format.ToLower() == "png" ? "image/png" : "image/jpeg";
                }

                if (fileBytes == null || fileBytes.Length == 0)
                    return BadRequest(new { Error = $"Failed to convert HTML to {format}." });

                // Create a temporary IFormFile from the file bytes
                var formFile = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "file", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = contentType
                };

                // Send the file to the user
                var result = await _IMessageInterface.SendFileToUserByZuidAsync(formFile, zuid, comments);

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = $"HTML converted to {format.ToUpper()} and sent successfully",
                    FileId = result.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }


        private async Task<byte[]> ConvertHtmlToPdfAsync(string htmlCode)
        {
            try
            {
                using var browser = await GetBrowserAsync();
                using var page = await browser.NewPageAsync();

                await page.SetContentAsync(htmlCode);

                var pdfOptions = new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true
                };

                return await page.PdfDataAsync(pdfOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting HTML to PDF: {ex.Message}");
                return null;
            }
        }


        private async Task<byte[]> ConvertHtmlToImageAsync(string htmlCode, string format)
        {
            try
            {
                using var browser = await GetBrowserAsync();
                using var page = await browser.NewPageAsync();

                // Set a Full HD viewport
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1920,
                    Height = 1080,
                    DeviceScaleFactor = 2 // for Retina / High DPI rendering
                });

                // Load your HTML content
                await page.SetContentAsync(htmlCode, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                });

                // Ensure fonts/images are fully loaded before capture
                await page.EvaluateFunctionAsync(@"() => new Promise(resolve => {
            if (document.fonts) {
                document.fonts.ready.then(resolve);
            } else {
                resolve();
            }
        })");

                // Define high-quality screenshot options
                var screenshotOptions = new ScreenshotOptions
                {
                    Type = format.ToLower() == "png" ? ScreenshotType.Png : ScreenshotType.Jpeg,
                    FullPage = true,
                    Quality = format.ToLower() == "png" ? null : 100 // JPEG only
                };

                // Take and return the screenshot as bytes
                return await page.ScreenshotDataAsync(screenshotOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting HTML to image: {ex.Message}");
                return null;
            }
        }

        private static async Task<IBrowser> GetBrowserAsync()
        {
            if (_cachedInstalledBrowser == null)
            {
                var fetcher = new BrowserFetcher();
                _cachedInstalledBrowser = await fetcher.DownloadAsync();
            }

            var executablePath = _cachedInstalledBrowser.GetExecutablePath();

            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });
        }


        // send voice allerts
        [HttpPost("send-voice-alert-to-user-by-zuid")]
        public async Task<IActionResult> SendVoiceAlert([FromBody] VoiceAlertMessageRequest request)
        {
            try
            {
                if (request.Zuids == null || !request.Zuids.Any())
                    return BadRequest(new { Error = "ZUIDDs are required." });

                if (string.IsNullOrEmpty(request.Message))
                    return BadRequest(new { Error = "Message is required." });

                var result = await _IMessageInterface.SendBotVoiceCallAsync(request.Message, request.Zuids);

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = "Voice alert sent successfully",
                    Response = result.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }





        [HttpPost("send-html-to-file-by-zuid-TNA")]
        public async Task<IActionResult> SendTnaHtmlToFile(string zuidORemail, [FromBody] TNACommentDto tNACommentDto)
        {
            try
            {
                if (string.IsNullOrEmpty(zuidORemail))
                    return BadRequest(new { Error = "ZUID is required." });

                var byteRes = await SendHtmlToImage(tNACommentDto);
                var bytes = (byte[])((FileContentResult)byteRes).FileContents;

                // Send the file to the user
                var result = await _IMessageInterface.SendFileToUserByZuid(bytes, tNACommentDto.JobNo, "image/png", zuidORemail, $"Recived From {tNACommentDto.JobNo} - {tNACommentDto.TaskName}");

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = $"HTML converted to image and sent successfully",
                    FileId = result.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }



        private async Task<IActionResult> SendHtmlToImage(TNACommentDto tNACommentDto)
        {
            try
            {


                var maincontent = $@"<div class='command-card'>
                                        <div class='command-header'>
                                        <div class='d-flex'>
                                            <h2><i class='bi bi-gear-fill'></i> {tNACommentDto.JobNo}</h2>
                                            <div class='status-badge ms-2'>{tNACommentDto.TaskName}</div>
                                        </div>
                                        <div class='command-time'><i class='bi bi-clock-fill'></i> {tNACommentDto.CreatedTime}</div>
                                        </div>

                                        <div class='command-body'>
                                        <div class='detail'>
                                            <label><i class='bi bi-upc-scan'></i> Job No:</label>
                                            <span>{tNACommentDto.JobNo}</span>
                                        </div>

                                        <div class='detail'>
                                            <label><i class='bi bi-grid-3x3-gap-fill'></i> Category:</label>
                                            <span>{tNACommentDto.CommentGroup}</span>
                                        </div>

                                        <div class='detail'>
                                            <label><i class='bi bi-terminal'></i> Command:</label>
                                            <span>
                                                {tNACommentDto.CommentText}
                                            </span>
                                        </div>

                                        <div class='detail'>
                                            <label><i class='bi bi-person-badge-fill'></i> Command By:</label>
                                            <span>{tNACommentDto.UserName}</span>
                                        </div>
                                        </div>
                                    </div>";


                var bytes = await ConvertHtmlToImageBytesTNA(maincontent);

                return File(bytes, "image/png", "converted_image.png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }



        private static async Task<byte[]> ConvertHtmlToImageBytesTNA(string html)
        {
            var htmlContent = html.Replace("\\n", "\n");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            try
            {
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });

                using var page = await browser.NewPageAsync();

                // ✅ Set a minimal viewport to fit content automatically
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 800,
                    Height = 600,
                    DeviceScaleFactor = 2 // makes image sharper (2x resolution)
                });

                // ✅ Load HTML content (with transparent background)
                string fullHtml = $@"
                                <!DOCTYPE html>
                                <html lang='en'>
                                <head>
                                <meta charset='UTF-8'>

                                 <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/css/bootstrap.min.css' rel='stylesheet' integrity='sha384-EVSTQN3/azprG1Anm3QDgpJLIm9Nao0Yz1ztcQTwFspd3yD65VohhpuuCOmLASjC' crossorigin='anonymous'>
                                <link href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css' rel='stylesheet'>
                                <style>
                               body {{
                                    margin: 0;
                                    padding: 0;
                                    background: #0d2015;
                                    display: flex;
                                    justify-content: center;
                                    align-items: center;
                                    min-height: 100vh;
                                    font-family: 'Gill Sans', 'Trebuchet MS', sans-serif !important;
                                }}

                                .command-card {{
                                    background: #133622;
                                    color: #edf1f6;
                                    border-radius: 14px;
                                    width: 480px;
                                    padding: 22px 28px;
                                    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.45);
                                    border: 1px solid rgba(255, 255, 255, 0.1);
                                }}

                                .command-header {{
                                    display: flex;
                                    justify-content: space-between;
                                    align-items: flex-start;
                                    margin-bottom: 16px;
                                }}

                                .command-header h2 {{
                                    font-size: 0.95rem;
                                    margin: 0;
                                    font-weight: 600;
                                    color: #edf1f6;
                                    display: flex;
                                    align-items: center;
                                    gap: 6px;
                                }}

                                .command-header h2 i {{
                                    color: #22b469;
                                }}

                                .status-badge {{
                                    background: #22b469;
                                    color: #fff;
                                    font-size: 0.7rem;
                                    padding: 3px 8px;
                                    border-radius: 6px;
                                    font-weight: 600;
                                    text-transform: uppercase;
                                }}

                                .command-time {{
                                    font-size: 0.75rem;
                                    color: #b9d3c2;
                                    display: flex;
                                    align-items: center;
                                    gap: 5px;
                                }}

                                .command-time i {{
                                    color: #22b469;
                                }}

                                .command-body {{
                                    border-top: 1px solid rgba(255, 255, 255, 0.12);
                                    padding-top: 12px;
                                }}

                                .detail {{
                                    margin-bottom: 10px;
                                    display: flex;
                                    justify-content: space-between;
                                    align-items: flex-start;
                                }}

                                .detail label {{
                                    color: #8fda91;
                                    font-weight: 600;
                                    font-size: 0.75rem;
                                    display: flex;
                                    align-items: center;
                                    gap: 5px;
                                    flex: 0 0 30%;
                                }}

                                .detail span {{
                                    flex: 1;
                                    color: #edf1f6;
                                    font-size: 0.75rem;
                                    line-height: 1.3rem;
                                    word-wrap: break-word;
                                }}

                                </style>
                               
                                </head>
                                <body>
                                {htmlContent}
                                </body>
                                </html>";

                await page.SetContentAsync(fullHtml, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Load }
                });

                // ✅ Wait for .command-card to render
                await page.WaitForSelectorAsync(".command-card");

                // ✅ Select the element
                var element = await page.QuerySelectorAsync(".command-card");

                if (element == null)
                    throw new Exception("Command card not found in HTML content.");

                // ✅ Screenshot only the element (no background or blank space)
                var screenshotBytes = await element.ScreenshotDataAsync(new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    OmitBackground = true // transparent background
                });

                return screenshotBytes;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to convert HTML to image: " + ex.Message, ex);
            }
        }




        [HttpPost("send-sisal-otp-html-to-file-by-zuid")]
        public async Task<IActionResult> SendOtpHtmlToFileByZuid(string zuidORemail, [FromBody] SISALApiRequestDto otp)
        {
            try
            {
                if (string.IsNullOrEmpty(zuidORemail))
                    return BadRequest(new { Error = "ZUID or Email is required." });

                // Convert the OTP card to image
                var imageBytes = await ConvertOtpHtmlToImage(otp);

                // Send image via Zoho Cliq
                var result = await _IMessageInterface.SendFileToUserByZuid(
                    fileBytes: imageBytes,
                    fileName: $"{otp.LoginUserName}_otp.png",
                    contentType: "image/png",
                    zuid: zuidORemail,
                    comments: $"OTP Notification for {otp.LoginUserName}"
                );

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = "✅ OTP image sent successfully!",
                    ZUID = zuidORemail
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Unexpected error: {ex.Message}" });
            }
        }

        private static async Task<byte[]> ConvertOtpHtmlToImage(SISALApiRequestDto otp)
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            try
            {
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });

                using var page = await browser.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 420,
                    Height = 900,
                    DeviceScaleFactor = 2
                });

                string htmlContent = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <!-- ✅ Bootstrap + Icons CDN -->
            <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/css/bootstrap.min.css' rel='stylesheet' integrity='sha384-EVSTQN3/azprG1Anm3QDgpJLIm9Nao0Yz1ztcQTwFspd3yD65VohhpuuCOmLASjC' crossorigin='anonymous'>
            <link href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css' rel='stylesheet'>

            <style>
                body {{
                    font-family: 'Poppins', sans-serif;
                    background: linear-gradient(135deg, #c2e9fb 0%, #a1c4fd 100%);
                    margin: 0;
                    padding: 30px;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    min-height: 100vh;
                }}
                .otp-card {{
                    width: 360px;
                    background: rgba(255,255,255,0.95);
                    border-radius: 16px;
                    box-shadow: 0 8px 30px rgba(0,0,0,0.1);
                    overflow: hidden;
                }}
                .card-header {{
                    background: linear-gradient(135deg, #4f46e5 0%, #9333ea 100%);
                    color: #fff;
                    text-align: center;
                    padding: 14px 10px;
                }}
                .card-header h1 {{
                    font-size: 20px;
                    margin: 0;
                }}
                .card-header p {{
                    font-size: 13px;
                    opacity: 0.9;
                    margin: 4px 0 0;
                }}
                .card-body {{ padding: 12px 18px; }}
                .detail-row {{
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    background: #f9fafb;
                    border-left: 3px solid #6366f1;
                    border-radius: 8px;
                    padding: 8px 10px;
                    margin-bottom: 8px;
                    font-size: 13px;
                    color: #374151;
                }}
                .detail-row i {{
                    color: #4f46e5;
                    font-size: 15px;
                }}
                .otp-display {{
                    text-align: center;
                    margin: 25px 0 20px;
                    border-radius: 10px;
                    background: #f3f4f6cc;
                    padding: 12px;
                }}
                .otp-display h2 {{
                    font-size: 16px;
                    margin-bottom: 6px;
                    color: #374151;
                }}
                .otp-code {{
                    font-size: 30px;
                    font-weight: 700;
                    letter-spacing: 6px;
                    background: linear-gradient(90deg, #6366f1, #9333ea);
                    -webkit-background-clip: text;
                    -webkit-text-fill-color: transparent;
                }}
                .card-footer {{
                    background: rgba(255,255,255,0.9);
                    padding: 10px;
                    text-align: center;
                    font-size: 12px;
                    color: #4b5563;
                }}
            </style>
        </head>
        <body>
            <div class='otp-card'>
                <div class='card-header'>
                    <h1><i class='bi bi-shield-lock'></i> OTP Verification</h1>
                    <p>SISAL OTP Notification</p>
                </div>
                <div class='card-body'>
                    <div class='detail-row'><i class='bi bi-person-circle'></i> <strong>Login User:</strong> {otp.LoginUserName}</div>
                    <div class='detail-row'><i class='bi bi-person-badge'></i> <strong>Full Name:</strong> {otp.FullName}</div>
                    <div class='detail-row'><i class='bi bi-fingerprint'></i> <strong>Bio ID:</strong> {otp.BioId}</div>
                    <div class='detail-row'><i class='bi bi-building'></i> <strong>Department:</strong> {otp.Department}</div>
                    <div class='detail-row'><i class='bi bi-person-check'></i> <strong>Req. Bio ID:</strong> {otp.RequestBioId}</div>
                    <div class='detail-row'><i class='bi bi-person-lines-fill'></i> <strong>Staff Name:</strong> {otp.StaffName}</div>

                    <div class='otp-display'>
                        <h2>Your Secure OTP</h2>
                        <div class='otp-code'>{otp.Otp}</div>
                    </div>
                </div>
                <div class='card-footer'>
                    <i class='bi bi-clock-history'></i> This OTP expires in <strong>{otp.ExpiryTime} minutes</strong>.
                </div>
            </div>
        </body>
        </html>";

                await page.SetContentAsync(htmlContent);
                await page.WaitForSelectorAsync(".otp-card");

                var element = await page.QuerySelectorAsync(".otp-card");
                if (element == null)
                    throw new Exception("OTP card not found in HTML.");

                var screenshotBytes = await element.ScreenshotDataAsync(new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    OmitBackground = false
                });

                return screenshotBytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert OTP HTML to image: {ex.Message}", ex);
            }
        }


    }
}