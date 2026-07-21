using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Resources;

namespace ClassicUO.Network.PacketHandlers;

internal static class ChatMessage
{
    public static void Receive(World world, ref StackDataReader p)
    {
        ushort cmd = p.ReadUInt16BE();

        switch (cmd)
        {
            case 0x03E8: // create conference
                p.Skip(4);
                string channelName = p.ReadUnicodeBE();
                bool hasPassword = p.ReadUInt16BE() == 0x31;
                world.ChatManager.CurrentChannelName = channelName;
                world.ChatManager.AddChannel(channelName, hasPassword);

                UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                break;

            case 0x03E9: // destroy conference
                p.Skip(4);
                channelName = p.ReadUnicodeBE();
                world.ChatManager.RemoveChannel(channelName);

                UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                break;

            case 0x03EB: // display enter username window
                world.ChatManager.ChatIsEnabled = ChatStatus.EnabledUserRequest;

                break;

            case 0x03EC: // close chat
                world.ChatManager.Clear();
                world.ChatManager.ChatIsEnabled = ChatStatus.Disabled;

                UIManager.GetGump<ChatGump>()?.Dispose();

                break;

            case 0x03ED: // username accepted, display chat
                p.Skip(4);
                string username = p.ReadUnicodeBE();
                world.ChatManager.ChatIsEnabled = ChatStatus.Enabled;
                AsyncNetClient.Socket.Send_ChatJoinCommand("General");

                break;

            case 0x03EE: // add user
                p.Skip(4);
                ushort userType = p.ReadUInt16BE();
                username = p.ReadUnicodeBE();

                break;

            case 0x03EF: // remove user
                p.Skip(4);
                username = p.ReadUnicodeBE();

                break;

            case 0x03F0: // clear all players
                break;

            case 0x03F1: // you have joined a conference
                p.Skip(4);
                channelName = p.ReadUnicodeBE();
                world.ChatManager.CurrentChannelName = channelName;

                UIManager.GetGump<ChatGump>()?.UpdateConference();

                GameActions.Print(
                    world,
                    string.Format(ResGeneral.YouHaveJoinedThe0Channel, channelName),
                    ProfileManager.CurrentProfile.ChatMessageHue,
                    MessageType.Regular,
                    1
                );

                break;

            case 0x03F4:
                p.Skip(4);
                channelName = p.ReadUnicodeBE();

                GameActions.Print(
                    world,
                    string.Format(ResGeneral.YouHaveLeftThe0Channel, channelName),
                    ProfileManager.CurrentProfile.ChatMessageHue,
                    MessageType.Regular,
                    1
                );

                break;

            case 0x0025:
            case 0x0026:
            case 0x0027:
                p.Skip(4);
                ushort msgType = p.ReadUInt16BE();
                username = p.ReadUnicodeBE();
                string msgSent = p.ReadUnicodeBE();

                if (ProfileManager.CurrentProfile.StripChatUsernameId && !string.IsNullOrEmpty(username) && username[0] == '<')
                {
                    int idEnd = username.IndexOf('>');

                    if (idEnd > 0)
                        username = username.Substring(idEnd + 1);
                }

                if (!string.IsNullOrEmpty(msgSent))
                {
                    int idx = msgSent.IndexOf('{');
                    int idxLast = msgSent.IndexOf('}') + 1;

                    if (idxLast > idx && idx > -1)
                        msgSent = msgSent.Remove(idx, idxLast - idx);
                }

                //Color c = new Color(49, 82, 156, 0);
                world.MessageManager.HandleMessage(null, msgSent, username,
                    ProfileManager.CurrentProfile.ChatMessageHue, MessageType.ChatSystem, 3, TextType.OBJECT, true);

                //GameActions.Print($"{username}: {msgSent}", ProfileManager.CurrentProfile.ChatMessageHue, MessageType.ChatSystem, 1);
                break;

            default:
                if ((cmd >= 0x0001 && cmd <= 0x0024) || (cmd >= 0x0028 && cmd <= 0x002C))
                {
                    // TODO: read Chat.enu ?
                    // http://docs.polserver.com/packets/index.php?Packet=0xB2

                    string msg = ChatManager.GetMessage(cmd - 1);

                    if (string.IsNullOrEmpty(msg))
                        return;

                    p.Skip(4);
                    string text = p.ReadUnicodeBE();

                    if (!string.IsNullOrEmpty(text))
                    {
                        int idx = msg.IndexOf("%1");

                        if (idx >= 0)
                            msg = msg.Replace("%1", text);

                        if (cmd - 1 == 0x000A || cmd - 1 == 0x0017)
                        {
                            idx = msg.IndexOf("%2");

                            if (idx >= 0)
                                msg = msg.Replace("%2", text);
                        }
                    }

                    GameActions.Print(world, msg, ProfileManager.CurrentProfile.ChatMessageHue, MessageType.ChatSystem,
                        1);
                }

                break;
        }
    }
}
