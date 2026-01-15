using System.ComponentModel;

namespace EzioHost.Shared.Enums;

public class WhisperEnum
{
    public enum WhisperModelType : byte
    {
        [Description("Tiny (~75MB)")] Tiny = 0,
        [Description("Base (~142MB)")] Base = 1,
        [Description("Small (~466MB)")] Small = 2,
        [Description("Medium (~1.5GB)")] Medium = 3,
        [Description("Large (~2.9GB)")] Large = 4
    }
}
