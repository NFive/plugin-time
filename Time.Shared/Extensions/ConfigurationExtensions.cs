using System;

namespace NFive.Time.Shared.Extensions
{
	public static class TimeSpanExtensions
	{
		public static bool IsNightTime(this TimeSpan time, NighttimeConfiguration config)
		{
			if (config.Start < config.End) return config.Start <= time && time <= config.End;

			return !(config.End <= time && time <= config.Start);
		}
	}
}
