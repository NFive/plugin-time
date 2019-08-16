using System;

namespace NFive.Time.Shared.Utilities
{
	public static class TimeHelper
	{
		public static bool IsNightTime(TimeSpan time, TimeSpan start, TimeSpan end)
		{
			if (start < end)
				return start <= time && time <= end;
			return !(end <= time && time <= start);
		}
	}
}
