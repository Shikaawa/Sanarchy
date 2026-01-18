using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Discord.Media
{
    public class IncomingVoiceStream
    {
        internal DiscordMediaConnection Session { get; private set; }
        public ulong UserId { get; private set; }

        private readonly ConcurrentQueue<DiscordVoicePacket> _packets;

        private event EventHandler _newPacket;
        private event EventHandler _onClose;

        internal int SilenceFramesReceived { get; set; }

        public bool Closed { get; private set; }
        public int QueuedPackets
        {
            get { return _packets.Count; }
        }

        internal IncomingVoiceStream(DiscordMediaConnection session, ulong userId)
        {
            _packets = new ConcurrentQueue<DiscordVoicePacket>();
            Session = session;
            UserId = userId;
        }

        internal void Enqueue(DiscordVoicePacket packet)
        {
            SilenceFramesReceived = 0;
            _packets.Enqueue(packet);
            _newPacket?.Invoke(this, new EventArgs());
        }

        internal void Close()
        {
            if (!Closed)
            {
                Closed = true;
                _onClose?.Invoke(this, new EventArgs());
            }
        }

        public Task<DiscordVoicePacket> ReadAsync()
		{
		    DiscordVoicePacket packet;
		    if (_packets.TryDequeue(out packet))
		        return Task.FromResult(packet);
		
		    if (Session.State != MediaConnectionState.Ready || Closed)
		        throw new InvalidOperationException("The parent session or this receiver has been closed.");
		
		    var tcs = new TaskCompletionSource<DiscordVoicePacket>();
		
		    EventHandler packetHandler = null;
		    EventHandler closeHandler = null;
		
		    packetHandler = delegate
		    {
		        _newPacket -= packetHandler;
		        _onClose -= closeHandler;
		
		        // Chain the async call properly
		        ReadAsync().ContinueWith(t =>
		        {
		            if (t.IsFaulted)
		                tcs.SetException(t.Exception.InnerException);
		            else if (t.IsCanceled)
		                tcs.SetCanceled();
		            else
		                tcs.SetResult(t.Result);
		        }, TaskContinuationOptions.ExecuteSynchronously);
		    };
		
		    closeHandler = delegate
		    {
		        _newPacket -= packetHandler;
		        _onClose -= closeHandler;
		
		        tcs.SetException(
		            new InvalidOperationException("The parent session or this receiver has been closed.")
		        );
		    };
		
		    _newPacket += packetHandler;
		    _onClose += closeHandler;
		
		    return tcs.Task;
		}

        public DiscordVoicePacket Read()
        {
            return ReadAsync().GetAwaiter().GetResult();
        }
    }
}
