using System;
using NFive.SDK.Core.Controllers;

namespace NFive.Time.Shared
{
	public class Configuration : ControllerConfiguration
	{
		public bool UseRealTime { get; set; } = true;
		public TimeSpan StartTime { get; set; } = new TimeSpan(12,0,0); 
		public TimeModifiersConfiguration Modifiers { get; set; } = new TimeModifiersConfiguration();
		public NightHoursConfiguration NightHours { get; set; } = new NightHoursConfiguration();
		public TimeSpan TimeSyncRate { get; set; } = new TimeSpan(1, 0, 0); 
	}

	public class TimeModifiersConfiguration
	{
		public float Day { get; set; } = 1f;
		public float Night { get; set; } = 1f;
	}

	public class NightHoursConfiguration
	{
		public TimeSpan Start { get; set; } = new TimeSpan(19, 0, 0);
		public TimeSpan End { get; set; } = new TimeSpan(4, 0, 0);
	}
}
