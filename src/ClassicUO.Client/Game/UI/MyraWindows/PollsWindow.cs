using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Controls.ResizableComponents;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Displays the community polls hosted in the TazUO Firebase realtime database and lets the user vote.
/// Single-choice polls (type 0) present the options as a radio-style group; multiple-choice polls
/// (type 1) allow any number of options. Once the user votes on a poll — tracked per-profile via
/// <see cref="Profile.VotedPolls"/> — that poll is shown as read-only results instead of a voting form.
/// </summary>
public sealed class PollsWindow : MyraControl
{
    private readonly VerticalStackPanel _contentPanel = new() { Spacing = 8 };
    private Dictionary<string, Poll> _polls;
    private bool _loading;
    private ScrollViewer _mainScroll;

    public PollsWindow() : base(TazLang.Get("polls_title", "TazUO Polls"))
    {
        CanBeSaved = true;
        Build();
        CenterInViewPort();
        LoadPolls();
    }

    public static void Show()
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is PollsWindow w && !w.IsDisposed)
            {
                w.BringOnTop();
                return;
            }
        }

        UIManager.Add(new PollsWindow());
    }

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, Padding = new Thickness(6) };

        var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toolbar.Widgets.Add(new MyraButton(TazLang.Get("polls_refresh", "Refresh"), LoadPolls));
        root.Widgets.Add(toolbar);

        root.Widgets.Add(_mainScroll = new ScrollViewer { MaxHeight = 500, Content = _contentPanel });

        SetRootContent(root);

        _rootWindow.Resized += OnResized;
    }

    private void OnResized(object sender, ResizeEventArgs e)
    {
        _mainScroll.MaxHeight = _rootWindow.Height;
        RebuildList();
    }

    private void LoadPolls()
    {
        if (_loading)
            return;

        _loading = true;
        _contentPanel.Widgets.Clear();
        _contentPanel.Widgets.Add(new MyraLabel(TazLang.Get("polls_loading", "Loading polls..."), MyraLabel.TextStyle.P));

        Task.Run(async () =>
        {
            Dictionary<string, Poll> polls = await FirebasePollsManager.FetchPollsAsync();

            MainThreadQueue.InvokeOnMainThread(() =>
            {
                _loading = false;

                if (IsDisposed)
                    return;

                _polls = polls;
                RebuildList();
            });
        });
    }

    private void RebuildList()
    {
        _contentPanel.Widgets.Clear();

        if (_polls == null)
        {
            _contentPanel.Widgets.Add(new MyraLabel(
                TazLang.Get("polls_load_failed", "Failed to load polls. Please try again later."),
                MyraLabel.TextStyle.P));
            return;
        }

        if (_polls.Count == 0)
        {
            _contentPanel.Widgets.Add(new MyraLabel(
                TazLang.Get("polls_none", "There are no polls available right now."),
                MyraLabel.TextStyle.P));
            return;
        }

        foreach (KeyValuePair<string, Poll> kvp in _polls)
            _contentPanel.Widgets.Add(BuildPollWidget(kvp.Key, kvp.Value));
    }

    private Widget BuildPollWidget(string pollId, Poll poll)
    {
        var panel = new VerticalStackPanel
        {
            Spacing = 4,
            Padding = new Thickness(8),
            Background = new SolidBrush(new Color(0, 0, 0, 60)),
            Border = new SolidBrush(MyraStyle.GridBorderColor),
            BorderThickness = new Thickness(1),
            MaxWidth = 1000
        };

        panel.Widgets.Add(new MyraLabel(poll.Question, MyraLabel.TextStyle.H4));

        AddAttachments(panel, poll);

        if (poll.Options == null || poll.Options.Count == 0)
        {
            panel.Widgets.Add(new MyraLabel(TazLang.Get("polls_no_options", "This poll has no options."), MyraLabel.TextStyle.P));
            return panel;
        }

        if (HasVoted(pollId))
            AddResults(panel, poll);
        else
            AddVotingForm(panel, pollId, poll);

        return panel;
    }

    /// <summary>
    /// Renders any attachments on the poll: URLs become clickable links, images are downloaded
    /// asynchronously and shown inline. Malformed attachments are already filtered out during parsing,
    /// so a poll with no valid attachments simply adds nothing here.
    /// </summary>
    private static void AddAttachments(VerticalStackPanel panel, Poll poll)
    {
        if (poll.Attachments == null || poll.Attachments.Count == 0)
            return;

        foreach (PollAttachment attachment in poll.Attachments)
        {
            switch (attachment.Type)
            {
                case AttachmentType.Url:
                    panel.Widgets.Add(new LinkLabel(attachment.Data, attachment.Data, MyraLabel.TextStyle.P));
                    break;

                case AttachmentType.Image:
                    var img = new MyraExternalImage(attachment.Data, 1000);
                    img.TouchLeft += (s, e) =>
                    {
                        PlatformHelper.LaunchBrowser(attachment.Data);
                    };
                    panel.Widgets.Add(img);
                    break;
            }
        }
    }

    private static void AddResults(VerticalStackPanel panel, Poll poll)
    {
        int total = poll.Options.Sum(o => o.Votes);

        foreach (PollOption opt in poll.Options)
        {
            int percent = total > 0 ? (int)Math.Round(opt.Votes * 100.0 / total) : 0;
            panel.Widgets.Add(new MyraLabel($"{opt.Label} — {opt.Votes} ({percent}%)", MyraLabel.TextStyle.P));
        }

        panel.Widgets.Add(new MyraLabel(
            TazLang.Get("polls_already_voted", "You have already voted on this poll."),
            MyraLabel.TextStyle.H6));
    }

    private void AddVotingForm(VerticalStackPanel panel, string pollId, Poll poll)
    {
        bool singleChoice = poll.Type == 0;
        var choices = new List<(PollOption option, MyraCheckButton cb)>();

        foreach (PollOption option in poll.Options)
        {
            var cb = new MyraCheckButton(option.Label);

            if (singleChoice)
            {
                cb.IsCheckedChanged += (_, _) =>
                {
                    if (!cb.IsChecked)
                        return;

                    // Radio-style behaviour: selecting one option clears the others.
                    foreach ((PollOption _, MyraCheckButton other) in choices)
                    {
                        if (!ReferenceEquals(other, cb))
                            other.IsChecked = false;
                    }
                };
            }

            choices.Add((option, cb));
            panel.Widgets.Add(cb);
        }

        panel.Widgets.Add(new MyraButton(
            TazLang.Get("polls_vote", "Vote"),
            () => SubmitVote(pollId, poll, choices)));
    }

    private void SubmitVote(string pollId, Poll poll, List<(PollOption option, MyraCheckButton cb)> choices)
    {
        List<PollOption> selected = choices.Where(c => c.cb.IsChecked).Select(c => c.option).ToList();

        if (selected.Count == 0)
        {
            new MyraDialog(
                TazLang.Get("polls_title", "TazUO Polls"),
                new MyraLabel(TazLang.Get("polls_select_prompt", "Please select an option before voting."), MyraLabel.TextStyle.P),
                _ => { });
            return;
        }

        Task.Run(async () =>
        {
            bool allOk = true;
            string error = null;
            foreach (PollOption option in selected)
            {
                VoteResult result = await FirebasePollsManager.VoteAsync(pollId, option);
                if (!result.Success)
                {
                    allOk = false;
                    error = result.Error;
                    break;
                }
            }

            MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (IsDisposed)
                    return;

                if (allOk)
                {
                    // Reflect our own votes immediately so the results view is accurate without a re-fetch.
                    foreach (PollOption option in selected)
                        option.Votes++;

                    MarkVoted(pollId);
                }
                else
                {
                    string message = TazLang.Get("polls_vote_failed", "Your vote could not be recorded. Please try again.");
                    if (!string.IsNullOrEmpty(error))
                        message += $"\n\n{error}";

                    new MyraDialog(
                        TazLang.Get("polls_title", "TazUO Polls"),
                        new MyraLabel(message, MyraLabel.TextStyle.P),
                        _ => { });
                }

                RebuildList();
            });
        });
    }

    private static bool HasVoted(string pollId)
    {
        Profile profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return false;

        return (profile.VotedPolls ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Contains(pollId);
    }

    private static void MarkVoted(string pollId)
    {
        Profile profile = ProfileManager.CurrentProfile;
        if (profile == null || HasVoted(pollId))
            return;

        string voted = profile.VotedPolls ?? string.Empty;
        profile.VotedPolls = string.IsNullOrEmpty(voted) ? pollId : $"{voted};{pollId}";
    }
}
