using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace NFive.Time.Server.Communications
{
	[PublicAPI]
	public interface ITimeManager
	{
		Task<bool> IsRealTimeEnabled();

		Task<TimeSpan> GetServerTime();

		void SetServerTime(TimeSpan time);
	}
}
