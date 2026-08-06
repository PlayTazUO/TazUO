// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;

namespace ClassicUO.Game.Data
{
    internal static class ServerErrorMessages
    {
        private static readonly Tuple<int, string>[] _errorCode =
        {
            Tuple.Create(3000018, TazLang.Get("character_password_invalid")),
            Tuple.Create(3000019, TazLang.Get("that_character_does_not_exist")),
            Tuple.Create(3000020, TazLang.Get("that_character_is_being_played")),
            Tuple.Create(3000021, TazLang.Get("character_is_not_old_enough")),
            Tuple.Create(3000022, TazLang.Get("character_is_queued_for_backup")),
            Tuple.Create(3000023, TazLang.Get("couldnt_carry_out_your_request"))
        };

        private static readonly Tuple<int, string>[] _pickUpErrors =
        {
            Tuple.Create(3000267, TazLang.Get("you_can_not_pick_that_up")),
            Tuple.Create(3000268, TazLang.Get("that_is_too_far_away")),
            Tuple.Create(3000269, TazLang.Get("that_is_out_of_sight")),
            Tuple.Create(3000270, TazLang.Get("that_item_does_not_belong_to_you")),
            Tuple.Create(3000271, TazLang.Get("you_are_already_holding_an_item"))
        };

        private static readonly Tuple<int, string>[] _generalErrors =
        {
            Tuple.Create(3000007, TazLang.Get("incorrect_name_password")),
            Tuple.Create(3000034, TazLang.Get("someone_is_already_using_this_account")),
            Tuple.Create(3000035, TazLang.Get("your_account_has_been_blocked")),
            Tuple.Create(3000036, TazLang.Get("your_account_credentials_are_invalid")),
            Tuple.Create(-1, TazLang.Get("communication_problem")),
            Tuple.Create(-1, TazLang.Get("the_igrconcurrency_limit_has_been_met")),
            Tuple.Create(-1, TazLang.Get("the_igrtime_limit_has_been_met")),
            Tuple.Create(-1, TazLang.Get("general_igrauthentication_failure")),
            Tuple.Create(3000037, TazLang.Get("couldnt_connect_to_uo"))
        };

        private static string GetLoginError(ClilocLoader cliloc, byte code, (int min, int max) delay) => code switch
        {
            0 => cliloc.GetString(3000007, TazLang.Get("incorrect_password")),
            1 => cliloc.GetString(3000009, TazLang.Get("character_does_not_exist")),
            2 => cliloc.GetString(3000006, TazLang.Get("character_already_exists")),
            3 => cliloc.GetString(3000016, TazLang.Get("client_could_not_attach_to_server")),
            4 => cliloc.GetString(3000017, TazLang.Get("client_could_not_attach_to_server")),
            5 => cliloc.GetString(3000012, TazLang.Get("another_character_online")),
            6 => cliloc.GetString(3000013, TazLang.Get("error_in_synchronization")),
            7 => cliloc.GetString(3000005, TazLang.Get("idle_too_long")),
            8 => cliloc.GetString(-1, TazLang.Get("could_not_attach_server")),
            9 => cliloc.GetString(-1, TazLang.Get("character_transfer_in_progress")),
            10 => cliloc.GetString(-1, TazLang.Get("name_is_invalid")),
            13 => cliloc.Translate(1161061, $"{delay.min}\t{delay.max}"),
            14 => cliloc.Translate(1161062, $"{delay.min}\t{delay.max}"),
            _ => $"Unkown error #{code}"
        };

        public static string GetError(byte packetID, byte code, (int min, int max) delay = default)
        {
            ClilocLoader cliloc = Client.Game.UO.FileManager.Clilocs;

            switch (packetID)
            {
                case 0x53:
                    return GetLoginError(cliloc, code, delay);

                case 0x85:
                    if (code >= 6)
                    {
                        code = 5;
                    }

                    Tuple<int, string> t = _errorCode[code];

                    return cliloc.GetString(t.Item1, t.Item2);

                case 0x27:
                    if (code >= 5)
                    {
                        code = 4;
                    }

                    t = _pickUpErrors[code];

                    return cliloc.GetString(t.Item1, t.Item2);

                case 0x82:
                    if (code >= 9)
                    {
                        code = 8;
                    }

                    t = _generalErrors[code];

                    return cliloc.GetString(t.Item1, t.Item2);
            }

            return string.Empty;
        }
    }
}