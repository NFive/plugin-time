using System;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using NFive.Time.Shared;
using NFive.Time.Shared.Extensions;

namespace NFive.Time.Client
{
	[PublicAPI]
	public class TimeService : Service
	{
		private Configuration config;
		private TimeSpan serverTime;
		private DateTime previousTime;
		private TimeSpan previousClockTime;

		public TimeService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			// Request server configuration
			this.config = await this.Comms.Event(TimeEvents.Configuration).ToServer().Request<Configuration>();

			// Handle server periodic sync
			this.Comms.Event(TimeEvents.Sync).FromServer().On<TimeSpan>((e, t) =>
			{
				this.serverTime = t;
				this.previousTime = DateTime.UtcNow;
				this.previousClockTime = t;
			});

			// Sync with server now
			this.serverTime = await this.Comms.Event(TimeEvents.Sync).ToServer().Request<TimeSpan>();
			this.previousClockTime = this.serverTime;
			this.previousTime = DateTime.UtcNow;

			this.Ticks.On(UpdateTick);
			this.Ticks.On(ClockUpdateTick);
		}

		private async Task ClockUpdateTick()
		{
			// Get difference between clock time and actual time
			var minutesDiff = (int)Math.Round((this.serverTime - this.previousClockTime).TotalMilliseconds / 60000);
			if (minutesDiff < 1) return;
			// Calculate needed delay to get clock update in a bit less than a second
			var delay = (int)Math.Round(900.0 / minutesDiff);
			for(var i = 0; i < minutesDiff; i++)
			{
				this.previousClockTime = this.previousClockTime.Add(TimeSpan.FromMinutes(1));
				// Set game time
				API.NetworkOverrideClockTime(this.previousClockTime.Hours, this.previousClockTime.Minutes, this.previousClockTime.Seconds);
				await Delay(delay);
			}
		}

		private async Task UpdateTick()
		{
			// Get time since last update
			var secondsDiff = (int)Math.Round((DateTime.UtcNow - this.previousTime).TotalMilliseconds / 1000);
			if (secondsDiff < 1) return;

			this.previousTime = DateTime.UtcNow;

			if (this.config.RealTime)
			{
				this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(secondsDiff));
			}
			else
			{
				for (var i = 0; i < secondsDiff; i++)
				{
					this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(Math.Ceiling(this.serverTime.IsNightTime(this.config.Nighttime) ? this.config.Modifiers.Night : this.config.Modifiers.Day)));
				}
			}

			// Ignore date
			if (this.serverTime.Days > 0) this.serverTime = this.serverTime.Subtract(TimeSpan.FromDays(this.serverTime.Days));

			await Delay(TimeSpan.FromSeconds(1));
		}


	}
}
