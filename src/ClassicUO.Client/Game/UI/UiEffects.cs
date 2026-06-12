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
        ArgumentNullException.ThrowIfNull(parent);
        if (replacedChildIndex < 0 || replacedChildIndex >= parent.Widgets.Count)
            throw new ArgumentOutOfRangeException(nameof(replacedChildIndex));

        // Roughly try to match monitor refresh rate
        int iterationTime = 1000 / Math.Min(GameController.SupportedRefreshRate, Settings.GlobalSettings.FPS);

        int transitionIterations = transitionTimeMs / 2 / iterationTime;

        await WidgetRemovalEffect(
            parent,
            replacedChildIndex,
            w => new Accessor<float>(() => w.Opacity),
            0,
            1,
            transitionIterations,
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
            transitionIterations,
            true,
            iterationTime
        );
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
        Widget widget = parent.Widgets[widgetIndex];

        Accessor<TValueType> propAccessor = getAffectedProp(widget);
        TValueType oldPropValue = propAccessor.Get();
        await WidgetEffect(propAccessor, minValue, maxValue, effectIterations, isIncrement, iterationTimeMs);

        // Myra is buggy and doesn't actually handle 'Replace' events...
        parent.Widgets.RemoveAt(widgetIndex);

        // Restore the prop's original value. This localizes the changes to our scope and prevents leaking
        // effect transitions outside to the consumers
        propAccessor.Set(oldPropValue);
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
        await WidgetEffect(propAccessor, minValue, maxValue, effectIterations, isIncrement, iterationTimeMs);

        // Myra is buggy and doesn't actually handle 'Replace' events...
        parent.Widgets.Insert(widgetIndex, widget);

        // Restore the prop's original value. This localizes the changes to our scope and prevents leaking
        // effect transitions outside to the consumers
        propAccessor.Set(oldPropValue);
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
        TValueType originalPropValue = affectedProp.Get();
        TValueType propDiffPerIteration = affectedProp.Get() / TValueType.CreateChecked(effectIterations);

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
            if (breakEarly)
                break;

            await Task.Delay(iterationTimeMs);
        }
    }
}
