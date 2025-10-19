using UnityEngine;
using ZXing;
using ZXing.QrCode;
using UnityEngine.UI;

public class QrCodeGenerator : MonoBehaviour
{
    [SerializeField] private RawImage rawImageReceiver;
    [SerializeField] private string textToEncode;

    private Texture2D storeEncodedTexture;

    private void Awake()
    {
        storeEncodedTexture = new(256, 256);
    }

    private Color32[] Encode(string textForEncoding, int width, int height)
    {
        var options = new QrCodeEncodingOptions
        {
            Height = height,
            Width = width,
            Margin = 1
        };

        BarcodeWriter writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = options
        };

        return writer.Write(textForEncoding);
    }

    public void EncodeTextToQrCode(string text = "")
    {
        textToEncode = text;
        EncodeText2QrCode();
    }

    private void EncodeText2QrCode()
    {
        if (storeEncodedTexture == null)
        {
            storeEncodedTexture = new(256, 256);
        }

        string textWrite = string.IsNullOrEmpty(textToEncode) ? "nothing here" : textToEncode;
        Color32[] covertPixelToTexture = Encode(textWrite, storeEncodedTexture.width, storeEncodedTexture.height);
        storeEncodedTexture.SetPixels32(covertPixelToTexture);
        storeEncodedTexture.Apply();

        if (rawImageReceiver != null)
        {
            rawImageReceiver.texture = storeEncodedTexture;
        }
        else
        {
            Debug.LogError("QrCodeGenerator: Raw Image Receiver did not set！");
        }
    }
}
