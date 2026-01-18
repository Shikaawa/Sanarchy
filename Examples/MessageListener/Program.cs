using System;
using System.Threading;
using Discord;
using Discord.Gateway;

namespace MessageLogger
{
    internal class Program
    {
        private static void Main()
        {
            var client = new DiscordSocketClient();
            client.OnLoggedIn += OnLoggedIn;
            client.OnMessageReceived += OnMessageReceived;

            Console.Write("Token: ");
            client.Login(Console.ReadLine());

            Thread.Sleep(Timeout.Infinite);
        }

        private static void OnLoggedIn(DiscordSocketClient client, LoginEventArgs args)
        {
            Console.WriteLine("Logged into " + args.User);
        }

        private static void OnMessageReceived(DiscordSocketClient client, MessageEventArgs args)
        {
            string from;
            DiscordChannel channel = client.GetChannel(args.Message.Channel.Id);

            if (channel.InGuild)
            {
                var textChannel = (TextChannel)channel;
                from = "#" + channel.Name + " / " +
                       client.GetCachedGuild(textChannel.Guild.Id).Name;
            }
            else
            {
                var privChannel = (PrivateChannel)channel;
                from = privChannel.Name ?? privChannel.Recipients[0].ToString();
            }

            Console.WriteLine("[" + from + "] " + args.Message.Author + ": " + args.Message.Content);
        }
    }
}
