using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class ImgBBUploader : MonoBehaviour
{
    private string imgbbApiKey = "dae12b6a166fb68a99ff5f77b519ae4a";

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
                Debug.LogError("上傳失敗: " + www.error);
                onComplete?.Invoke(null);
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


