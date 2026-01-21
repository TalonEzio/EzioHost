using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EzioHost.Core.Services.Implement;

public class OnnxModelService(
    IOnnxModelRepository onnxModelRepository,
    ILogger<OnnxModelService> logger) : IOnnxModelService
{
    public async Task<IEnumerable<OnnxModel>> GetOnnxModels()
    {
        logger.LogDebug("Getting all ONNX models");
        var models = await onnxModelRepository.GetOnnxModels();
        logger.LogDebug("Retrieved {Count} ONNX models", models.Count());
        return models;
    }

    public Task<OnnxModel?> GetOnnxModelById(Guid id)
    {
        logger.LogDebug("Getting ONNX model by ID: {ModelId}", id);
        return onnxModelRepository.GetOnnxModelById(id);
    }

    public async Task<OnnxModel> AddOnnxModel(OnnxModel newModel)
    {
        logger.LogInformation(
            "Adding new ONNX model. ModelId: {ModelId}, Name: {Name}, Scale: {Scale}",
            newModel.Id,
            newModel.Name,
            newModel.Scale);

        var result = await onnxModelRepository.AddOnnxModel(newModel);
        logger.LogInformation("Successfully added ONNX model {ModelId}", newModel.Id);
        return result;
    }

    public async Task<OnnxModel> UpdateOnnxModel(OnnxModel updateModel)
    {
        logger.LogInformation(
            "Updating ONNX model. ModelId: {ModelId}, Name: {Name}",
            updateModel.Id,
            updateModel.Name);

        var result = await onnxModelRepository.UpdateOnnxModel(updateModel);
        logger.LogInformation("Successfully updated ONNX model {ModelId}", updateModel.Id);
        return result;
    }

    public async Task DeleteOnnxModel(Guid id)
    {
        logger.LogInformation("Deleting ONNX model. ModelId: {ModelId}", id);
        await onnxModelRepository.DeleteOnnxModel(id);
        logger.LogInformation("Successfully deleted ONNX model {ModelId}", id);
    }

    public async Task DeleteOnnxModel(OnnxModel deleteModel)
    {
        logger.LogInformation(
            "Deleting ONNX model. ModelId: {ModelId}, Name: {Name}",
            deleteModel.Id,
            deleteModel.Name);

        await onnxModelRepository.DeleteOnnxModel(deleteModel);
        logger.LogInformation("Successfully deleted ONNX model {ModelId}", deleteModel.Id);
    }
}