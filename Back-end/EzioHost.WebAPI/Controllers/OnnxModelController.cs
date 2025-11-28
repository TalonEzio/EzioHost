using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OnnxModelController(IDirectoryProvider directoryProvider, IOnnxModelService onnxModelService, IUpscaleService upscaleService, IMapper mapper) : ControllerBase
    {
        public string WebRootPath = directoryProvider.GetWebRootPath();
        public string OnnxPath = directoryProvider.GetOnnxModelFolder();
        public string TempPath = directoryProvider.GetTempPath();
        [HttpGet]
        public async Task<IActionResult> GetOnnxModels([FromQuery] bool? requireDemo = false)
        {
            var models = await onnxModelService.GetOnnxModels();

            if (requireDemo.HasValue && requireDemo.Value)
            {
                models = models.Where(x => !string.IsNullOrEmpty(x.DemoInput) && !string.IsNullOrEmpty(x.DemoOutput)).ToList();
            }

            var onnxModelDtos = mapper.Map<List<OnnxModelDto>>(models);
            return Ok(onnxModelDtos);
        }

        [HttpPut]
        public async Task<IActionResult> AddNewOnnxModel([FromForm] OnnxModelCreateDto model, [FromForm] IFormFile modelFile)
        {
            if (modelFile.Length == 0)
            {
                return BadRequest("File Error");
            }

            var id = Guid.NewGuid();
            var onnxFilePath = Path.Combine(OnnxPath, id + ".onnx");
            await using (var stream = new FileStream(onnxFilePath, FileMode.Create, FileAccess.Write))
            {
                await modelFile.CopyToAsync(stream);
            }
            var newModel = new OnnxModel()
            {
                Id = id,
                Name = model.Name,
                Scale = model.Scale,
                SupportVideoType = model.SupportVideoType,
                Precision = model.Precision,
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

            if (model.CreatedBy != User.UserId)
            {
                return Unauthorized();
            }
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
            var inputFilepath = Path.Combine(TempPath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(imageFile.FileName));
            var outputFilePath = Path.Combine(TempPath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(imageFile.FileName));
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
            if (model == null)
            {
                return NotFound();
            }

            var modelPath = Path.Combine(WebRootPath, model.FileLocation);
            await onnxModelService.DeleteOnnxModel(model);
            if (System.IO.File.Exists(modelPath))
            {
                System.IO.File.Delete(modelPath);
            }
            return NoContent();
        }
    }
}
