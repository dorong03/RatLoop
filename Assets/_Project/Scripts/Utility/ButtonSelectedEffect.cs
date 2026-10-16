using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSelectedEffect : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private Animator _animator;

    [SerializeField] private string selectedStateName = "Selected";
    
    [SerializeField] private AudioClip buttonSelectedAudioClip;
    
    [SerializeField] private float minPitchRange = 0.9f;
    [SerializeField] private float maxPitchRange = 1.1f;

    [SerializeField] private GameObject indicatorObject;

    private bool isFirstSelected = false;

    public void Awake()
    {
        if (GetComponent<Animator>() != null)
        {
            _animator = GetComponent<Animator>();
        }
    }
    
    public void OnEnable()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && gameObject.Equals(eventSystem.firstSelectedGameObject))
        {
            isFirstSelected = true;
        }

        if (indicatorObject != null)
        {
            indicatorObject.SetActive(false);
        }
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        if (isFirstSelected)
        {
            if (_animator != null)
            {
                _animator.Play(selectedStateName, 0, 1f);
                _animator.Update(0);
            }            
            isFirstSelected = false;
        }
        else
        {
            if (buttonSelectedAudioClip != null)
            {
                SoundManager.instance.PlaySFX(buttonSelectedAudioClip, minPitchRange, maxPitchRange);
            }
        }

        if (indicatorObject != null)
        {
            indicatorObject.SetActive(true);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (indicatorObject != null)
        {
            indicatorObject.SetActive(false);
        }
    }
}
