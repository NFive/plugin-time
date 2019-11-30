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
			});

			// Sync with server now
			this.serverTime = await this.Comms.Event(TimeEvents.Sync).ToServer().Request<TimeSpan>();
			this.previousTime = DateTime.UtcNow;

			this.Ticks.On(UpdateTick);
		}

		private async Task UpdateTick()
		{
			// Get time since last update
			var secondsDiff = (int)(DateTime.UtcNow - this.previousTime).TotalSeconds;
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

			// Set game time
			API.NetworkOverrideClockTime(this.serverTime.Hours, this.serverTime.Minutes, this.serverTime.Seconds);

			await Delay(TimeSpan.FromSeconds(1));
		}
	}
}
