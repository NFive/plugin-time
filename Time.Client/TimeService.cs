using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using System;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Interface;
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

		public TimeService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			// Request server configuration
			this.config = await this.Comms.Event(TimeEvents.Configuration).ToServer().Request<Configuration>();

			this.Comms.Event(TimeEvents.Sync).FromServer().On<TimeSpan>((e, t) =>
			{
				this.serverTime = t;
				this.previousTime = DateTime.Now;
			});

			this.serverTime = await this.Comms.Event(TimeEvents.Sync).ToServer().Request<TimeSpan>();
			this.previousTime = DateTime.Now;
			this.Ticks.On(TimeUpdateTick);
		}

		private async Task TimeUpdateTick()
		{
			int secondsDiff = (int)(DateTime.Now - this.previousTime).TotalSeconds;
			if (secondsDiff < 1)
				return;
			this.previousTime = DateTime.Now;

			if (this.config.UseRealTime)
			{
				this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(secondsDiff));
			}
			else
			{
				for (int i = 0; i < secondsDiff; i++)
				{
					if (TimeHelper.IsNightTime(this.serverTime, this.config.NightHours.Start,
						this.config.NightHours.End))
					{
						int elapsedSeconds = (int)Math.Ceiling(this.config.Modifiers.Night);
						this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(elapsedSeconds));
					}
					else
					{
						int elapsedSeconds = (int)Math.Ceiling(this.config.Modifiers.Day);
						this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(elapsedSeconds));
					}
				}
			}

			if (this.serverTime.Days > 0)
				this.serverTime = this.serverTime.Subtract(TimeSpan.FromDays(1));
			API.NetworkOverrideClockTime(this.serverTime.Hours, this.serverTime.Minutes, this.serverTime.Seconds);
			await Delay(TimeSpan.FromSeconds(1));
		}
	}
}
