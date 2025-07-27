using UnityEngine;
using ZXing;
using ZXing.QrCode;
using UnityEngine.UI;

public class QrCodeGenerator : MonoBehaviour
{
    [SerializeField] private RawImage rawImageReceiver;
    [SerializeField] private string textToEncode;

    private Texture2D storeEncodedTexture;

    private void Start()
    {
        storeEncodedTexture = new(256, 256);
    }

    private Color32[] Encode(string textForEncoding, int width, int height)
    {
        BarcodeWriter writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Height = height, Width = width }
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
        string textWrite = string.IsNullOrEmpty(textToEncode) ? "nothing here" : textToEncode;
        Color32[] covertPixelToTexture = Encode(textWrite, storeEncodedTexture.width, storeEncodedTexture.height);
        storeEncodedTexture.SetPixels32(covertPixelToTexture);
        storeEncodedTexture.Apply();

        rawImageReceiver.texture = storeEncodedTexture;
    }
}
