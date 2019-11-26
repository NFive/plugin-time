using System;
using System.Timers;
using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.Controllers;
using NFive.Time.Shared;
using NFive.Time.Shared.Utilities;

namespace NFive.Time.Server
{
	[PublicAPI]
	public class TimeController : ConfigurableController<Configuration>
	{
		private TimeSpan serverTime;
		private Timer timeUpdateTimer;
		private Timer timeBroadcastTimer;
		private DateTime previousTime;
		private ICommunicationManager comms;
		public TimeController(ILogger logger, Configuration configuration, ICommunicationManager comms) : base(logger, configuration)
		{
			this.comms = comms;
			// Send configuration when requested
			this.comms.Event(TimeEvents.Configuration).FromClients().OnRequest(e => e.Reply(this.Configuration));
			this.comms.Event(TimeEvents.Sync).FromClients().OnRequest(e =>
			{
				e.Reply(this.Configuration.UseRealTime
					? new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)
					: this.serverTime);
			});

			this.comms.Event(TimeEvents.IsRealTime).FromServer().OnRequest(e => e.Reply(this.Configuration.UseRealTime));
			this.comms.Event(TimeEvents.Get).FromServer().OnRequest(e => e.Reply(this.serverTime));
			this.comms.Event(TimeEvents.Set).FromServer().On<TimeSpan>((e, t) =>
			{
				if (this.Configuration.UseRealTime) return;
				this.serverTime = t;
				BroadcastTime(null, null);

			});

			this.serverTime = this.Configuration.StartTime;
			this.previousTime = DateTime.Now;

			this.timeUpdateTimer = new Timer()
			{
				AutoReset = true,
				Enabled = true,
				Interval = 1000
			};
			this.timeUpdateTimer.Elapsed += UpdateTime;

			this.timeBroadcastTimer = new Timer()
			{
				AutoReset = true,
				Enabled = true,
				Interval = this.Configuration.TimeSyncRate.TotalMilliseconds
			};
			this.timeBroadcastTimer.Elapsed += BroadcastTime;
		}

		private void BroadcastTime(object sender, ElapsedEventArgs e)
		{
			if (this.Configuration.UseRealTime)
				this.serverTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
			this.comms.Event(TimeEvents.Sync).ToClients().Emit(this.serverTime);
		}

		private void UpdateTime(object sender, ElapsedEventArgs e)
		{
			if(this.Configuration.UseRealTime)
				return;
			int secondsDiff = (int)(DateTime.Now - this.previousTime).TotalSeconds;
			if (secondsDiff < 1)
				return;
			this.previousTime = DateTime.Now;
			for (int i = 0; i < secondsDiff; i++)
			{
				if (TimeHelper.IsNightTime(this.serverTime, this.Configuration.NightHours.Start,
					this.Configuration.NightHours.End))
				{
					int elapsedSeconds = (int) Math.Ceiling(this.Configuration.Modifiers.Night);
					this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(elapsedSeconds));
				}
				else
				{
					int elapsedSeconds = (int) Math.Ceiling(this.Configuration.Modifiers.Day);
					this.serverTime = this.serverTime.Add(TimeSpan.FromSeconds(elapsedSeconds));
				}

				if (this.serverTime.Days > 0)
					this.serverTime = this.serverTime.Subtract(TimeSpan.FromDays(1));
			}
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
