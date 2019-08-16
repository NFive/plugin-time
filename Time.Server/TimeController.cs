using System;
using System.Timers;
using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Rcon;
using NFive.SDK.Server.Rpc;
using NFive.Time.Shared;
using NFive.Time.Shared.Utilities;

namespace NFive.Time.Server
{
	[PublicAPI]
	public class TimeController : ConfigurableController<Configuration>
	{
		private TimeSpan serverTime;
		private Timer timeUpdateTimer;
		private DateTime previousTime;

		public TimeController(ILogger logger, IEventManager events, IRpcHandler rpc, IRconManager rcon, Configuration configuration) : base(logger, events, rpc, rcon, configuration)
		{
			// Send configuration when requested
			this.Rpc.Event(TimeEvents.Configuration).On(e => e.Reply(this.Configuration));

			this.Rpc.Event(TimeEvents.RequestTime).On(e =>
			{
				e.Reply(this.Configuration.UseRealTime
					? new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)
					: this.serverTime);
			});

			if(!TimeSpan.TryParse(this.Configuration.StartTime, out this.serverTime))
				throw new Exception("Invalid default starting serverTime provided in the configuration file");
			this.previousTime = DateTime.Now;
			timeUpdateTimer = new Timer()
			{
				AutoReset = true,
				Enabled = true,
				Interval = 1000,
			};
			timeUpdateTimer.Elapsed += UpdateTime;
		}

		private void UpdateTime(object sender, ElapsedEventArgs e)
		{
			if(this.Configuration.UseRealTime)
				return;
			int secondsDiff = DateTime.Now.Second - this.previousTime.Second;
			if(secondsDiff == 0)
				return;
			this.previousTime = DateTime.Now;
			for (int i = 0; i < secondsDiff; i++)
			{
				if (TimeHelper.IsNightTime(this.serverTime, this.Configuration.NightHours.Start,
					this.Configuration.NightHours.End))
				{
					int elapsedSeconds = (int) Math.Ceiling(this.Configuration.Modifiers.Night);
					this.serverTime = this.serverTime.Add(new TimeSpan(0, 0, elapsedSeconds));
				}
				else
				{
					int elapsedSeconds = (int) Math.Ceiling(this.Configuration.Modifiers.Day);
					this.serverTime = this.serverTime.Add(new TimeSpan(0, 0, elapsedSeconds));
				}

				if (this.serverTime.Days > 0)
					this.serverTime = this.serverTime.Subtract(new TimeSpan(1, 0, 0, 0));
			}

			// this.Logger.Debug("Server time: " + this.serverTime);
		}

		public override void Reload(Configuration configuration)
		{
			// Update local configuration
			base.Reload(configuration);

			// Send out new configuration
			this.Rpc.Event(TimeEvents.Configuration).Trigger(this.Configuration);
		}
	}
}
