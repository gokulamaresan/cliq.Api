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

        // private static async Task<byte[]> ConvertHtmlToImageBytesAsync(string html)
        // {
        //     var htmlContent = html.Replace("\\n", "\n");
        //     var browserFetcher = new BrowserFetcher();
        //     await browserFetcher.DownloadAsync();

        //     try
        //     {
        //         using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        //         {
        //             Headless = true,
        //             Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        //         });
        //         using var page = await browser.NewPageAsync();
        //         await page.SetContentAsync(htmlContent);
        //         using var screenshotStream = await page.ScreenshotStreamAsync(new ScreenshotOptions
        //         {
        //             FullPage = true,
        //             Type = ScreenshotType.Png
        //         });
        //         using var ms = new MemoryStream();
        //         await screenshotStream.CopyToAsync(ms);
        //         return ms.ToArray();
        //     }
        //     catch (Exception ex)
        //     {
        //         // Log or handle the error
        //         throw new Exception("Failed to convert HTML to image: " + ex.Message, ex);
        //     }
        // }

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
                                <style>
                                body {{
                                margin: 0;
                                padding: 0;
                                background: transparent;
                                font-family: 'Gill Sans', 'Trebuchet MS', sans-serif !important;
                                color: #edf1f6;
                                }}
                                .command-card {{
                                background: #10351f;
                                border-radius: 12px;
                                padding: 25px 30px;
                                width: 600px;
                                box-shadow: 0 0 15px rgba(0,0,0,0.3);
                                color: #edf1f6;
                                font-size: 0.85rem;
                                }}
                                .command-header {{
                                display: flex;
                                justify-content: space-between;
                                align-items: center;
                                border-bottom: 1px solid rgba(255,255,255,0.1);
                                padding-bottom: 8px;
                                margin-bottom: 15px;
                                }}
                                .command-header h2 {{
                                font-size: 1rem;
                                font-weight: 700;
                                margin: 0;
                                color: #edf1f6;
                                }}
                                .status-badge {{
                                background: #25b66a;
                                color: #fff;
                                font-size: 0.7rem;
                                padding: 3px 8px;
                                border-radius: 6px;
                                margin-left: 8px;
                                font-weight: 600;
                                text-transform: uppercase;
                                }}
                                .command-body .detail {{
                                display: flex;
                                justify-content: space-between;
                                margin-bottom: 10px;
                                }}
                                .detail label {{
                                color: #9de09a;
                                font-weight: 600;
                                width: 30%;
                                }}
                                .detail span {{
                                width: 65%;
                                color: #edf1f6;
                                line-height: 1.3rem;
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

        [HttpPost("Convert-Html-To-Image-download-TNA")]
        public async Task<IActionResult> ConvertHtmlToImageDownloadTnA([FromForm] HtmlRequest request)
        {
            var maincontent = @" <div class='command-card'>
                                    <div class='command-header'>
                                        <h2>JOB-2025-0456 <span class='status-badge'>NORTH ZONE</span></h2>
                                        <div>11 Nov 2025, 02:30 PM</div>
                                    </div>
                                    <div class='command-body'>
                                        <div class='detail'><label>Job No:</label><span>JOB-2025-0456</span></div>
                                        <div class='detail'><label>Category:</label><span>Maintenance</span></div>
                                        <div class='detail'><label>Command:</label><span> Start Machine Line A and verify synchronization for all
                                                conveyor belts.<br> Once complete, update the log and inform the Head Operator for status
                                                confirmation.<br> Ensure safety protocols are checked before reactivation. </span></div>
                                        <div class='detail'><label>Command By:</label><span>Mr. Rajesh Kumar</span></div>
                                    </div>
                                </div>";
            var bytes = await ConvertHtmlToImageBytesAsync(maincontent);
            return File(bytes, "image/png", "converted_image.png");
        }
    }
}
