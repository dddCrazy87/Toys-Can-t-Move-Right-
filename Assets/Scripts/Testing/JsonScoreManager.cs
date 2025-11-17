using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerRecord
{
    public string name;
    public int score;
    public string skin;
    public string color;
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

    public void AddPlayerRecord(string pName, int pScore, string pSkin, string pColor)
    {
        if (cache == null || cache.records == null)
        {
            LoadFromDiskIfExists();
        }

        cache.records.Add(new PlayerRecord
        {
            name = pName,
            score = pScore,
            skin = pSkin,
            color = pColor
        });

        SaveToDisk();
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
