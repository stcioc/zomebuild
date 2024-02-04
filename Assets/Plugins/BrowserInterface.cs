using System.Runtime.InteropServices;

public class BrowserInterface
{

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void BrowserTextDownload(string filename, string textContent);
    [DllImport("__Internal")]
    private static extern void BrowserTextUpload(string objectName, string methodName, string extensions);
    [DllImport("__Internal")]
    private static extern int JavascriptConfirm(string message);
    [DllImport("__Internal")]
    private static extern string JavascriptPrompt(string message, string defaultText);

#endif

    public static void TextDownload(string filename, string textContent)
    {
#if UNITY_WEBGL
        BrowserTextDownload(filename, textContent);
#endif
    }
    public static void TextUpload(string objectName, string methodName, string extensions)
    {
#if UNITY_WEBGL
        BrowserTextUpload(objectName, methodName, extensions);
#endif

    }
    public static int Confirm(string message)
    {
#if UNITY_WEBGL
        JavascriptConfirm(message);
#else
        return 0;
#endif
    }
    public static string Prompt(string message, string defaultText)
    {
#if UNITY_WEBGL
        JavascriptPrompt(message);
#else
        return "";
#endif
    }

}
