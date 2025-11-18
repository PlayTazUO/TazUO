// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI
{
    internal static class UIScaleHelper
    {
        private static float ResolveScale()
        {
            if (CUOEnviroment.IsHighDPI)
            {
                // HiDPI：UI 按 DPI 放大以匹配放大的 BackBuffer
                return CUOEnviroment.DPIScaleFactor <= 0f ? 1f : CUOEnviroment.DPIScaleFactor;
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile?.GlobalScaling == true && profile.GlobalScale > 0f)
            {
                return profile.GlobalScale;
            }

            if (!Game.Managers.UIManager.InGame && CUOEnviroment.DPIScaleFactor > 1f)
            {
                return CUOEnviroment.DPIScaleFactor;
            }

            return 1f;
        }

        public static float GetCurrentScale()
        {
            float scale = ResolveScale();
            return scale <= 0f ? 1f : scale;
        }

        public static bool IsScaled => Math.Abs(GetCurrentScale() - 1f) > float.Epsilon;

        public static int ConvertToLogical(int value)
        {
            float scale = GetCurrentScale();
            return scale == 1f ? value : (int)MathF.Round(value / scale);
        }

        public static float ConvertToLogical(float value)
        {
            float scale = GetCurrentScale();
            return scale == 1f ? value : value / scale;
        }

        public static Point ConvertToLogical(Point point) =>
            new Point(ConvertToLogical(point.X), ConvertToLogical(point.Y));

        public static Rectangle ConvertToLogical(Rectangle bounds)
        {
            if (!IsScaled)
            {
                return bounds;
            }

            float scale = GetCurrentScale();

            return new Rectangle
            (
                (int)MathF.Round(bounds.X / scale),
                (int)MathF.Round(bounds.Y / scale),
                (int)MathF.Round(bounds.Width / scale),
                (int)MathF.Round(bounds.Height / scale)
            );
        }

        public static Rectangle GetLogicalWindowBounds()
        {
            // macOS HiDPI：返回窗口的逻辑边界，不应用 UI 缩放
            // 因为 UI 坐标系统使用的是未缩放的逻辑坐标，缩放只在绘制时通过 Matrix.CreateScale 应用
            if (CUOEnviroment.IsHighDPI)
            {
                return Client.Game.Window.ClientBounds;
            }
            
            // Windows/Linux：如果有 Profile 的全局缩放，需要转换
            return ConvertToLogical(Client.Game.Window.ClientBounds);
        }

        public static int ConvertToPhysical(int value)
        {
            float scale = GetCurrentScale();
            return scale == 1f ? value : (int)MathF.Round(value * scale);
        }

        public static Point ConvertToPhysical(Point point) =>
            new Point(ConvertToPhysical(point.X), ConvertToPhysical(point.Y));

        public static Rectangle ConvertToPhysical(Rectangle bounds)
        {
            float scale = GetCurrentScale();

            if (scale == 1f)
            {
                return bounds;
            }

            return new Rectangle
            (
                (int)MathF.Round(bounds.X * scale),
                (int)MathF.Round(bounds.Y * scale),
                (int)MathF.Round(bounds.Width * scale),
                (int)MathF.Round(bounds.Height * scale)
            );
        }
    }
}
