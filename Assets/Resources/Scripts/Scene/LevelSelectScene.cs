using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelSelectScene : MonoBehaviour
{
    [Header("Manual Slots")]
    [SerializeField] private SelectLevelButton[] levelSlots;

    [Header("Mouse Cursor")] 
    [SerializeField] private RectTransform mouseCursor;
    [SerializeField] private GameObject mouseIndicator;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float jumpHeight = 45f;
    [SerializeField] private int jumpCount = 3;
    [SerializeField] private Animator mouseAnimator;
    [SerializeField] private string jumpTriggerName = "Jump";

    private int selectedIndex;
    private bool isMoving;
    private float moveTimer;
    private Vector3 moveStartPosition;
    private Vector3 moveEndPosition;

    private void Start()
    {
        RefreshSlots();
        selectedIndex = FindInitialSelectedIndex();
        SnapMouseToSelectedSlot();
        SelectCurrentSlot();
    }

    private void Update()
    {
        UpdateMouseMove();
        HandleInput();
    }

    private void RefreshSlots()
    {
        int maxClearLevelID = GameManager.instance.maxClearLevelID;

        if (levelSlots == null)
        {
            return;
        }

        foreach (SelectLevelButton slot in levelSlots)
        {
            if (slot != null)
            {
                slot.RefreshState(maxClearLevelID);
            }
        }
    }

    private int FindInitialSelectedIndex()
    {
        int bestIndex = -1;
        int bestLevelID = int.MinValue;
        int maxClearLevelID = GameManager.instance.maxClearLevelID;

        if (levelSlots == null)
        {
            return -1;
        }

        for (int i = 0; i < levelSlots.Length; i++)
        {
            LevelData data = GetLevelData(i);
            if (data == null || data.levelID > maxClearLevelID)
            {
                continue;
            }

            if (data.levelID > bestLevelID)
            {
                bestLevelID = data.levelID;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            return bestIndex;
        }

        return FindNextPlayableSlotIndex(0, 1);
    }

    private void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Title");
            return;
        }

        if (isMoving)
        {
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            MoveSelection(-1);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            MoveSelection(1);
        }
    }

    private void MoveSelection(int direction)
    {
        int nextIndex = FindNextPlayableSlotIndex(selectedIndex + direction, direction);
        if (nextIndex < 0 || nextIndex == selectedIndex)
        {
            return;
        }

        selectedIndex = nextIndex;
        bool mouseDir = direction == 1 ? true : false;
        if (mouseIndicator != null)
        {
            mouseIndicator.SetActive(false);
        }
        StartMouseMove(GetMouseTargetPosition(selectedIndex), mouseDir);
        SelectCurrentSlot();
    }

    private int FindNextPlayableSlotIndex(int startIndex, int direction)
    {
        if (levelSlots == null)
        {
            return -1;
        }

        for (int i = startIndex; i >= 0 && i < levelSlots.Length; i += direction)
        {
            SelectLevelButton slot = levelSlots[i];
            if (slot != null && slot.IsUnlocked)
            {
                return i;
            }
        }

        return -1;
    }

    private void EnterSelectedLevel()
    {
        SelectLevelButton slot = GetSelectedSlot();
        if (slot != null && slot.IsUnlocked)
        {
            slot.OnClickLevelButton();
        }
    }

    private void SnapMouseToSelectedSlot()
    {
        if (mouseCursor != null)
        {
            mouseCursor.position = GetMouseTargetPosition(selectedIndex);
        }
    }

    private void StartMouseMove(Vector3 targetPosition, bool mouseDir)
    {
        if (mouseCursor == null)
        {
            return;
        }

        Quaternion mouseDirection = Quaternion.Euler(0,0,0);
        
        if (!mouseDir)
        {
            mouseDirection.y = 180;
        }
        
        mouseCursor.rotation = mouseDirection;
        
        isMoving = true;
        moveTimer = 0f;
        moveStartPosition = mouseCursor.position;
        moveEndPosition = targetPosition;

        if (mouseAnimator != null && !string.IsNullOrEmpty(jumpTriggerName))
        {
            mouseAnimator.SetTrigger(jumpTriggerName);
        }
    }

    private void UpdateMouseMove()
    {
        if (!isMoving || mouseCursor == null)
        {
            return;
        }
        
        moveTimer += Time.deltaTime;
        float duration = Mathf.Max(0.01f, moveDuration);
        float t = Mathf.Clamp01(moveTimer / duration);
        Vector3 position = Vector3.Lerp(moveStartPosition, moveEndPosition, SmoothStep(t));
        position.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI * jumpCount)) * jumpHeight;
        mouseCursor.position = position;

        if (t >= 1f)
        {
            isMoving = false;
            mouseCursor.position = moveEndPosition;
            mouseIndicator.SetActive(true);
        }
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private Vector3 GetMouseTargetPosition(int index)
    {
        SelectLevelButton slot = GetSlot(index);
        return slot != null ? slot.MousePoint.position : Vector3.zero;
    }

    private void SelectCurrentSlot()
    {
        SelectLevelButton slot = GetSelectedSlot();
        if (slot == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(slot.gameObject);
        }
    }

    private SelectLevelButton GetSelectedSlot()
    {
        return GetSlot(selectedIndex);
    }

    private SelectLevelButton GetSlot(int index)
    {
        if (levelSlots == null || index < 0 || index >= levelSlots.Length)
        {
            return null;
        }

        return levelSlots[index];
    }

    private LevelData GetLevelData(int index)
    {
        SelectLevelButton slot = GetSlot(index);
        return slot != null ? slot.LevelData : null;
    }
}
