using System;
using UnityEngine;

// 플레이어 설정 (볼륨, 화면 모드). PlayerPrefs 에 저장되고 바뀌면 Changed 이벤트로 알린다.
public static class GameSettings
{
    public const int VolumeSteps = 10;

    private const string BgmKey = "Settings.BgmVolume";
    private const string SfxKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";

    private static bool loaded;
    private static int bgmStep = VolumeSteps;
    private static int sfxStep = VolumeSteps;
    private static bool fullscreen = true;

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        Changed = null;
    }

    // 0 ~ VolumeSteps
    public static int BgmStep { get { Load(); return bgmStep; } }
    public static int SfxStep { get { Load(); return sfxStep; } }
    public static float BgmVolume => BgmStep / (float)VolumeSteps;
    public static float SfxVolume => SfxStep / (float)VolumeSteps;
    public static bool Fullscreen { get { Load(); return fullscreen; } }

    public static void SetBgmStep(int step)
    {
        Load();
        bgmStep = Mathf.Clamp(step, 0, VolumeSteps);
        Save();
    }

    public static void SetSfxStep(int step)
    {
        Load();
        sfxStep = Mathf.Clamp(step, 0, VolumeSteps);
        Save();
    }

    public static void SetFullscreen(bool value)
    {
        Load();
        fullscreen = value;
        ApplyScreen();
        Save();
    }

    public static void ApplyAll()
    {
        Load();
        ApplyScreen();
        Changed?.Invoke();
    }

    private static void ApplyScreen()
    {
        if (Application.isEditor)
        {
            return;
        }
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    private static void Load()
    {
        if (loaded)
        {
            return;
        }
        loaded = true;
        bgmStep = PlayerPrefs.GetInt(BgmKey, VolumeSteps);
        sfxStep = PlayerPrefs.GetInt(SfxKey, VolumeSteps);
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
    }

    private static void Save()
    {
        PlayerPrefs.SetInt(BgmKey, bgmStep);
        PlayerPrefs.SetInt(SfxKey, sfxStep);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
