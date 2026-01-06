using System.Diagnostics;
using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OnnxModelController(
    IDirectoryProvider directoryProvider,
    IOnnxModelService onnxModelService,
    IUpscaleService upscaleService,
    IMapper mapper) : ControllerBase
{
    public string OnnxPath = directoryProvider.GetOnnxModelFolder();
    public string TempPath = directoryProvider.GetTempPath();
    public string WebRootPath = directoryProvider.GetWebRootPath();

    [HttpGet]
    public async Task<IActionResult> GetOnnxModels([FromQuery] bool? requireDemo = false)
    {
        var models = await onnxModelService.GetOnnxModels();

        if (requireDemo.HasValue && requireDemo.Value)
            models = models.Where(x => !string.IsNullOrEmpty(x.DemoInput) && !string.IsNullOrEmpty(x.DemoOutput))
                .ToList();

        var onnxModelDtos = mapper.Map<List<OnnxModelDto>>(models);
        return Ok(onnxModelDtos);
    }

    [HttpPut]
    public async Task<IActionResult> AddNewOnnxModel([FromForm] OnnxModelCreateDto model,
        [FromForm] IFormFile modelFile)
    {
        if (modelFile.Length == 0) return BadRequest("File Error");

        var id = Guid.NewGuid();
        var onnxFilePath = Path.Combine(OnnxPath, id + ".onnx");
        await using (var stream = new FileStream(onnxFilePath, FileMode.Create, FileAccess.Write))
        {
            await modelFile.CopyToAsync(stream);
        }

        var newModel = new OnnxModel
        {
            Id = id,
            Name = model.Name,
            Scale = model.Scale,
            ElementType = model.ElementType,
            MustInputWidth = model.MustInputWidth,
            MustInputHeight = model.MustInputHeight,
            CreatedBy = User.UserId,
            FileLocation = Path.GetRelativePath(WebRootPath, onnxFilePath)
        };
        await onnxModelService.AddOnnxModel(newModel);
        return Ok(new { Message = $"Model uploaded successfully. Detected precision: {onnxFilePath}" });
    }

    [HttpPost("demo-reset/{modelId:guid}")]
    public async Task<IActionResult> ResetDemo([FromRoute] Guid modelId)
    {
        var model = await onnxModelService.GetOnnxModelById(modelId);
        if (model == null) return NotFound();

        if (model.CreatedBy != User.UserId) return Unauthorized();
        model.DemoInput = model.DemoOutput = string.Empty;

        await onnxModelService.UpdateOnnxModel(model);
        return Ok();
    }


    [HttpPost("demo/{modelId:guid}")]
    public async Task<IActionResult> DemoUpscale([FromRoute] Guid modelId, [FromForm] IFormFile imageFile)
    {
        var model = await onnxModelService.GetOnnxModelById(modelId);
        if (model == null) return NotFound();

        //if (model.CreatedBy != User.UserId)
        //{
        //    return Unauthorized();
        //}
        var sw = Stopwatch.StartNew();
        var inputFilepath = Path.Combine(TempPath,
            Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(imageFile.FileName));
        var outputFilePath = Path.Combine(TempPath,
            Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(imageFile.FileName));
        var tempFile = new FileStream(inputFilepath, FileMode.Create);
        await imageFile.CopyToAsync(tempFile);

        try
        {
            await upscaleService.UpscaleImage(model, inputFilepath, outputFilePath);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        inputFilepath = Path.GetRelativePath(WebRootPath, inputFilepath);
        outputFilePath = Path.GetRelativePath(WebRootPath, outputFilePath);

        model.DemoInput = inputFilepath;
        model.DemoOutput = outputFilePath;

        await onnxModelService.UpdateOnnxModel(model);
        sw.Stop();

        var responseDto = mapper.Map<UpscaleDemoResponseDto>(model);
        responseDto.ElapsedMilliseconds = sw.ElapsedMilliseconds;

        return Ok(responseDto);
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOnnxModel([FromRoute] Guid id)
    {
        var model = await onnxModelService.GetOnnxModelById(id);
        if (model == null) return NotFound();

        var modelPath = Path.Combine(WebRootPath, model.FileLocation);
        await onnxModelService.DeleteOnnxModel(model);
        if (System.IO.File.Exists(modelPath)) System.IO.File.Delete(modelPath);
        return NoContent();
    }

    [HttpPost("analyze")]
    [Authorize]
    public async Task<IActionResult> AnalyzeOnnxModel([FromForm] IFormFile modelFile)
    {
        if (modelFile.Length == 0) return BadRequest("File Error");

        var tempFilePath = Path.Combine(TempPath, Path.GetRandomFileName() + ".onnx");

        try
        {
            // Save file temporarily
            await using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await modelFile.CopyToAsync(stream);
            }

            // Analyze model
            var metadata = AnalyzeOnnxModelFile(tempFilePath);

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            return Ok(new OnnxModelMetadataDto
            {
                ErrorMessage = $"Không thể đọc thông tin model: {ex.Message}"
            });
        }
        finally
        {
            // Clean up temp file
            if (System.IO.File.Exists(tempFilePath))
                System.IO.File.Delete(tempFilePath);
        }
    }

    private static OnnxModelMetadataDto AnalyzeOnnxModelFile(string modelPath)
    {
        try
        {
            using var session = new InferenceSession(modelPath);

            var metadata = new OnnxModelMetadataDto();
            var inputShape = Array.Empty<int>();

            // Get input metadata
            if (session.InputMetadata.Count > 0)
            {
                var inputMetadata = session.InputMetadata.Values.First();

                inputShape = inputMetadata.Dimensions;

                // Input shape is typically [batch, channels, height, width] or [batch, height, width, channels]
                // For upscale models, it's usually [1, 3, H, W] (NCHW format)
                if (inputShape.Length >= 4)
                {
                    // NCHW format: [batch, channels, height, width]
                    var height = inputShape[^2];
                    var width = inputShape[^1];

                    if (height > 0) metadata.MustInputHeight = height;
                    if (width > 0) metadata.MustInputWidth = width;
                }

                metadata.ElementType = inputMetadata.ElementDataType switch
                {
                    TensorElementType.Float => Shared.Enums.TensorElementType.Float,
                    TensorElementType.UInt8 => Shared.Enums.TensorElementType.UInt8,
                    TensorElementType.Int8 => Shared.Enums.TensorElementType.Int8,
                    TensorElementType.UInt16 => Shared.Enums.TensorElementType.UInt16,
                    TensorElementType.Int16 => Shared.Enums.TensorElementType.Int16,
                    TensorElementType.Int32 => Shared.Enums.TensorElementType.Int32,
                    TensorElementType.Int64 => Shared.Enums.TensorElementType.Int64,
                    TensorElementType.String => Shared.Enums.TensorElementType.String,
                    TensorElementType.Bool => Shared.Enums.TensorElementType.Bool,
                    TensorElementType.Float16 => Shared.Enums.TensorElementType.Float16,
                    TensorElementType.Double => Shared.Enums.TensorElementType.Double,
                    TensorElementType.UInt32 => Shared.Enums.TensorElementType.UInt32,
                    TensorElementType.UInt64 => Shared.Enums.TensorElementType.UInt64,
                    TensorElementType.Complex64 => Shared.Enums.TensorElementType.Complex64,
                    TensorElementType.Complex128 => Shared.Enums.TensorElementType.Complex128,
                    TensorElementType.BFloat16 => Shared.Enums.TensorElementType.BFloat16,
                    _ => Shared.Enums.TensorElementType.Float
                };
            }

            // Get output metadata to calculate scale
            if (session.OutputMetadata.Count > 0 && inputShape.Length >= 4)
            {
                var outputMetadata = session.OutputMetadata.Values.First();
                var outputShape = outputMetadata.Dimensions;

                // Calculate scale from input/output dimensions
                if (outputShape.Length >= 4)
                {
                    var inputHeight = inputShape[^2];
                    var inputWidth = inputShape[^1];
                    var outputHeight = outputShape[^2];
                    var outputWidth = outputShape[^1];

                    if (inputHeight > 0 && outputHeight > 0 && inputWidth > 0 && outputWidth > 0)
                    {
                        var heightScale = outputHeight / inputHeight;
                        var widthScale = outputWidth / inputWidth;

                        // Scale should be the same for width and height
                        if (heightScale == widthScale && heightScale is > 0 and <= 8) metadata.Scale = heightScale;
                    }
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            return new OnnxModelMetadataDto
            {
                ErrorMessage = $"Lỗi khi đọc model: {ex.Message}"
            };
        }
    }
}