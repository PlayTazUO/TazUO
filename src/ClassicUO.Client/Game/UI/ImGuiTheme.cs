using System.Numerics;

namespace ClassicUO.Game.UI
{
    /// <summary>
    /// A collection of color definitions for a consistent ImGui theme.
    /// </summary>
    public static class ImGuiTheme
    {
        public static class Colors
        {
            // Bases
            public static readonly Vector4 Base100 = new Vector4(0.118f, 0.115f, 0.143f, 1.00f); // #1E1D24FF
            public static readonly Vector4 Base200 = new Vector4(0.196f, 0.200f, 0.220f, 1.00f); // #323338ff
            public static readonly Vector4 Base300 = new Vector4(0.314f, 0.314f, 0.314f, 1.00f); // #505050ff
            public static readonly Vector4 BaseContent = new Vector4(0.941f, 0.941f, 0.941f, 1.00f); // #f0f0f0ff

            // Principal Palette
            public static readonly Vector4 Primary = new Vector4(0.667f, 0.412f, 0.051f, 1.00f); // #aa690dff
            public static readonly Vector4 PrimaryContent = new Vector4(0.118f, 0.115f, 0.143f, 1.00f); // #1E1D24FF

            public static readonly Vector4 Secondary = new Vector4(0.82f, 0.68f, 0.32f, 1.00f); // #D1AD52FF
            public static readonly Vector4 SecondaryContent = new Vector4(0.118f, 0.115f, 0.143f, 1.00f); // #1E1D24FF

            public static readonly Vector4 Accent = new Vector4(0.88f, 0.74f, 0.38f, 1.00f); // #E0BD61FF
            public static readonly Vector4 AccentContent = new Vector4(0.10f, 0.08f, 0.15f, 1.00f); // #191428FF

            // Neutral
            public static readonly Vector4 Neutral = new Vector4(0.097f, 0.094f, 0.118f, 1.00f); // #191820FF
            public static readonly Vector4 NeutralContent = BaseContent; // #E3E5F4FF

            // Info - States
            public static readonly Vector4 Info = new Vector4(0.36f, 0.72f, 0.62f, 1.00f); // #5CB89EFF
            public static readonly Vector4 InfoContent = new Vector4(0.10f, 0.15f, 0.25f, 1.00f); // #192640FF
            public static readonly Vector4 Success = new Vector4(0.40f, 0.75f, 0.58f, 1.00f); // #66BF94FF
            public static readonly Vector4 SuccessContent = new Vector4(0.10f, 0.16f, 0.25f, 1.00f); // #1A2940FF
            public static readonly Vector4 Warning = new Vector4(0.96f, 0.78f, 0.32f, 1.00f); // #F5C753FF
            public static readonly Vector4 WarningContent = new Vector4(0.18f, 0.18f, 0.10f, 1.00f); // #2E2E1AFF
            public static readonly Vector4 Error = new Vector4(0.74f, 0.20f, 0.18f, 1.00f); // #BD332EFF
            public static readonly Vector4 ErrorContent = new Vector4(0.12f, 0.10f, 0.10f, 1.00f); // #1F1A1AFF

            // Extras for UI commons
            public static readonly Vector4 Border = new Vector4(0.097f, 0.094f, 0.118f, 0.50f); // #19182080
            public static readonly Vector4 BorderShadow = new Vector4(0.00f, 0.00f, 0.00f, 0.00f); // #00000000
            public static readonly Vector4 ScrollbarBg = new Vector4(0.089f, 0.087f, 0.110f, 1.00f); // #16161CFF
            public static readonly Vector4 ScrollbarGrab = new Vector4(0.300f, 0.350f, 0.400f, 1.00f); // #4C5966FF
            public static readonly Vector4 ScrollbarGrabHovered = new Vector4(0.400f, 0.500f, 0.550f, 1.00f); // #66808CFF
            public static readonly Vector4 ScrollbarGrabActive = new Vector4(0.450f, 0.550f, 0.600f, 1.00f); // #738C99FF

        }
    }
}
