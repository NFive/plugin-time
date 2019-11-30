using System;
using System.Timers;
using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.Controllers;
using NFive.Time.Shared;
using NFive.Time.Shared.Extensions;

namespace NFive.Time.Server
{
	[PublicAPI]
	public class TimeController : ConfigurableController<Configuration>
	{
		private readonly ICommunicationManager comms;
		private readonly Timer timeUpdateTimer;
		private readonly Timer timeBroadcastTimer;
		private TimeSpan serverTime;
		private DateTime previousTime;

		public TimeController(ILogger logger, Configuration configuration, ICommunicationManager comms) : base(logger, configuration)
		{
			this.comms = comms;

			// Send configuration when requested
			this.comms.Event(TimeEvents.Configuration).FromClients().OnRequest(e => e.Reply(this.Configuration));

			// Send current time when requested
			this.comms.Event(TimeEvents.Sync).FromClients().OnRequest(e => e.Reply(this.Configuration.RealTime ? DateTime.UtcNow.TimeOfDay : this.serverTime));

			// Send sync configuration to other plugins when requested
			this.comms.Event(TimeEvents.IsRealTime).FromServer().OnRequest(e => e.Reply(this.Configuration.RealTime));

			// Send current time to other plugins when requested
			this.comms.Event(TimeEvents.Get).FromServer().OnRequest(e => e.Reply(this.serverTime));

			// Listen for time update requests from other plugins
			this.comms.Event(TimeEvents.Set).FromServer().On<TimeSpan>((e, t) =>
			{
				if (this.Configuration.RealTime) return;

				this.serverTime = t;

				Broadcast();
			});

			// Setup start time
			this.serverTime = this.Configuration.BootTime;
			this.previousTime = DateTime.UtcNow;

			// Start timers
			this.timeUpdateTimer = new Timer
			{
				AutoReset = true,
				Enabled = true,
				Interval = 1000
			};
			this.timeUpdateTimer.Elapsed += Update;

			this.timeBroadcastTimer = new Timer
			{
				AutoReset = true,
				Enabled = true,
				Interval = this.Configuration.SyncRate.TotalMilliseconds
			};
			this.timeBroadcastTimer.Elapsed += (s, e) => Broadcast();
		}

		private void Broadcast()
		{
			if (this.Configuration.RealTime) this.serverTime = DateTime.UtcNow.TimeOfDay;

			this.comms.Event(TimeEvents.Sync).ToClients().Emit(this.serverTime);
		}

		private void Update(object sender, ElapsedEventArgs e)
		{
			if (this.Configuration.RealTime) return;

			// Get time since last update
			var secondsDiff = (int)Math.Round((DateTime.UtcNow - this.previousTime).TotalMilliseconds / 1000);
			if (secondsDiff < 1) return;

			this.previousTime = DateTime.UtcNow;

			for (var i = 0; i < secondsDiff; i++)
			{
				this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(Math.Ceiling(this.serverTime.IsNightTime(this.Configuration.Nighttime) ? this.Configuration.Modifiers.Night : this.Configuration.Modifiers.Day)));
			}

			// Ignore date
			if (this.serverTime.Days > 0) this.serverTime = this.serverTime.Subtract(TimeSpan.FromDays(this.serverTime.Days));
		}

		public override void Reload(Configuration configuration)
		{
			// Update local configuration
			base.Reload(configuration);

			// Send out new configuration
			this.comms.Event(TimeEvents.Configuration).ToClients().Emit(configuration);
		}
	}
}
