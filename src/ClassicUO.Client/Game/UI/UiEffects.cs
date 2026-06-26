using System;
using System.Numerics;
using System.Threading.Tasks;
using ClassicUO.Common;
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
        int transitionTimeMs = 250
    )
    {
        AssertContainerOperationValid(parent, replacedChildIndex);

        (int iterationTime, int iterations) = ComputeTransition(transitionTimeMs / 2);

        await WidgetRemovalEffect(
            parent,
            replacedChildIndex,
            w => new Accessor<float>(() => w.Opacity),
            0,
            1,
            iterations,
            false,
            iterationTime
        );

        await WidgetInsertEffect(
            parent,
            newWidget,
            replacedChildIndex,
            w => new Accessor<float>(() => w.Opacity),
            0,
            1,
            iterations,
            true,
            iterationTime
        );
    }

    public static async Task FadeIn(
        Container parent,
        Widget widget,
        int insertAtIndex,
        int transitionTimeMs = 250
    )
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (insertAtIndex < 0 || insertAtIndex > parent.Widgets.Count)
            throw new ArgumentOutOfRangeException(nameof(insertAtIndex));

        (int iterationTime, int iterations) = ComputeTransition(transitionTimeMs);
        await WidgetInsertEffect(parent, widget, insertAtIndex, w => new Accessor<float>(() => w.Opacity), 0, 1, iterations, true, iterationTime);
    }

    public static async Task FadeOut(
        Container parent,
        int widgetIndex,
        int transitionTimeMs = 250
    )
    {
        AssertContainerOperationValid(parent, widgetIndex);

        (int iterationTime, int iterations) = ComputeTransition(transitionTimeMs);
        await WidgetRemovalEffect(parent, widgetIndex, w => new Accessor<float>(() => w.Opacity), 0, 1, iterations, false, iterationTime);
    }

    private static void AssertContainerOperationValid(Container parent, int widgetIndex)
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (widgetIndex < 0 || widgetIndex >= parent.Widgets.Count)
            throw new ArgumentOutOfRangeException(nameof(widgetIndex));
    }

    // Roughly try to match monitor refresh rate
    private static (int iterationTimeMs, int iterations) ComputeTransition(int transitionTimeMs)
    {
        int fps = Math.Max(60, Math.Min(GameController.SupportedRefreshRate, Settings.GlobalSettings.FPS));
        int iterationTime = 1000 / fps;
        return (iterationTime, transitionTimeMs / iterationTime);
    }

    private static async Task WidgetRemovalEffect<TValueType>(
        Container parent,
        int widgetIndex,
        Func<Widget, Accessor<TValueType>> getAffectedProp,
        TValueType? minValue,
        TValueType? maxValue,
        int effectIterations,
        bool isIncrement,
        int iterationTimeMs
    ) where TValueType :
        struct,
        INumber<TValueType>
    {
        // These run on the calling thread — safe only when callers are on the main thread (currently always true).
        Widget widget = parent.Widgets[widgetIndex];

        Accessor<TValueType> propAccessor = getAffectedProp(widget);
        TValueType oldPropValue = propAccessor.Get();
        await WidgetEffect(propAccessor, minValue, maxValue, effectIterations, isIncrement, iterationTimeMs);

        MainThreadQueue.BubblingInvokeOnMainThread(() =>
        {
            parent.Widgets.RemoveAt(widgetIndex);

            // Restore the prop's original value after the effect is done.
            // This localizes the changes to our scope and prevents leaking property changes done during transitions outside to the consumers
            propAccessor.Set(oldPropValue);
        });
    }

    private static async Task WidgetInsertEffect<TValueType>(
        Container parent,
        Widget widget,
        int widgetIndex,
        Func<Widget, Accessor<TValueType>> getAffectedProp,
        TValueType? minValue,
        TValueType? maxValue,
        int effectIterations,
        bool isIncrement,
        int iterationTimeMs
    ) where TValueType :
        struct,
        INumber<TValueType>
    {
        Accessor<TValueType> propAccessor = getAffectedProp(widget);
        TValueType oldPropValue = propAccessor.Get();

        // Update events must be done on the main thread, or we risk crashes
        MainThreadQueue.BubblingInvokeOnMainThread(() =>
        {
            // The effect starts at the minimum and works its way up back to the widget's original value.
            if (isIncrement && minValue.HasValue)
                propAccessor.Set(minValue.Value);

            parent.Widgets.Insert(widgetIndex, widget);
        });

        await WidgetEffect(propAccessor, minValue, maxValue, effectIterations, isIncrement, iterationTimeMs);

        // Restore the prop's original value after the effect is done.
        // This localizes the changes to our scope and prevents leaking property changes done during transitions outside to the consumers
        MainThreadQueue.InvokeOnMainThread(() => propAccessor.Set(oldPropValue));
    }

    private static async Task WidgetEffect<TValueType>(
        Accessor<TValueType> affectedProp,
        TValueType? minValue,
        TValueType? maxValue,
        int effectIterations,
        bool isIncrement,
        int iterationTimeMs
    ) where TValueType :
        struct,
        INumber<TValueType>
    {
        effectIterations = Math.Max(1, effectIterations);
        TValueType originalPropValue = affectedProp.Get();
        // For increment step from the current value up to maxValue.
        // For decrement step from the current value down to zero.
        TValueType propDiffPerIteration = isIncrement && maxValue.HasValue
            ? (maxValue.Value - originalPropValue) / TValueType.CreateChecked(effectIterations)
            : originalPropValue / TValueType.CreateChecked(effectIterations);

        for (int i = 1; i < effectIterations + 1; i++)
        {
            TValueType increment = propDiffPerIteration * TValueType.CreateChecked(i);
            TValueType newPropValue = isIncrement
                ? originalPropValue + increment
                : originalPropValue - increment;

            bool breakEarly = false;
            if (newPropValue < minValue)
            {
                newPropValue = minValue.Value;
                breakEarly = true;
            }
            else if (newPropValue > maxValue)
            {
                newPropValue = maxValue.Value;
                breakEarly = true;
            }

            MainThreadQueue.InvokeOnMainThread(() => affectedProp.Set(newPropValue));
            if (breakEarly || i >= effectIterations)
                break;

            await Task.Delay(iterationTimeMs);
        }
    }
}
