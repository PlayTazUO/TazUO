using System;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI;

public static class UiEffects
{
    public static async Task FadeReplace(
        Container parent,
        int replacedChildIndex,
        Widget newWidget,
        int transitionTimeMs = 300
    )
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (replacedChildIndex < 0 || replacedChildIndex >= parent.Widgets.Count)
            throw new ArgumentOutOfRangeException(nameof(replacedChildIndex));

        int fadeoutTime = transitionTimeMs / 2;
        int fadeinTime = transitionTimeMs - fadeoutTime;

        // Roughly try to match monitor refresh rate
        int iterationTime = 1000 / Math.Min(GameController.SupportedRefreshRate, Settings.GlobalSettings.FPS);

        int fadeoutIterations = fadeoutTime / iterationTime;
        int fadeinIterations = fadeinTime / iterationTime;

        // Fade out old
        Widget oldWidget = parent.Widgets[replacedChildIndex];
        float opacityDecrements = oldWidget.Opacity / fadeoutIterations;

        float originalOpacity = oldWidget.Opacity;
        for (int i = 1; i < fadeoutIterations + 1; i++)
        {
            float newOpacity = Math.Max(0, originalOpacity - opacityDecrements * i);
            MainThreadQueue.EnqueueAction(() => oldWidget.Opacity = newOpacity);
            if (newOpacity <= 0)
                break;
            await Task.Delay(iterationTime);
        }

        // Fade in new
        float opacityIncrements = newWidget.Opacity / fadeinIterations;

        // Myra is buggy and doesn't actually handle 'Replace' events...
        parent.Widgets.RemoveAt(replacedChildIndex);

        // Once we've removed the widget, we need to restore its original opacity;
        // otherwise, next render of the same instance will be incorrect
        oldWidget.Opacity = originalOpacity;

        // Store our 'target' opacity
        originalOpacity = newWidget.Opacity;

        // Start of complete transparent
        newWidget.Opacity = 0;
        parent.Widgets.Insert(replacedChildIndex, newWidget);

        for (int i = 1; i < fadeinIterations + 1; i++)
        {
            float newOpacity = Math.Clamp(opacityIncrements * i, 0, Math.Min(1, originalOpacity));
            MainThreadQueue.EnqueueAction(() => newWidget.Opacity = newOpacity);
            if (newOpacity >= 1)
                break;
            await Task.Delay(iterationTime);
        }
    }
}
