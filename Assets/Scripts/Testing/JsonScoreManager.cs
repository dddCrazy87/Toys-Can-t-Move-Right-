using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class PlayerRecord
{
    public string name;
    public int score;
    public string skin;
    public string color;
    public string levelName;
    public string date;
}

[Serializable]
public class PlayerRecordList
{
    public List<PlayerRecord> records = new List<PlayerRecord>();
}

public class JsonScoreManager : MonoBehaviour
{
    public enum PathMode
    {
        UsePersistentDataPath,
        UseAbsolutePath
    }

    [Header("檔案路徑設定")]
    public PathMode pathMode = PathMode.UsePersistentDataPath;

    [Tooltip("UsePersistentDataPath: 這是子資料夾名稱\nUseAbsolutePath: 這是完整資料夾路徑")]
    public string folderPath = "";

    public string fileName = "scores.json";

    // 🚨 加入事件：讀取完成後會被呼叫
    public event Action OnLoadFinished;

    private PlayerRecordList cache = new PlayerRecordList();

    private void Awake()
    {
        LoadFromDiskIfExists();
        OnLoadFinished?.Invoke();  // 🔔 呼叫事件
    }

    // ------------------------------------------------------------
    // 對外 API
    // ------------------------------------------------------------

    public void AddPlayerRecord(string pName, int pScore, string pSkin, string pColor, string pLevelName = "")
    {
        if (cache == null || cache.records == null)
        {
            LoadFromDiskIfExists();
        }

        var record = new PlayerRecord
        {
            name = pName,
            score = pScore,
            skin = pSkin,
            color = pColor,
            levelName = pLevelName,
            date = System.DateTime.Now.ToString("yyyy-MM-dd")
        };

        cache.records.Add(record);
        SaveToDisk();

        // 同步寫入 Firebase
        StartCoroutine(PostToFirebase(record));
    }

    [Header("Firebase 設定")]
    [Tooltip("Firebase Realtime Database URL（在 Inspector 中設定）")]
    public string firebaseUrl = "";

    private IEnumerator PostToFirebase(PlayerRecord record)
    {
        if (string.IsNullOrWhiteSpace(firebaseUrl) || !firebaseUrl.StartsWith("http"))
        {
            Debug.LogWarning("[Firebase] URL 未設定或格式不正確，跳過雲端寫入。請在 Inspector 中設定 Firebase URL。");
            yield break;
        }
        string levelKey = string.IsNullOrEmpty(record.levelName) ? "unknown" : record.levelName;
        string url = $"{firebaseUrl.TrimEnd('/')}/leaderboard/{levelKey}.json";
        string json = JsonUtility.ToJson(record);

        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Firebase] 寫入失敗: {request.error}");
        }
        else
        {
            Debug.Log($"[Firebase] 寫入成功: {record.name} - {record.score} ({record.levelName})");
        }
    }

    public List<PlayerRecord> GetAllRecords()
    {
        if (cache == null || cache.records == null)
        {
            LoadFromDiskIfExists();
        }
        return cache.records;
    }

    public void ClearAllRecords()
    {
        cache = new PlayerRecordList();
        SaveToDisk();
    }

    // ------------------------------------------------------------
    // 內部：路徑處理
    // ------------------------------------------------------------

    private string GetFullFilePath()
    {
        string dir;

        if (pathMode == PathMode.UsePersistentDataPath)
        {
            dir = string.IsNullOrEmpty(folderPath)
                ? Application.persistentDataPath
                : Path.Combine(Application.persistentDataPath, folderPath);
        }
        else
        {
            dir = folderPath;
        }

        return Path.Combine(dir, fileName);
    }

    // ------------------------------------------------------------
    // 內部：讀取 / 寫入
    // ------------------------------------------------------------

    private void LoadFromDiskIfExists()
    {
        string fullPath = GetFullFilePath();
        string dir = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(dir))
        {
            cache = new PlayerRecordList();
            return;
        }

        if (File.Exists(fullPath))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                var loaded = JsonUtility.FromJson<PlayerRecordList>(json);
                cache = loaded ?? new PlayerRecordList();
            }
            catch
            {
                cache = new PlayerRecordList();
            }
        }
        else
        {
            cache = new PlayerRecordList();
        }
    }

    private void SaveToDisk()
    {
        string fullPath = GetFullFilePath();
        string dir = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        try
        {
            string json = JsonUtility.ToJson(cache, true);
            File.WriteAllText(fullPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"寫入 JSON 失敗：{e.Message}");
        }
    }
}
