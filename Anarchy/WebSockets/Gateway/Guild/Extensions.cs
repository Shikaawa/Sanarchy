using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Gateway
{
    public static class GuildMemberExtensions
    {
        public static IReadOnlyList<SocketGuild> GetCachedGuilds(this DiscordSocketClient client)
        {
            if (!client.Config.Cache)
                throw new NotSupportedException("Caching is disabled for this client.");

            lock (client.GuildCache.Lock)
                return client.GuildCache.Values.ToList();
        }

        public static SocketGuild GetCachedGuild(this DiscordSocketClient client, ulong guildId)
        {
            if (!client.Config.Cache)
                throw new NotSupportedException("Caching is disabled for this client.");

            try
            {
                return client.GuildCache[guildId];
            }
            catch (KeyNotFoundException)
            {
                throw new DiscordHttpException(
                    new DiscordHttpError(DiscordError.UnknownGuild, "Guild was not found in the cache"));
            }
        }

        public static ClientGuildSettings GetGuildSettings(this DiscordSocketClient client, ulong guildId)
        {
            client.GetCachedGuild(guildId);

            ClientGuildSettings settings;
            if (client.GuildSettings.TryGetValue(guildId, out settings))
                return settings;

            return null;
        }

        public static DiscordChannelSettings GetChannelSettings(this DiscordSocketClient client, ulong channelId)
        {
            foreach (var settings in client.PrivateChannelSettings)
                if (settings.Id == channelId)
                    return settings;

            foreach (var guildSettings in client.GuildSettings.Values)
                foreach (var channel in guildSettings.ChannelOverrides)
                    if (channel.Id == channelId)
                        return channel;

            return null;
        }

        public static Task<IReadOnlyList<GuildMember>> GetGuildMembersAsync(
            this DiscordSocketClient client, ulong guildId, uint limit)
        {
            List<GuildMember> members = new List<GuildMember>();
            TaskCompletionSource<IReadOnlyList<GuildMember>> tcs =
                new TaskCompletionSource<IReadOnlyList<GuildMember>>();

            EventHandler<GuildMembersEventArgs> handler = null;

            handler = delegate (DiscordSocketClient c, GuildMembersEventArgs args)
            {
                if (args.GuildId != guildId)
                    return;

                members.AddRange(args.Members);

                if (args.Index + 1 == args.Total)
                {
                    client.OnGuildMembersReceived -= handler;
                    tcs.SetResult(members);
                }
            };

            client.OnGuildMembersReceived += handler;
            client.Send(GatewayOpcode.RequestGuildMembers,
                new GuildMemberQuery { GuildId = guildId, Limit = limit });

            return tcs.Task;
        }

        public static IReadOnlyList<GuildMember> GetGuildMembers(
            this DiscordSocketClient client, ulong guildId, uint limit)
        {
            return client.GetGuildMembersAsync(guildId, limit)
                         .GetAwaiter()
                         .GetResult();
        }

        private static void SetGuildSubscriptions(
            this DiscordSocketClient client,
            ulong guildId,
            GuildSubscriptionProperties properties)
        {
            properties.GuildId = guildId;
            client.Send(GatewayOpcode.GuildSubscriptions, properties);
        }

        private static int[][] CreateChunks(int from, bool more)
        {
            int[][] results = new int[more ? 3 : 1][];

            results[0] = new[] { 0, 99 };

            if (more)
            {
                for (int i = 1; i <= 2; i++)
                {
                    results[i] = new[] { from, from + 99 };
                    from += 100;
                }
            }

            return results;
        }

        public static void SubscribeToGuildEvents(
            this DiscordSocketClient client, ulong guildId)
        {
            GuildSubscriptionProperties props = new GuildSubscriptionProperties();
            props.Typing = true;
            props.Activities = true;
            props.Threads = true;

            SetGuildSubscriptions(client, guildId, props);
        }

        public static Task<IReadOnlyList<GuildMember>> GetGuildChannelMembersAsync(
            this DiscordSocketClient client,
            ulong guildId,
            ulong channelId,
            uint limit)
        {
            TaskCompletionSource<IReadOnlyList<GuildMember>> tcs =
                new TaskCompletionSource<IReadOnlyList<GuildMember>>();

            List<GuildMember> members = new List<GuildMember>();

            EventHandler<LoginEventArgs> loginHandler = null;
            EventHandler<DiscordMemberListUpdate> handler = null;

            loginHandler = delegate
            {
                members.Clear();

                GuildSubscriptionProperties props = new GuildSubscriptionProperties();
                props.Activities = true;
                props.Typing = true;
                props.Threads = true;
                props.Channels.Add(channelId, CreateChunks(0, false));

                client.SetGuildSubscriptions(guildId, props);
            };

            handler = delegate (DiscordSocketClient s, DiscordMemberListUpdate e)
            {
                if (e.Guild.Id != guildId)
                    return;

                try
                {
                    foreach (var op in e.Operations)
                    {
                        if (op.Type != "SYNC")
                            continue;

                        foreach (var item in op.Items)
                            if (item.Member != null)
                                members.Add(item.Member);

                        if (op.Items.Count < 100 ||
                            (limit > 0 && members.Count >= limit))
                        {
                            client.OnMemberListUpdate -= handler;
                            client.OnLoggedIn -= loginHandler;

                            tcs.SetResult(
                                limit > 0
                                    ? members.Take((int)limit).ToList()
                                    : members);

                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            client.OnMemberListUpdate += handler;
            client.OnLoggedIn += loginHandler;

            loginHandler(client, null);

            return tcs.Task;
        }

        public static IReadOnlyList<GuildMember> GetGuildChannelMembers(
            this DiscordSocketClient client,
            ulong guildId,
            ulong channelId,
            uint limit)
        {
            return client.GetGuildChannelMembersAsync(guildId, channelId, limit)
                         .GetAwaiter()
                         .GetResult();
        }
    }
}
