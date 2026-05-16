using UnityEngine;
using UnityEngine.UI;

public class SelectLevelButton : MonoBehaviour
{
    public enum LevelSelectState
    {
        Locked,
        Playable
    }

    private LevelData levelData;

    [Header("Level")]
    [SerializeField] private LevelData defaultLevelData;

    [Header("UI")]
    [SerializeField] private Button _button;
    [SerializeField] private GameObject lockedView;
    [SerializeField] private GameObject playableView;
    [SerializeField] private Transform mousePoint;

    private bool isUnlocked;
    private LevelSelectState currentState;

    public LevelData LevelData => levelData != null ? levelData : defaultLevelData;
    public Transform MousePoint => mousePoint != null ? mousePoint : transform;
    public bool IsUnlocked => isUnlocked;
    public LevelSelectState CurrentState => currentState;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClickLevelButton);
            _button.onClick.AddListener(OnClickLevelButton);
        }
    }

    public void SetUp(LevelData levelData)
    {
        this.levelData = levelData;
        RefreshState(GameManager.instance.maxClearLevelID);
    }

    public void RefreshState(int maxClearLevelID)
    {
        LevelData data = LevelData;
        if (data == null)
        {
            isUnlocked = false;
            currentState = LevelSelectState.Locked;
            SetInteractable(false);
            SetStateViews(false, false);
            return;
        }

        if (maxClearLevelID < data.levelID)
        {
            ApplyState(LevelSelectState.Locked);
        }
        else
        {
            ApplyState(LevelSelectState.Playable);
        }
    }

    public void OnClickLevelButton()
    {
        if (!isUnlocked)
        {
            return;
        }

        GameManager.instance.EnterLevel(LevelData);
    }

    private void ApplyState(LevelSelectState state)
    {
        currentState = state;
        isUnlocked = state != LevelSelectState.Locked;

        SetInteractable(isUnlocked);
        SetStateViews(
            state == LevelSelectState.Locked,
            state == LevelSelectState.Playable);
    }

    private void SetInteractable(bool interactable)
    {
        if (_button != null)
        {
            _button.interactable = interactable;
        }
    }

    private void SetStateViews(bool locked, bool playable)
    {
        if (lockedView != null)
        {
            lockedView.SetActive(locked);
        }

        if (playableView != null)
        {
            playableView.SetActive(playable);
        }

    }
}
