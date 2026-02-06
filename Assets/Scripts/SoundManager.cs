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

    [Header("사운드 클립 등록")]
    [SerializeField] private List<SoundData> sfxClips;
    
    // 빠른 검색을 위한 딕셔너리
    private Dictionary<SfxType, AudioClip> sfxDictionary = new Dictionary<SfxType, AudioClip>();

    [Header("디버그 (인스펙터에서 테스트)")]
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

    private void Update()
    {
        // 인스펙터 디버그용 코드
        if (playTest)
        {
            playTest = false;
            PlaySFX(testSoundType);
            Debug.Log($"[SoundDebug] {testSoundType} 사운드 테스트 재생");
        }
    }

    public void PlaySFX(SfxType type)
    {
        if (sfxDictionary.TryGetValue(type, out AudioClip clip))
        {
            // 걷기 소리는 피치(음높이)를 살짝 랜덤하게 주면 더 자연스러움
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