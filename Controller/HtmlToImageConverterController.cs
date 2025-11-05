using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using System.IO;

namespace Cliq.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]


    public class HtmlRequest
    {
        public string Html { get; set; }
    }


    public class HtmlToImageConverterController : ControllerBase  // <-- inherit ControllerBase
    {

        private static async Task<byte[]> ConvertHtmlToImageBytesAsync(string html)
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
                await page.SetContentAsync(htmlContent);
                using var screenshotStream = await page.ScreenshotStreamAsync(new ScreenshotOptions
                {
                    FullPage = true,
                    Type = ScreenshotType.Png
                });
                using var ms = new MemoryStream();
                await screenshotStream.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                // Log or handle the error
                throw new Exception("Failed to convert HTML to image: " + ex.Message, ex);
            }
        }

        [HttpPost("Convert-Html-To-Image-byte")]
        public async Task<byte[]> ConvertHtmlToImageByteAsync([FromForm] HtmlRequest request)
        {
            return await ConvertHtmlToImageBytesAsync(request.Html);
        }

        [HttpPost("Convert-Html-To-Image-download")]
        public async Task<IActionResult> ConvertHtmlToImageDownloadAsync([FromForm] HtmlRequest request)
        {
            var bytes = await ConvertHtmlToImageBytesAsync(request.Html);
            return File(bytes, "image/png", "converted_image.png");
        }
    }
}
