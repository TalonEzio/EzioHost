using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities
{
    [Table("OnnxModels")]
    public class OnnxModel : BaseAuditableEntityWithUserId<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FileLocation { get; set; } = string.Empty;
        public string DemoInput { get; set; } = string.Empty;
        public string DemoOutput { get; set; } = string.Empty;
        public int Scale { get; set; }
        public int MustInputWidth { get; set; }
        public int MustInputHeight { get; set; }
        public VideoEnum.VideoType SupportVideoType { get; set; } = VideoEnum.VideoType.Anime | VideoEnum.VideoType.Movie;
        public OnnxModelPrecision Precision { get; set; }
    }
   
}
