using Microsoft.AspNetCore.Mvc;
using Server.Core;
using Server.Services;
using Shared.DTOs;
using Shared.Utils;
using System.Diagnostics;

namespace Server.Controllers;

[ApiController]
[Route("api/image")]
public class ImageController : ControllerBase
{
    private readonly ImageProcessingService _service;
    private readonly ImageSessionStore _sessions;

    public ImageController(ImageProcessingService service, ImageSessionStore sessions)
    {
        _service = service;
        _sessions = sessions;
    }

    [HttpPost("upload")]
    public ActionResult<UploadImageResponse> Upload([FromBody] UploadImageRequest request)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ImageConverter.ValidateImageBytes(request.Image, request.FileName);
            var bitmap = ImageConverter.FromBytes(request.Image);
            var id = _sessions.Store(ImageConverter.ToBytes(bitmap));
            sw.Stop();

            Logger.Info($"Upload OK id={id} size={request.Image.Length}B {bitmap.Width}x{bitmap.Height} in {sw.ElapsedMilliseconds}ms");

            return Ok(new UploadImageResponse
            {
                ImageId = id,
                Message = "Image uploaded successfully",
                Width = bitmap.Width,
                Height = bitmap.Height
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Upload failed: {ex.Message}");
            return BadRequest(new ErrorResponse { Message = ex.Message, Code = "UPLOAD_ERROR" });
        }
    }

    [HttpPost("process")]
    public ActionResult<ApiResponse<ApplyFiltersResponse>> Process([FromBody] ApplyFiltersRequest request)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            byte[] imageBytes;
            if (request.ImageId.HasValue)
            {
                imageBytes = _sessions.Get(request.ImageId.Value)
                    ?? throw new ArgumentException("Session image not found");
            }
            else if (request.Image != null && request.Image.Length > 0)
            {
                ImageConverter.ValidateImageBytes(request.Image);
                imageBytes = request.Image;
            }
            else
            {
                throw new ArgumentException("Provide Image bytes or ImageId");
            }

            if (request.Filters == null || request.Filters.Count == 0)
                throw new ArgumentException("At least one filter is required");

            Logger.Info($"Process start input={imageBytes.Length}B filters={request.Filters.Count}");

            var image = ImageConverter.FromBytes(imageBytes);
            var (result, timings) = _service.Process(image, request.Filters);
            var output = ImageConverter.ToBytes(result);

            if (request.ImageId.HasValue)
                _sessions.Update(request.ImageId.Value, output);

            sw.Stop();
            Logger.Info($"Process OK output={output.Length}B total={sw.ElapsedMilliseconds}ms");

            return Ok(new ApiResponse<ApplyFiltersResponse>
            {
                Success = true,
                Data = new ApplyFiltersResponse
                {
                    Image = output,
                    TotalProcessingTimeMs = sw.ElapsedMilliseconds,
                    FilterTimings = timings.Select(t => new FilterTimingDto
                    {
                        Filter = t.Filter.ToString(),
                        DurationMs = t.DurationMs
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Process failed: {ex.Message}");
            return BadRequest(new ApiResponse<ApplyFiltersResponse>
            {
                Success = false,
                Error = new ErrorResponse { Message = ex.Message, Code = "PROCESS_ERROR" }
            });
        }
    }
}
