using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Rpc;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using System;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using NFive.Time.Shared;
using NFive.Time.Shared.Utilities;

namespace NFive.Time.Client
{
	[PublicAPI]
	public class TimeService : Service
	{
		private Configuration config;
		private TimeSpan serverTime;
		private DateTime previousTime;

		public TimeService(ILogger logger, ITickManager ticks, IEventManager events, IRpcHandler rpc, ICommandManager commands, OverlayManager overlay, User user) : base(logger, ticks, events, rpc, commands, overlay, user) { }

		public override async Task Started()
		{
			// Request server configuration
			this.config = await this.Rpc.Event(TimeEvents.Configuration).Request<Configuration>();


			// Update local configuration on server configuration change
			this.Rpc.Event(TimeEvents.Configuration).On<Configuration>((e, c) => this.config = c);

			this.Rpc.Event(TimeEvents.Sync).On<TimeSpan>((e, t) =>
			{
				this.serverTime = t;
				this.previousTime = DateTime.Now;
			});

			this.serverTime = await this.Rpc.Event(TimeEvents.Sync).Request<TimeSpan>();
			this.previousTime = DateTime.Now;
			this.Ticks.Attach(TimeUpdateTick);
		}

		private async Task TimeUpdateTick()
		{
			int secondsDiff = DateTime.Now.Second - this.previousTime.Second;
			if (secondsDiff == 0)
				return;
			this.previousTime = DateTime.Now;

			if (this.config.UseRealTime)
			{
				this.serverTime = this.serverTime.Add(new TimeSpan(0, 0, secondsDiff));
			}
			else
			{
				for (int i = 0; i < secondsDiff; i++)
				{
					if (TimeHelper.IsNightTime(this.serverTime, this.config.NightHours.Start,
						this.config.NightHours.End))
					{
						int elapsedSeconds = (int)Math.Ceiling(this.config.Modifiers.Night);
						this.serverTime = this.serverTime.Add(new TimeSpan(0, 0, elapsedSeconds));
					}
					else
					{
						int elapsedSeconds = (int)Math.Ceiling(this.config.Modifiers.Day);
						this.serverTime = this.serverTime.Add(new TimeSpan(0, 0, elapsedSeconds));
					}

					if (this.serverTime.Days > 0)
						this.serverTime = this.serverTime.Subtract(new TimeSpan(1, 0, 0, 0));
				}
			}

			API.NetworkOverrideClockTime(this.serverTime.Hours, this.serverTime.Minutes, this.serverTime.Seconds);

			// this.Logger.Debug("Client time: " + this.serverTime);
			await Delay(TimeSpan.FromSeconds(1));
		}
	}
}
