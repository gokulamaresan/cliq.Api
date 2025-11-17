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
        public async Task<IActionResult> SendTnaHtmlToFile(string zuidORemail, [FromBody] TNACommentDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(zuidORemail))
                    return BadRequest(new { Error = "ZUID is required." });

                var bytes = await GenerateTnaCardImage(dto);

                var result = await _IMessageInterface.SendFileToUserByZuid(
                    bytes,
                    dto.JobNo,
                    "image/png",
                    zuidORemail,
                    $"Received From {dto.JobNo} - {dto.TaskName}"
                );

                if (result.IsFailed)
                    return BadRequest(new { Error = result.Errors[0].Message });

                return Ok(new
                {
                    Message = "HTML converted to image and sent successfully",
                    FileId = result.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }





        private async Task<byte[]> GenerateTnaCardImage(TNACommentDto dto)
        {
            string html = $@"
<div class='card' id='commentCard'>

    <div class='header'>

        <div class='left-header'>
            <i class='bi bi-briefcase header-icon'></i>
            <div class='job-details'>
                <h3>{dto.JobNo}</h3>
                <p>{dto.CommentGroup}</p>
            </div>
        </div>

        <div class='premium-badge'>
            <i class='bi bi-star-fill'></i> {dto.TaskName}
        </div>

    </div>

    <div class='body'>
      <div class='comment-group'>
        <i class='bi bi-chat-dots comment-group-icon'></i>
        <span>Comment</span>
      </div>
      <p class='comment-text'>{dto.CommentText}</p>
    </div>

    <div class='footer'>
      <div class='footer-item'>
        <i class='bi bi-person'></i>
        <span>{dto.UserName}</span>
      </div>
      <div class='footer-item'>
        <i class='bi bi-clock'></i>
        <span>{dto.CreatedTime}</span>
      </div>
    </div>

</div>";



            return await ConvertToImage(html);
        }




        private static async Task<byte[]> ConvertToImage(string innerHtml)
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            using var page = await browser.NewPageAsync();

            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 900,
                Height = 1200,
                DeviceScaleFactor = 2
            });

            string html = @$"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>

<!-- FIXED ICON LOAD -->
<link rel='stylesheet'
      href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css'>

<style>
/* RESET */
* {{
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}}

body {{
  font-family: 'Segoe UI', sans-serif;
  background: #ffffff;
  display: flex;
  justify-content: center;
  align-items: center;
}}

/* CARD */
.card {{
  width: 410px;
  padding: 35px 50px;
  background: #ffffff;
  border-radius: 20px;
  border: 1px solid rgba(220,220,220,0.55);
  box-shadow:
    0px 8px 20px rgba(0,0,0,0.06),
    0px 2px 6px rgba(0,0,0,0.04);

  position: relative;
  color: #1e293b;
}}

/* ---- HEADER FIX ---- */
.header {{
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 12px;
  border-bottom: 1px solid rgba(0,0,0,0.08);
}}

.left-header {{
  display: flex;
  align-items: center;
  gap: 12px;
}}

.header-icon {{
  font-size: 31px;
  color: #6b7280;
}}

.job-details h3 {{
  font-size: 20px;
  font-weight: 800;
}}

.job-details p {{
  font-size: 12px;
  color: #475569;
}}

/* ---- PREMIUM BADGE ---- */
.premium-badge {{
  background: linear-gradient(135deg, #fde68a, #fcd34d, #fbbf24);
  padding: 6px 13px;
  border-radius: 40px;

  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 6px;

  color: #3d3d3d;
  box-shadow: 0px 6px 16px rgba(251,191,36,0.45);
}}

/* ---- BODY ---- */
.comment-group {{
  display: flex;
  align-items: center;
  margin: 18px 0 10px 0;
}}

.comment-group-icon {{
  font-size: 20px;
  color: #2563eb;
  margin-right: 10px;
}}

.comment-group span {{
  font-weight: 700;
  font-size: 12px;
  letter-spacing: 0.5px;
  color: #1e40af;
  text-transform: uppercase;
}}

.comment-text {{
  font-size: 13px;
  line-height: 1.7;
  color: #1f2937;
  margin-bottom: 22px;
}}

/* ---- FOOTER ---- */
.footer {{
  padding-top: 16px;
  border-top: 1px solid rgba(0,0,0,0.08);
  display: flex;
  justify-content: space-between;
  font-size: 11px;
  color: #475569;
}}

.footer-item {{
  display: flex;
  align-items: center;
  gap: 6px;
}}


</style>
</head>

<body>
    {innerHtml}
</body>

</html>";

            await page.SetContentAsync(html);

            // Wait for Bootstrap Icons font to finish loading
            await page.WaitForNetworkIdleAsync();

            // Extra safety delay to render icons
            await page.WaitForTimeoutAsync(300);

            await page.WaitForSelectorAsync(".card");

            var card = await page.QuerySelectorAsync(".card");

            return await card.ScreenshotDataAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Png,
                OmitBackground = true
            });
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