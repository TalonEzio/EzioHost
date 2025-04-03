using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Interface
{
    public interface IOnnxModelService
    {
        Task<IEnumerable<OnnxModel>> GetOnnxModels();
        Task<OnnxModel?> GetOnnxModelById(Guid id);
        Task<OnnxModel> AddOnnxModel(OnnxModel newModel);
        Task<OnnxModel> UpdateOnnxModel(OnnxModel updateModel);
        Task DeleteOnnxModel(Guid id);
        Task DeleteOnnxModel(OnnxModel deleteModel);
    }
}
