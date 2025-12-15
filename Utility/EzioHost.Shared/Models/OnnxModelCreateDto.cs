using System.ComponentModel.DataAnnotations;
using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models;

public class OnnxModelCreateDto
{
    public Guid Id { get; set; }

    [StringLength(200, MinimumLength = 2)] public string Name { get; set; } = string.Empty;

    [Range(1, 8)] public int Scale { get; set; } = 1;

    [Range(0, 2160)] public int MustInputWidth { get; set; }

    [Range(0, 2160)] public int MustInputHeight { get; set; }

    public OnnxModelPrecision Precision { get; set; }
}