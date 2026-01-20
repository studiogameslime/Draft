using Firebase.Analytics;
using UnityEngine;

public class FirebaseAnalyticsManager : MonoBehaviour
{
    public static FirebaseAnalyticsManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void LogEvent(string eventName, params Parameter[] parameters)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (eventName != "") return;
        FirebaseAnalytics.LogEvent(eventName, parameters);
#endif
    }
}
