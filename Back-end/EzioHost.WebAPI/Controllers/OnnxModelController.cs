using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Common;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class OnnxModelController(IDirectoryProvider directoryProvider, IOnnxModelService onnxModelService, IUpscaleService upscaleService, IMapper mapper) : ControllerBase
    {
        public string WebRootPath = directoryProvider.GetWebRootPath();
        public string OnnxPath = directoryProvider.GetOnnxModelFolder();
        public string TempPath = directoryProvider.GetTempPath();
        [HttpGet]
        public async Task<IActionResult> GetOnnxModels()
        {
            var models = await onnxModelService.GetOnnxModels();

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
                CreatedBy = User.GetUserId(),
                FileLocation = Path.GetRelativePath(WebRootPath, onnxFilePath)
            };
            await onnxModelService.AddOnnxModel(newModel);
            return Ok(new { Message = $"Model uploaded successfully. Detected precision: {onnxFilePath}" });
        }

        [HttpPost("demo/{modelId:guid}")]
        //[Authorize]
        public async Task<IActionResult> DemoUpscale([FromRoute] Guid modelId, [FromForm] IFormFile imageFile)
        {
            var model = await onnxModelService.GetOnnxModelById(modelId);
            if (model == null) return NotFound();

            //if (model.CreatedBy != User.GetUserId())
            //{
            //    return Unauthorized();
            //}

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

            var responseDto = mapper.Map<UpscaleDemoResponseDto>(model);
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
