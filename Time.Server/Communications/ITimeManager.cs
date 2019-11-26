using System;
using System.Threading.Tasks;

namespace NFive.Time.Server.Communications
{
	public interface ITimeManager
	{
		Task<bool> IsRealTimeEnabled();
		Task<TimeSpan> GetServerTime();
		void SetServerTime(TimeSpan time);
	}
}
