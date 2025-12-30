using EzioHost.Shared.Constants;
using EzioHost.Shared.Models;
using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface IOnnxModelApi
{
    [Get("/api/OnnxModel")]
    Task<List<OnnxModelDto>> GetOnnxModels([Query] bool? requireDemo = null);

    [Put("/api/OnnxModel")]
    [Multipart]
    Task AddOnnxModel(
        [AliasAs(nameof(OnnxModelCreateDto.Name))]
        string name,
        [AliasAs(nameof(OnnxModelCreateDto.Scale))]
        int scale,
        [AliasAs(nameof(OnnxModelCreateDto.MustInputWidth))]
        int mustInputWidth,
        [AliasAs(nameof(OnnxModelCreateDto.MustInputHeight))]
        int mustInputHeight,
        [AliasAs(nameof(OnnxModelCreateDto.ElementType))]
        int precision,
        [AliasAs(FormFieldNames.ModelFile)] StreamPart modelFile);

    [Delete("/api/OnnxModel/{id}")]
    Task DeleteOnnxModel(Guid id);

    [Post("/api/OnnxModel/demo/{modelId}")]
    [Multipart]
    Task<UpscaleDemoResponseDto> DemoUpscale(
        Guid modelId,
        [AliasAs(FormFieldNames.ImageFile)] StreamPart imageFile);

    [Post("/api/OnnxModel/demo-reset/{modelId}")]
    Task ResetDemo(Guid modelId);

    [Post("/api/OnnxModel/analyze")]
    [Multipart]
    Task<OnnxModelMetadataDto> AnalyzeOnnxModel([AliasAs(FormFieldNames.ModelFile)] StreamPart modelFile);
}