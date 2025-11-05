using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cliq.Api.Interface;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using PuppeteerSharp.Media;

namespace cliq.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChannelController : ControllerBase
    {
        private readonly IChannelInterface _channelInterface;
          private static InstalledBrowser _cachedInstalledBrowser;

        public ChannelController(IChannelInterface channelInterface)
        {
            _channelInterface = channelInterface;
        }

        // get channel details
        [HttpGet("channel-details")]
        public async Task<IActionResult> GetChannelDetails()
        {
            try
            {
                var result = await _channelInterface.GetChannelDetails();
                if (result.IsFailed)
                    return BadRequest(new { errors = result.Errors[0].Message });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = ex.Message });
            }
        }

        //post message in channel
        [HttpPost("post-message")]
        public async Task<IActionResult> PostMessageInChannel([FromBody] string message, [FromQuery] string channelName)
        {
            try
            {
                var result = await _channelInterface.PostMessageInChannel(message, channelName);
                if (result.IsFailed)
                    return BadRequest(new { errors = result.Errors[0].Message });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = ex.Message });
            }
        }

        //upload file to channel
        [HttpPost("Send-file-channel")]
        public async Task<IActionResult> UploadFileToChannel([FromQuery] string channelName, IFormFile file, [FromQuery] string? comments)
        {
            try
            {
                var result = await _channelInterface.UploadFileToChannelAsync(channelName, file, comments);
                if (result.IsFailed)
                    return BadRequest(new { errors = result.Errors[0].Message });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = ex.Message });
            }
        }


        [HttpPost("send-html-to-file")] 
        public async Task<IActionResult> SendHtmlToFile([FromForm] string channelName, [FromForm] string htmlCode, [FromForm] string format = "pdf", [FromForm] string comments = "")
        {
            try
            {
                if (string.IsNullOrEmpty(channelName))
                    return BadRequest(new { Error = "Channel Name is required." });

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
                var result = await _channelInterface.UploadFileToChannelAsync(channelName, formFile , comments);

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

    }
}