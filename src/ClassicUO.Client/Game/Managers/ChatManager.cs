// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Configuration;

namespace ClassicUO.Game.Managers
{
    public sealed class ChatManager
    {
        private readonly World _world;

        public ChatManager(World world)
        {
            _world = world;
        }

        public readonly Dictionary<string, ChatChannel> Channels = new Dictionary<string, ChatChannel>();
        public ChatStatus ChatIsEnabled;
        public string CurrentChannelName = string.Empty;

        private static readonly string[] _messages =
        {
            TazLang.Get("you_are_already_ignoring_maximum"),
            TazLang.Get("you_are_already_ignoring1"),
            TazLang.Get("you_are_now_ignoring1"),
            TazLang.Get("you_are_no_longer_ignoring1"),
            TazLang.Get("you_are_not_ignoring1"),
            TazLang.Get("you_are_no_longer_ignoring_anyone"),
            TazLang.Get("that_is_not_avalid_conference_name"),
            TazLang.Get("there_is_already_aconference"),
            TazLang.Get("you_must_have_operator_status"),
            TazLang.Get("conference1_renamed_to2"),
            TazLang.Get("you_must_be_in_aconference"),
            TazLang.Get("there_is_no_player_named1"),
            TazLang.Get("there_is_no_conference_named1"),
            TazLang.Get("that_is_not_the_correct_password"),
            TazLang.Get("has_chosen_to_ignore_you"),
            TazLang.Get("not_given_you_speaking_privileges"),
            TazLang.Get("you_can_now_receive_pm"),
            TazLang.Get("you_will_no_longer_receive_pm"),
            TazLang.Get("you_are_showing_your_char_name"),
            TazLang.Get("you_are_not_showing_your_char_name"),
            TazLang.Get("is_remaining_anonymous"),
            TazLang.Get("has_chosen_to_not_receive_pm"),
            TazLang.Get("is_known_in_the_lands_of_britannia_as2"),
            TazLang.Get("has_been_kicked_out_of_the_conference"),
            TazLang.Get("aconference_moderator_kicked_you"),
            TazLang.Get("you_are_already_in_the_conference1"),
            TazLang.Get("is_no_longer_aconference_moderator"),
            TazLang.Get("is_now_aconference_moderator"),
            TazLang.Get("has_removed_you_from_moderators"),
            TazLang.Get("has_made_you_aconference_moderator"),
            TazLang.Get("no_longer_has_speaking_privileges"),
            TazLang.Get("now_has_speaking_privileges"),
            TazLang.Get("removed_your_speaking_privileges"),
            TazLang.Get("granted_you_speaking_privileges"),
            TazLang.Get("everyone_will_have_speaking_privs"),
            TazLang.Get("moderators_will_have_speaking_privs"),
            TazLang.Get("password_to_the_conference_changed"),
            TazLang.Get("the_conference_named1_is_full"),
            TazLang.Get("you_are_banning1_from_this_conference"),
            TazLang.Get("banned_you_from_the_conference"),
            TazLang.Get("you_have_been_banned")
        };


        public static string GetMessage(int index) => index < _messages.Length ? _messages[index] : string.Empty;

        public void AddChannel(string text, bool hasPassword)
        {
            if (!Channels.TryGetValue(text, out ChatChannel channel))
            {
                channel = new ChatChannel(text, hasPassword);
                Channels[text] = channel;
            }
        }

        public void RemoveChannel(string name)
        {
            if (Channels.ContainsKey(name))
            {
                Channels.Remove(name);
            }
        }

        public void Clear() => Channels.Clear();

        //static ChatManager()
        //{
        //    using (StreamReader reader = new StreamReader(File.OpenRead(UOFileManager.GetUOFilePath("Chat.enu"))))
        //    {
        //        while (!reader.EndOfStream)
        //        {
        //            string line = reader.ReadLine();
        //        }
        //    }
        //}
    }
}