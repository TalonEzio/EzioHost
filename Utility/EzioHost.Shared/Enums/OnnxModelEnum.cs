using System.ComponentModel;

namespace EzioHost.Shared.Enums
{

    public enum OnnxModelPrecision
    {
        [Description("Fp16")]
        Fp16 = 16,
        [Description("Fp32")]
        Fp32 = 32,
        [Description("Fp16, Fp32")]
        MixedMode = 64,

    }
}
