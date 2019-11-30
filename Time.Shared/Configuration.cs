using System;
using NFive.SDK.Core.Controllers;

namespace NFive.Time.Shared
{
	public class Configuration : ControllerConfiguration
	{
		public TimeSpan SyncRate { get; set; } = TimeSpan.FromHours(1);

		public NighttimeConfiguration Nighttime { get; set; } = new NighttimeConfiguration();

		public bool RealTime { get; set; } = true;

		public TimeSpan BootTime { get; set; } = TimeSpan.FromHours(12);

		public TimeModifiersConfiguration Modifiers { get; set; } = new TimeModifiersConfiguration();
	}

	public class NighttimeConfiguration
	{
		public TimeSpan Start { get; set; } = TimeSpan.FromHours(19);

		public TimeSpan End { get; set; } = TimeSpan.FromHours(4);
	}

	public class TimeModifiersConfiguration
	{
		public float Day { get; set; } = 1f;

		public float Night { get; set; } = 1f;
	}
}
