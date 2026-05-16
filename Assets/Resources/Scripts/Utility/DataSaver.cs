using System.IO;
using UnityEngine;

public class DataSaver : MonoBehaviour
{
    public static DataSaver instance;

    private string savePath;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Path.Combine(Application.persistentDataPath, "PlayData");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Save(SessionData sessionData)
    {
        try
        {
            string json = JsonUtility.ToJson(sessionData, prettyPrint: true);
            string fileName = sessionData.sessionID + ".json";
            string fullPath = Path.Combine(savePath, fileName);
            File.WriteAllText(fullPath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("저장 실패: " + e.Message);
        }
    }
}
