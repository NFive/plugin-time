using NFive.SDK.Core.Controllers;

namespace NFive.Time.Shared
{
	public class Configuration : ControllerConfiguration
	{
		public bool UseRealTime { get; set; } = true;
		public string StartTime { get; set; } = "12:00";
		public TimeModifiersConfiguration Modifiers { get; set; } = new TimeModifiersConfiguration();
		public NightHoursConfiguration NightHours { get; set; } = new NightHoursConfiguration();
		public int TimeSyncRate { get; set; } = 60000;
	}

	public class TimeModifiersConfiguration
	{
		public float Day { get; set; } = 1f;
		public float Night { get; set; } = 1f;
	}

	public class NightHoursConfiguration
	{
		public string Start { get; set; } = "19:00";
		public string End { get; set; } = "04:00";
	}
}
