using Discord.Sdk;

namespace DiscordSocialSDK.Wrapper;

public static class Extensions
{
    public static bool IsOnline(this UserHandle user) => user.Status() != StatusType.Offline && user.Status() != StatusType.Invisible;
}
