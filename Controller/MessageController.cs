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

        // ---------------------------------------------
        // Send text message through Zoho Cliq bot
        // ---------------------------------------------
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

        // ---------------------------------------------
        // Upload and send file to user by ZUID
        // ---------------------------------------------
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

        // ---------------------------------------------
        // Send text message directly to user by ZUID
        // ---------------------------------------------
        [HttpPost("send-text-message-to-user-by-zuid")]
        public async Task<IActionResult> SendTextMessage([FromForm] string zuid, [FromForm] string message)
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

        // ---------------------------------------------
        // Convert HTML to PDF or image and send via Zoho Cliq
        // ---------------------------------------------
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

        // ---------------------------------------------
        // HTML -> PDF conversion using PuppeteerSharp
        // ---------------------------------------------
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

        // ---------------------------------------------
        // HTML -> Image conversion using PuppeteerSharp
        // ---------------------------------------------
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


        // ---------------------------------------------
        // Launch Chromium browser instance (cached)
        // Works with PuppeteerSharp v21+
        // ---------------------------------------------
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


        public class VoiceAlertMessageRequest
        {
            public List<string> Zuids { get; set; }

            public string Message { get; set; }
        }
    }
}