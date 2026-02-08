using UnityEngine;
using System.Collections.Generic;

public enum SfxType
{
    Walk,
    Jump,
    Land,
    Cheese,
    Die,
    Replay
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("오디오 소스")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource; 

    [Header("BGM 설정")]
    [SerializeField] private AudioClip lobbyBgmClip;
    [SerializeField] private AudioClip stageBgmClip;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;

    [Header("사운드 클립 등록")]
    [SerializeField] private List<SoundData> sfxClips;
    
    private Dictionary<SfxType, AudioClip> sfxDictionary = new Dictionary<SfxType, AudioClip>();

    [Header("디버그")]
    [SerializeField] private SfxType testSoundType;
    [SerializeField] private bool playTest; 

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        foreach (var data in sfxClips)
        {
            if (!sfxDictionary.ContainsKey(data.type))
            {
                sfxDictionary.Add(data.type, data.clip);
            }
        }
    }

    private void Start()
    {
        PlayLobbyBGM();
    }

    private void Update()
    {
        if (playTest)
        {
            playTest = false;
            PlaySFX(testSoundType);
            Debug.Log($"[SoundDebug] {testSoundType} 사운드 테스트 재생");
        }
    }

    public void PlayLobbyBGM()
    {
        ChangeBGM(lobbyBgmClip);
    }

    public void PlayStageBGM()
    {
        ChangeBGM(stageBgmClip);
    }

    private void ChangeBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;
        
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void PlaySFX(SfxType type)
    {
        if (sfxDictionary.TryGetValue(type, out AudioClip clip))
        {
            if (type == SfxType.Walk)
            {
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
                sfxSource.volume = 0.2f;
            }
            else
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = 1f;
            }
            
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"사운드 누락됨: {type}");
        }
    }
}

[System.Serializable]
public class SoundData
{
    public SfxType type;
    public AudioClip clip;
}