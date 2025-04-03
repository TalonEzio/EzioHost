using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class OnnxModelController(IDirectoryProvider directoryProvider, IOnnxModelService onnxModelService, IMapper mapper) : ControllerBase
    {
        public string WebRootPath = directoryProvider.GetWebRootPath();
        public string OnnxPath = directoryProvider.GetOnnxModelFolder();
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
    }
}
