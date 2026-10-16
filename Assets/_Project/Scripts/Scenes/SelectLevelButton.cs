using UnityEngine;
using UnityEngine.UI;

public class SelectLevelButton : MonoBehaviour
{
    public enum LevelSelectState
    {
        Locked,
        Playable
    }

    // 레벨 목록 순서 (0부터). 스테이지 번호는 LevelIndex + 1.
    private int levelIndex = -1;
    private LevelRoot level;

    [Header("UI")]
    [SerializeField] private Button _button;
    [SerializeField] private GameObject lockedView;
    [SerializeField] private GameObject playableView;
    [SerializeField] private Transform mousePoint;

    private bool isUnlocked;
    private LevelSelectState currentState;

    public int LevelIndex => levelIndex;
    public LevelRoot Level => level;
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

    public void SetUp(int index, LevelRoot levelRoot)
    {
        levelIndex = index;
        level = levelRoot;
        name = $"{index + 1}_Button";
        RefreshState(GameManager.instance.UnlockedStage);
    }

    public void RefreshState(int unlockedStage)
    {
        if (level == null)
        {
            isUnlocked = false;
            currentState = LevelSelectState.Locked;
            SetInteractable(false);
            SetStateViews(false, false);
            return;
        }

        if (levelIndex + 1 > unlockedStage)
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

        GameManager.instance.EnterLevel(levelIndex);
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
