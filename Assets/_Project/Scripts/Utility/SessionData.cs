using System;
using System.Collections.Generic;

[Serializable]
public class SessionData
{
    public string sessionID;
    public string startTime;
    public List<AttemptData> attempts;

    public SessionData()
    {
        attempts = new List<AttemptData>();
    }

    public void Init(string deviceName)
    {
        sessionID = deviceName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
