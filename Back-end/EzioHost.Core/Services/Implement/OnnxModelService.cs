using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Implement;

public class OnnxModelService(IOnnxModelRepository onnxModelRepository) : IOnnxModelService
{
    public Task<IEnumerable<OnnxModel>> GetOnnxModels()
    {
        return onnxModelRepository.GetOnnxModels();
    }

    public Task<OnnxModel?> GetOnnxModelById(Guid id)
    {
        return onnxModelRepository.GetOnnxModelById(id);
    }

    public Task<OnnxModel> AddOnnxModel(OnnxModel newModel)
    {
        return onnxModelRepository.AddOnnxModel(newModel);
    }

    public Task<OnnxModel> UpdateOnnxModel(OnnxModel updateModel)
    {
        return onnxModelRepository.UpdateOnnxModel(updateModel);
    }

    public Task DeleteOnnxModel(Guid id)
    {
        return onnxModelRepository.DeleteOnnxModel(id);
    }

    public Task DeleteOnnxModel(OnnxModel deleteModel)
    {
        return onnxModelRepository.DeleteOnnxModel(deleteModel);
    }
}