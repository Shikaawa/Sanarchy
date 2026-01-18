using Anarchy;

namespace Discord.Gateway
{
    internal class VoiceStateDictionary : ConcurrentDictionary<ulong, DiscordVoiceStateContainer>
    {
        public new DiscordVoiceStateContainer this[ulong userId]
        {
            get
            {
            	DiscordVoiceStateContainer container;
                if (TryGetValue(userId, out container))
                    return container;
                else
                    return this[userId] = new DiscordVoiceStateContainer(userId);
            }
            set { base[userId] = value; }
        }
    }
}
