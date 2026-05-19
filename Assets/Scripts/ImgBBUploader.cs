using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class ImgBBUploader : MonoBehaviour
{
    private string imgbbApiKey = "dae12b6a166fb68a99ff5f77b519ae4a";

    // 定義一個預設的錯誤提示圖片網址 (建議事先上傳一張「圖片無法顯示」的圖到 ImgBB 並將網址貼在這邊)
    private string fallbackImageUrl = "https://placehold.co/600x400/png?text=Upload+Failed";

    public IEnumerator UploadToImgBB(Action<string> onComplete)
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
        byte[] imageBytes = screenTexture.EncodeToJPG(75);
        Destroy(screenTexture);

        WWWForm form = new WWWForm();
        form.AddField("image", System.Convert.ToBase64String(imageBytes));
        form.AddField("name", "shot");

        string uploadURL = $"https://api.imgbb.com/1/upload?key={imgbbApiKey}";

        using (UnityWebRequest www = UnityWebRequest.Post(uploadURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                // 將 LogError 改為 LogWarning，並提示正在使用預設圖片
                Debug.LogWarning("上傳失敗: " + www.error + "，將使用預設圖片繼續進行。");

                // 傳送預設圖片網址，讓後續程式可以順利繼續
                onComplete?.Invoke(fallbackImageUrl);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                ImgBBResponse responseObj = JsonUtility.FromJson<ImgBBResponse>(jsonResponse);

                string cleanUrl = responseObj.data.url;
                Debug.Log("圖片網址: " + cleanUrl);

                onComplete?.Invoke(cleanUrl);
            }
        }
    }
}

[System.Serializable]
public class ImgBBResponse { public ImgBBData data; }

[System.Serializable]
public class ImgBBData { public string url; }


