using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI;

public static class UiEffects
{
    private static readonly Dictionary<Container, CancellationTokenSource> _activeTransitions = new();
    private static readonly Lock _transitionLock = new();

    /// <summary>Fades out an existing child widget and fades in a replacement at the same index.</summary>
    /// <param name="parent">Container that owns the widget being replaced.</param>
    /// <param name="replacedChildIndex">Index of the child to remove via fade-out.</param>
    /// <param name="newWidget">Widget to insert and fade in after removal completes.</param>
    /// <param name="transitionTimeMs">Total duration of the full replace effect in milliseconds. Each fade phase gets half.</param>
    public static async Task FadeReplace(
        Container parent,
        int replacedChildIndex,
        Widget newWidget,
        int transitionTimeMs = 250
    )
    {
        AssertContainerOperationValid(parent, replacedChildIndex);
        await EnforceNonMainThread();

        await WithParentTransition(
            parent,
            async cToken =>
            {
                (int iterationTime, int iterations) = ComputeTransition(transitionTimeMs / 2);

                await WidgetRemovalEffect(
                    parent,
                    replacedChildIndex,
                    w => new Accessor<float>(() => w.Opacity),
                    0,
                    1,
                    iterations,
                    false,
                    iterationTime,
                    cToken
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
                    iterationTime,
                    cToken
                );
            }
        );
    }

    /// <summary>Inserts a widget into a container and fades it in from transparent to fully opaque.</summary>
    /// <param name="parent">Container to insert the widget into.</param>
    /// <param name="widget">Widget to insert and animate.</param>
    /// <param name="insertAtIndex">Position in <paramref name="parent"/>'s widget list to insert at.</param>
    /// <param name="transitionTimeMs">Duration of the fade-in animation in milliseconds.</param>
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

        await EnforceNonMainThread();

        await WithParentTransition(
            parent,
            async cToken =>
            {
                (int iterationTime, int iterations) = ComputeTransition(transitionTimeMs);
                await WidgetInsertEffect(
                    parent,
                    widget,
                    insertAtIndex,
                    w => new Accessor<float>(() => w.Opacity),
                    0,
                    1,
                    iterations,
                    true,
                    iterationTime,
                    cToken
                );
            }
        );
    }

    /// <summary>Fades out a child widget and removes it from the container once the animation completes.</summary>
    /// <param name="parent">Container that owns the widget to remove.</param>
    /// <param name="widgetIndex">Index of the child widget to fade out and remove.</param>
    /// <param name="transitionTimeMs">Duration of the fade-out animation in milliseconds.</param>
    public static async Task FadeOut(
        Container parent,
        int widgetIndex,
        int transitionTimeMs = 250
    )
    {
        AssertContainerOperationValid(parent, widgetIndex);
        await EnforceNonMainThread();

        await WithParentTransition(parent,
            async cToken =>
            {
                (int iterationTime, int iterations) = ComputeTransition(transitionTimeMs);
                await WidgetRemovalEffect(
                    parent,
                    widgetIndex,
                    w => new Accessor<float>(() => w.Opacity),
                    0,
                    1,
                    iterations,
                    false,
                    iterationTime,
                    cToken
                );
            }
        );
    }

    /// <summary>
    /// Wraps a transition action with begin/end lifecycle management, suppressing <see cref="OperationCanceledException"/>
    /// that result from a newer transition preempting this one.
    /// </summary>
    /// <param name="parent">Container whose active transition slot this action occupies.</param>
    /// <param name="action">Transition logic to execute; receives a cancellation token tied to this transition's slot.</param>
    private static async Task WithParentTransition(Container parent, Func<CancellationToken, Task> action)
    {
        CancellationTokenSource cts = BeginTransition(parent);
        try
        {
            await action(cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            EndTransition(parent, cts);
        }
    }

    /// <summary>
    /// Registers a new transition for <paramref name="parent"/>, cancelling and disposing any transition already in progress.
    /// </summary>
    /// <param name="parent">Container to begin a transition for.</param>
    /// <returns>A fresh <see cref="CancellationTokenSource"/> bound to the new transition slot.</returns>
    private static CancellationTokenSource BeginTransition(Container parent)
    {
        var cts = new CancellationTokenSource();
        lock (_transitionLock)
        {
            if (_activeTransitions.TryGetValue(parent, out CancellationTokenSource existing))
            {
                existing.Cancel();
                existing.Dispose();
            }

            _activeTransitions[parent] = cts;
        }

        return cts;
    }

    /// <summary>
    /// Removes and disposes the transition slot for <paramref name="parent"/> if <paramref name="cts"/> is still the active one.
    /// Skips disposal if a newer transition already replaced and disposed it via <see cref="BeginTransition"/>.
    /// </summary>
    /// <param name="parent">Container whose transition slot to release.</param>
    /// <param name="cts">The <see cref="CancellationTokenSource"/> that was returned by the matching <see cref="BeginTransition"/> call.</param>
    private static void EndTransition(Container parent, CancellationTokenSource cts)
    {
        bool isActive;
        lock (_transitionLock)
        {
            isActive = _activeTransitions.TryGetValue(parent, out CancellationTokenSource current) && ReferenceEquals(current, cts);
            if (isActive)
                _activeTransitions.Remove(parent);
        }

        // Only dispose if we're still the active transition — if not, BeginTransition already disposed us
        if (isActive)
            cts.Dispose();
    }

    /// <summary>Throws if <paramref name="parent"/> is null or <paramref name="widgetIndex"/> is out of bounds.</summary>
    /// <param name="parent">Container to validate.</param>
    /// <param name="widgetIndex">Index that must be within <c>[0, parent.Widgets.Count)</c>.</param>
    private static void AssertContainerOperationValid(Container parent, int widgetIndex)
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (widgetIndex < 0 || widgetIndex >= parent.Widgets.Count)
            throw new ArgumentOutOfRangeException(nameof(widgetIndex));
    }

    /// <summary>
    /// Yields to a thread-pool continuation if called on the main thread.
    /// Prevents mid-render UI mutations when effects are triggered from Myra event handlers,
    /// which run inside the render cycle and would conflict with Myra's internal state.
    /// </summary>
    private static async Task EnforceNonMainThread()
    {
        if (MainThreadQueue.IsMainThread)
            await Task.Yield();
    }

    /// <summary>Derives per-frame timing for an animation so it roughly matches the monitor refresh rate.</summary>
    /// <param name="transitionTimeMs">Desired total animation duration in milliseconds.</param>
    /// <returns>
    /// A tuple of the delay between iterations in milliseconds and the total number of iterations to perform.
    /// </returns>
    private static (int iterationTimeMs, int iterations) ComputeTransition(int transitionTimeMs)
    {
        int fps = Math.Min(GameController.SupportedRefreshRate, Settings.GlobalSettings.FPS);

        // While this should never happen, if for some reason the refresh rate is 0, assume 60 FPS
        if (fps <= 0)
            fps = 60;

        int iterationTime = 1000 / fps;
        return (iterationTime, transitionTimeMs / iterationTime);
    }

    /// <summary>Animates a numeric property on a child widget, then removes the widget from the container.</summary>
    /// <param name="parent">Container that owns the widget.</param>
    /// <param name="widgetIndex">Index of the child widget to animate and remove.</param>
    /// <param name="getAffectedProp">Factory that returns an <see cref="Accessor{T}"/> for the property to animate on a given widget.</param>
    /// <param name="minValue">Lower clamp for the animated property value.</param>
    /// <param name="maxValue">Upper clamp for the animated property value.</param>
    /// <param name="effectIterations">Number of animation steps to perform.</param>
    /// <param name="isIncrement"><c>true</c> to step up toward <paramref name="maxValue"/>; <c>false</c> to step down toward <paramref name="minValue"/>.</param>
    /// <param name="iterationTimeMs">Delay in milliseconds between each animation step.</param>
    /// <param name="ct">Token used to cancel the animation if a newer transition preempts this one.</param>
    private static async Task WidgetRemovalEffect<TValueType>(
        Container parent,
        int widgetIndex,
        Func<Widget, Accessor<TValueType>> getAffectedProp,
        TValueType? minValue,
        TValueType? maxValue,
        int effectIterations,
        bool isIncrement,
        int iterationTimeMs,
        CancellationToken ct
    ) where TValueType :
        struct,
        INumber<TValueType>
    {
        Widget widget = null;
        Accessor<TValueType> propAccessor = null;
        TValueType oldPropValue = default;

        MainThreadQueue.BubblingInvokeOnMainThread(() =>
        {
            widget = parent.Widgets[widgetIndex];
            propAccessor = getAffectedProp(widget);
            oldPropValue = propAccessor.Get();
        });

        try
        {
            await WidgetEffect(propAccessor, minValue, maxValue, effectIterations, isIncrement, iterationTimeMs, ct);

            // Use Remove(widget) not RemoveAt(index): if external code mutated the container
            // (e.g. search Clear/Add), the index may now point to the wrong widget.
            // Passing ct skips this entirely if a newer transition has already started.
            MainThreadQueue.BubblingInvokeOnMainThread(() =>
            {
                parent.Widgets.Remove(widget);
                propAccessor.Set(oldPropValue);
            }, ct);
        }
        catch (OperationCanceledException)
        {
            // Restore opacity — widget stays in parent; next transition will handle it
            MainThreadQueue.InvokeOnMainThread(() => propAccessor.Set(oldPropValue));
            throw;
        }
    }

    /// <summary>Inserts a widget into the container and animates a numeric property to produce an entry effect.</summary>
    /// <param name="parent">Container to insert the widget into.</param>
    /// <param name="widget">Widget to insert and animate.</param>
    /// <param name="widgetIndex">Position in <paramref name="parent"/>'s widget list at which to insert.</param>
    /// <param name="getAffectedProp">Factory that returns an <see cref="Accessor{T}"/> for the property to animate on the widget.</param>
    /// <param name="minValue">Starting value for the animated property when <paramref name="isIncrement"/> is <c>true</c>; lower clamp otherwise.</param>
    /// <param name="maxValue">Upper clamp for the animated property value.</param>
    /// <param name="effectIterations">Number of animation steps to perform.</param>
    /// <param name="isIncrement"><c>true</c> to step up toward <paramref name="maxValue"/>; <c>false</c> to step down toward <paramref name="minValue"/>.</param>
    /// <param name="iterationTimeMs">Delay in milliseconds between each animation step.</param>
    /// <param name="ct">Token used to cancel the animation if a newer transition preempts this one.</param>
    private static async Task WidgetInsertEffect<TValueType>(
        Container parent,
        Widget widget,
        int widgetIndex,
        Func<Widget, Accessor<TValueType>> getAffectedProp,
        TValueType? minValue,
        TValueType? maxValue,
        int effectIterations,
        bool isIncrement,
        int iterationTimeMs,
        CancellationToken ct
    ) where TValueType :
        struct,
        INumber<TValueType>
    {
        Accessor<TValueType> propAccessor = getAffectedProp(widget);
        TValueType oldPropValue = MainThreadQueue.BubblingInvokeOnMainThread(propAccessor.Get);

        // Passing ct: if a newer transition has already started (CTS cancelled), skip the insert entirely.
        MainThreadQueue.BubblingInvokeOnMainThread(() =>
        {
            // The effect starts at the minimum and works its way up back to the widget's original value.
            if (isIncrement && minValue.HasValue)
                propAccessor.Set(minValue.Value);

            parent.Widgets.Insert(widgetIndex, widget);
        }, ct);

        try
        {
            await WidgetEffect(propAccessor, minValue, maxValue, effectIterations, isIncrement, iterationTimeMs, ct);
        }
        finally
        {
            // Restore whether completed or cancelled — widget is in parent either way
            MainThreadQueue.InvokeOnMainThread(() => propAccessor.Set(oldPropValue));
        }
    }

    /// <summary>
    /// Core animation loop: steps a numeric property from its current value toward <paramref name="maxValue"/> or zero,
    /// dispatching each update to the main thread and awaiting <paramref name="iterationTimeMs"/> between steps.
    /// </summary>
    /// <param name="affectedProp">Accessor for the property to animate.</param>
    /// <param name="minValue">Lower clamp applied if the computed value falls below this during animation.</param>
    /// <param name="maxValue">Target value for increment animations; upper clamp for decrement animations.</param>
    /// <param name="effectIterations">Number of steps in the animation. Clamped to at least 1.</param>
    /// <param name="isIncrement"><c>true</c> to step the property up toward <paramref name="maxValue"/>; <c>false</c> to step it down toward zero.</param>
    /// <param name="iterationTimeMs">Delay in milliseconds to wait between each step.</param>
    /// <param name="ct">Token that cancels the loop when a newer transition preempts this one.</param>
    private static async Task WidgetEffect<TValueType>(
        Accessor<TValueType> affectedProp,
        TValueType? minValue,
        TValueType? maxValue,
        int effectIterations,
        bool isIncrement,
        int iterationTimeMs,
        CancellationToken ct
    ) where TValueType :
        struct,
        INumber<TValueType>
    {
        effectIterations = Math.Max(1, effectIterations);
        TValueType originalPropValue = MainThreadQueue.BubblingInvokeOnMainThread(affectedProp.Get);
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

            MainThreadQueue.InvokeOnMainThread(() => affectedProp.Set(newPropValue), ct);
            if (breakEarly || i >= effectIterations)
                break;

            await Task.Delay(iterationTimeMs, ct);
        }
    }
}
