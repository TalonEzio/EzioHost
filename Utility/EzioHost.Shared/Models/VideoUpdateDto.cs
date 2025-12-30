using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models;

public class VideoUpdateDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public VideoEnum.VideoShareType ShareType { get; set; }
}