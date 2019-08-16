using System;

namespace NFive.Time.Shared.Utilities
{
	public static class TimeHelper
	{
		public static bool IsNightTime(TimeSpan time, string start, string end)
		{
			if (!TimeSpan.TryParse(start, out var nightStart))
				throw new Exception("Invalid night start time.");
			if (!TimeSpan.TryParse(end, out var nightEnd))
				throw new Exception("Invalid night end time.");
			if (nightStart < nightEnd)
				return nightStart <= time && time <= nightEnd;
			return !(nightEnd <= time && time <= nightStart);
		}
	}
}
