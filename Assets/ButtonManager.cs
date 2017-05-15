using RenderHeads.Media.AVProVideo;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour {

    // Use this for initialization

    string rootpath;
    public Dropdown ui_dropdown;
    public Button[] buttons;
    public MediaPlayer mp;
    public MediaPlayer mpCache;
    public Text text;
    private DirectoryInfo rootd;
    private DirectoryInfo[] rootitemsds;
    public DirectoryInfo[] curitemFiles;
    
    void Start () {
        
#if UNITY_ANDROID && !UNITY_EDITOR
              rootpath = "sdcard/facecheck/";
#else
              rootpath = Application.dataPath +"/../facecheck/";
#endif

        rootd = new DirectoryInfo(rootpath);
        rootitemsds = rootd.GetDirectories("*.*", System.IO.SearchOption.TopDirectoryOnly);
        for (int i = 0; i < rootitemsds.Length; i++)
        {
            Debug.Log(rootitemsds[i].Name);
            ui_dropdown.AddOptions(new List<string>() { rootitemsds[i].Name });
        }
        OnDropDownChange(0);
    }
    DirectoryInfo curSelectDI;
    FileInfo[] cruSelectDIFIS;
    FileInfo curChongFuDongZuo;
    public void OnDropDownChange(int index)
    {
        curSelectDI = rootitemsds[index];
        cruSelectDIFIS = curSelectDI.GetFiles();
        if (buttons != null && buttons.Length > 0)
        {
            int buttonindex = 0;
            for (int i = 0; i < cruSelectDIFIS.Length; i++)
            {
                if (cruSelectDIFIS[i].Name == "chongfudongzuo.mp4")
                {
                    curChongFuDongZuo = cruSelectDIFIS[i];
                }
                else
                {
                    buttons[buttonindex].GetComponentInChildren<Text>().text = cruSelectDIFIS[i].Name;
                    buttons[buttonindex].name = i.ToString();
                    buttonindex += 1;
                }
            }
        }
        
    }

    bool looping = false;
    public void OnButtonClick(GameObject obj)
    {
        if (looping)
            return;
        StopCoroutine(PlayOnEndLoop());
        int index = int.Parse(obj.name);
        FileInfo fi = cruSelectDIFIS[index];
        text.text = fi.FullName;

        if (!mp.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, fi.FullName, true))
        {
            Debug.LogError("Failed to open video!");
        }
        //mp.m_Loop = false;
        StartCoroutine(PlayOnEndLoop());
        //mp.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, fi.FullName, true);
    }

    IEnumerator PlayOnEndLoop()
    {
        looping = true;
        yield return new WaitForSeconds(0.3f);
        _display._mediaPlayer = mp;
        float mpms = mp.Info.GetDurationMs();
        float mplength = mpms / 1000f;
        Debug.Log("mpms:"+ mpms + " mplength:" +mplength);

        yield return new WaitForSeconds(1f);
        if (!mpCache.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, curChongFuDongZuo.FullName, true))
        {
            Debug.LogError("Failed to open video!");
        }
        
        yield return new WaitForSeconds(mplength - 1.3f);

        _display._mediaPlayer = mpCache;
        looping = false;
        yield return null;
    }

    public bool _useFading = true;
    private float _eventTimer = 1f;
    private MediaPlayer.FileLocation _nextVideoLocation;
    private string _nextVideoPath;
    public DisplayUGUI _display;
    private void LoadVideo(string filePath, bool url = false)
    {
        // Set the video file name and to load. 
        if (url)
            _nextVideoLocation = MediaPlayer.FileLocation.AbsolutePathOrURL; 
        else
            _nextVideoLocation = MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder;

        _nextVideoPath = filePath;

        // IF we're not using fading then load the video immediately
        if (_useFading)
        {
            //_display.color = new Color(1,1,1,0);
            // Load the video
            if (!mp.OpenVideoFromFile(_nextVideoLocation, _nextVideoPath, mp.m_AutoStart))
            {
                Debug.LogError("Failed to open video!");
            }
            //StartCoroutine(LoadVideFadeShow());
        }
        else
        {
            StartCoroutine("LoadVideoWithFading");
        }
    }

    IEnumerator LoadVideFadeShow()
    {
        float fade = 0 ;
        while (_display.color.a <1)
        {
            fade += 0.3f;
            _display.color = new Color(1, 1, 1, fade);
            yield return null;
        }
    }

    private IEnumerator LoadVideoWithFading()
    {
        const float FadeDuration = 0.1f;
        float fade = FadeDuration;

        // Fade down
        while (fade > 0f && Application.isPlaying)
        {
            fade -= Time.deltaTime;
            fade = Mathf.Clamp(fade, 0f, FadeDuration);

            _display.color = new Color(1f, 1f, 1f, fade / FadeDuration);
            _display._mediaPlayer.Control.SetVolume(fade / FadeDuration);

            yield return null;
        }

        // Wait 3 frames for display object to update
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Load the video
        if (Application.isPlaying)
        {
            if (!mp.OpenVideoFromFile(_nextVideoLocation, _nextVideoPath, mp.m_AutoStart))
            {
                Debug.LogError("Failed to open video!");
            }
            else
            {
                // Wait for the first frame to come through (could also use events for this)
                while (Application.isPlaying && (VideoIsReady(mp) || AudioIsReady(mp)))
                {
                    yield return null;
                }

                // Wait 3 frames for display object to update
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
            }
        }

        // Fade up
        while (fade < FadeDuration && Application.isPlaying)
        {
            fade += Time.deltaTime;
            fade = Mathf.Clamp(fade, 0f, FadeDuration);

            _display.color = new Color(1f, 1f, 1f, fade / FadeDuration);
            _display._mediaPlayer.Control.SetVolume(fade / FadeDuration);

            yield return null;
        }
    }

    private static bool VideoIsReady(MediaPlayer mp)
    {
        return (mp != null && mp.TextureProducer != null && mp.TextureProducer.GetTextureFrameCount() <= 0);

    }
    private static bool AudioIsReady(MediaPlayer mp)
    {
        return (mp != null && mp.Control != null && mp.Control.CanPlay() && mp.Info.HasAudio() && !mp.Info.HasVideo());
    }
}
