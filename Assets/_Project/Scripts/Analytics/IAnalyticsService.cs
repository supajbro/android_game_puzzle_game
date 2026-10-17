namespace JellyRiot.Analytics
{
    public interface IAnalyticsService
    {
        void RecordEvent(string eventName);
    }
}
