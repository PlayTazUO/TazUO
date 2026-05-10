using System;
using System.Timers;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Processes;

/// <summary>
/// This is intended to figure out a good object delay for people who are uncertain of their servers delay settings
/// </summary>
public static class AutomatedObjectDelay
{
    private const int STEP_CHANGE = 100;

    private static int _delay = 1100;
    private static Timer _timer;
    private static Item _item;
    private static Action _onCompletion = null;

    public static void Begin(Action onCompletion = null)
    {
        _delay = ProfileManager.CurrentProfile.MoveMultiObjectDelay = 1100;
        _onCompletion = onCompletion;
        GameActions.Print("Please select an item in your backpack we can move around for this test.", Constants.HUE_SUCCESS);
        GameActions.Print("Please do not do anything until this test is complete.", Constants.HUE_ERROR);
        World.Instance.TargetManager.SetTargeting((o) =>
        {
            if (o is not Item item || item.Container != World.Instance.Player.Backpack)
            {
                GameActions.Print("That is not a proper item for this.", Constants.HUE_ERROR);
                return;
            }

            EventSink.ClilocMessageReceived += EventSinkOnClilocMessageReceived;

            ObjectActionQueue.Instance.Clear();
            _item = item;
            TryMoveItem(item);
        });
    }

    private static void EventSinkOnClilocMessageReceived(object sender, MessageEventArgs e)
    {
        if (e.Cliloc == 500119)
        {
            _delay += (STEP_CHANGE * 2) + (int)AsyncNetClient.Socket.Statistics.Ping;
            ProfileManager.CurrentProfile.MoveMultiObjectDelay = _delay;
            End();
        }
    }

    private static void TryMoveItem(Item item)
    {
        if (item == null) return;

        var mr = new MoveRequest(item, World.Instance.Player.Backpack, item.Amount);

        GameActions.Print($"Enqueueing item for a move test with a delay of {_delay}");

        ObjectActionQueue.Instance.Enqueue(
            new ObjectActionQueueItem(() =>
            {
                mr.Execute();
                GameActions.Print($"Moved item, starting timer for next move test.");
            }, (aqi) =>
            {
                _delay -= STEP_CHANGE;

                if (_delay <= 0)
                {
                    End();
                    return;
                }

                ProfileManager.CurrentProfile.MoveMultiObjectDelay = _delay;

                _timer = new Timer(TimeSpan.FromMilliseconds(_delay));
                _timer.Elapsed += TimerOnElapsed;
                _timer.AutoReset = false;
                _timer.Start();
            }),
            ActionPriority.MoveItem);
    }

    private static void TimerOnElapsed(object sender, ElapsedEventArgs e) =>
        MainThreadQueue.EnqueueAction(() =>
        {
            TryMoveItem(_item);
        });

    private static void End()
    {
        GameActions.Print($"Automated object delay finished. ({_delay})", Constants.HUE_SUCCESS);
        _item = null;
        _timer?.Stop();
        _timer = null;
        _onCompletion?.Invoke();
        _onCompletion = null;
        EventSink.ClilocMessageReceived -= EventSinkOnClilocMessageReceived;
    }
}
