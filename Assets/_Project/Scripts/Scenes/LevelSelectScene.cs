using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 스테이지 선택 화면. 레벨 목록(LevelList)으로 버튼을 자동 생성한다.
// 버튼은 지그재그로 놓이고, 한 페이지에 다 들어가지 않으면 페이지를 넘긴다.
public class LevelSelectScene : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private SelectLevelButton slotPrefab;
    [SerializeField] private RectTransform slotContainer;
    [Tooltip("첫 번째 버튼 위치 (slotContainer 기준)")]
    [SerializeField] private Vector2 firstSlotPosition = new Vector2(-607f, -153f);
    [Tooltip("버튼 사이 가로 간격")]
    [SerializeField] private float slotSpacing = 167.86f;
    [Tooltip("짝수 번째 버튼이 위로 올라가는 높이")]
    [SerializeField] private float zigzagHeight = 77f;
    [SerializeField, Min(1)] private int slotsPerPage = 8;

    [Header("Mouse Cursor")]
    [SerializeField] private RectTransform mouseCursor;
    [SerializeField] private GameObject mouseIndicator;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float jumpHeight = 45f;
    [SerializeField] private int jumpCount = 3;
    [SerializeField] private Animator mouseAnimator;
    [SerializeField] private string jumpTriggerName = "Jump";

    private readonly List<SelectLevelButton> levelSlots = new List<SelectLevelButton>();
    private int selectedIndex;
    private int currentPage = -1;
    private bool isMoving;
    private float moveTimer;
    private Vector3 moveStartPosition;
    private Vector3 moveEndPosition;

    private void Start()
    {
        BuildSlots();
        selectedIndex = FindInitialSelectedIndex();
        ShowPage(PageOf(selectedIndex));
        SnapMouseToSelectedSlot();
        SelectCurrentSlot();
    }

    private void Update()
    {
        UpdateMouseMove();
        HandleInput();
    }

    private void BuildSlots()
    {
        LevelList levels = GameManager.instance.Levels;
        int unlockedStage = GameManager.instance.UnlockedStage;

        for (int i = 0; i < levels.Count; i++)
        {
            SelectLevelButton slot = Instantiate(slotPrefab, slotContainer);
            int column = i % slotsPerPage;
            var rect = (RectTransform)slot.transform;
            rect.anchoredPosition = firstSlotPosition + new Vector2(slotSpacing * column, column % 2 == 1 ? zigzagHeight : 0f);
            slot.SetUp(i, levels.Get(i));
            slot.RefreshState(unlockedStage);
            levelSlots.Add(slot);
        }
    }

    private int PageOf(int index) => Mathf.Max(0, index) / slotsPerPage;

    private void ShowPage(int page)
    {
        if (page == currentPage)
        {
            return;
        }

        currentPage = page;
        for (int i = 0; i < levelSlots.Count; i++)
        {
            levelSlots[i].gameObject.SetActive(PageOf(i) == page);
        }
        // 비활성 상태였던 버튼의 위치가 바로 반영되도록 갱신한다.
        Canvas.ForceUpdateCanvases();
    }

    private int FindInitialSelectedIndex()
    {
        int lastEntered = GameManager.instance.LastEnteredIndex;
        if (lastEntered >= 0 && lastEntered < levelSlots.Count && levelSlots[lastEntered].IsUnlocked)
        {
            return lastEntered;
        }

        // 마지막으로 연 스테이지
        for (int i = levelSlots.Count - 1; i >= 0; i--)
        {
            if (levelSlots[i].IsUnlocked)
            {
                return i;
            }
        }
        return 0;
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
            SoundManager.instance.PlaySFX(SfxType.StageError);
            return;
        }

        bool pageChanged = PageOf(nextIndex) != currentPage;
        selectedIndex = nextIndex;

        if (pageChanged)
        {
            // 페이지가 바뀌면 이동 연출 없이 새 페이지의 버튼으로 바로 옮긴다.
            ShowPage(PageOf(selectedIndex));
            SetMouseDirection(direction == 1);
            SnapMouseToSelectedSlot();
            if (mouseIndicator != null)
            {
                mouseIndicator.SetActive(true);
            }
        }
        else
        {
            if (mouseIndicator != null)
            {
                mouseIndicator.SetActive(false);
            }
            StartMouseMove(GetMouseTargetPosition(selectedIndex), direction == 1);
        }
        SelectCurrentSlot();
    }

    private int FindNextPlayableSlotIndex(int startIndex, int direction)
    {
        for (int i = startIndex; i >= 0 && i < levelSlots.Count; i += direction)
        {
            if (levelSlots[i] != null && levelSlots[i].IsUnlocked)
            {
                return i;
            }
        }

        return -1;
    }

    private void SnapMouseToSelectedSlot()
    {
        if (mouseCursor != null)
        {
            mouseCursor.position = GetMouseTargetPosition(selectedIndex);
        }
    }

    private void SetMouseDirection(bool right)
    {
        if (mouseCursor != null)
        {
            mouseCursor.rotation = Quaternion.Euler(0f, right ? 0f : 180f, 0f);
        }
    }

    private void StartMouseMove(Vector3 targetPosition, bool mouseDir)
    {
        if (mouseCursor == null)
        {
            return;
        }

        SetMouseDirection(mouseDir);

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
            if (mouseIndicator != null)
            {
                mouseIndicator.SetActive(true);
            }
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
        SelectLevelButton slot = GetSlot(selectedIndex);
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

    private SelectLevelButton GetSlot(int index)
    {
        return index >= 0 && index < levelSlots.Count ? levelSlots[index] : null;
    }
}
