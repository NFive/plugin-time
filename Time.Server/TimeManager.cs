using System;
using System.Threading.Tasks;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.IoC;
using NFive.Time.Server.Communications;
using NFive.Time.Shared;

namespace NFive.Time.Server
{
	[Component(Lifetime = Lifetime.Singleton)]
	public class TimeManager : ITimeManager
	{
		private ICommunicationManager comms;

		public TimeManager(ICommunicationManager comms)
		{
			this.comms = comms;
		}

		public async Task<bool> IsRealTimeEnabled()
		{
			return await this.comms.Event(TimeEvents.IsRealTime).ToServer().Request<bool>();
		}

		public async Task<TimeSpan> GetServerTime()
		{
			return await this.comms.Event(TimeEvents.Get).ToServer().Request<TimeSpan>();
		}

		public void SetServerTime(TimeSpan time)
		{
			this.comms.Event(TimeEvents.Set).ToServer().Emit(time);
		}

	}
}
