using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    [SerializeField] private Text livesText;
    [SerializeField] private Text timeText;
    
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject finalClearPanel;
    [SerializeField] private Button ResumeButton;
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ReTryButton;
    [SerializeField] private Button InfoButton;

    private Stack<GameObject> panelStack = new();
    private Stack<GameObject> previousSelectedStack = new();
    
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
    }
    
    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        if (finalClearPanel != null)
        {
            finalClearPanel.SetActive(false);
        }

        GameManager.instance.OnReplayCountChanged += UpdateLivesText;
        GameManager.instance.OnTimeChanged += UpdateTimeText;
        GameManager.instance.OnEnterLevel += ShowLevelUI;
        GameManager.instance.OnExitLevel += HideLevelUI;
        GameManager.instance.OnPressPause += OnPressPause;
        GameManager.instance.OnFinalLevelClear += ShowFinalClearPanel;
        
        ResumeButton.onClick.AddListener(GameManager.instance.TogglePause);
        ExitButton.onClick.AddListener(GameManager.instance.ExitLevel);
        ReTryButton.onClick.AddListener(GameManager.instance.Retry);
        InfoButton.onClick.AddListener(ActiveInfoPanel);
    }

    private void OnDestroy()
    {
        GameManager.instance.OnReplayCountChanged -= UpdateLivesText;
        GameManager.instance.OnTimeChanged -= UpdateTimeText;
        GameManager.instance.OnEnterLevel -= ShowLevelUI;
        GameManager.instance.OnExitLevel -= HideLevelUI;
        GameManager.instance.OnPressPause -= OnPressPause;
        GameManager.instance.OnFinalLevelClear -= ShowFinalClearPanel;
    }

    private void UpdateLivesText(int lives)
    {
        livesText.text = "X "+ lives;
    }

    private void UpdateTimeText(int time)
    {
        timeText.text = time.ToString();
    }

    // 패널이 열려있는 동안에는 항상 최상단 패널 안의 버튼이 선택된 상태를 유지한다. (키보드 전용 조작)
    private void Update()
    {
        if (panelStack.Count == 0)
        {
            return;
        }

        GameObject topPanel = panelStack.Peek();

        // 엔딩 패널은 버튼 없이 엔터/ESC로만 닫고 스테이지 선택 화면으로 돌아간다.
        // TODO : 학교 전시용 빌드라 임시로 Update에서 Keyboard를 직접 읽는다.
        //        전시 후 InputAction 기반으로 옮길 것.
        if (topPanel == finalClearPanel && IsConfirmOrCancelPressed())
        {
            GameManager.instance.ExitLevel();
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy || !selected.transform.IsChildOf(topPanel.transform))
        {
            SelectFirstSelectable(topPanel);
        }
    }

    private void OnPressPause(bool pause)
    {
        if (pause)
        {
            Time.timeScale = 0f;
            GameManager.instance.gameState = GameState.Pause;
            OpenPanel(pausePanel);
        }
        else
        {
            GameObject activePanel = ClosePanel();
            if (activePanel != null && activePanel.Equals(pausePanel))
            {
                GameManager.instance.gameState = GameState.Playing;
                Time.timeScale = 1f;
            }
        }
    }

    private void ActiveInfoPanel()
    {
        if (infoPanel != null)
        {
            OpenPanel(infoPanel);
        }
    }

    private void ShowFinalClearPanel()
    {
        if (finalClearPanel != null)
        {
            OpenPanel(finalClearPanel);
        }
        else
        {
            // 패널이 연결되지 않았더라도 게임이 멈추지 않도록 바로 나간다.
            GameManager.instance.ExitLevel();
        }
    }

    private bool IsConfirmOrCancelPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.enterKey.wasPressedThisFrame ||
               keyboard.numpadEnterKey.wasPressedThisFrame ||
               keyboard.escapeKey.wasPressedThisFrame;
    }

    private void OpenPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        previousSelectedStack.Push(eventSystem != null ? eventSystem.currentSelectedGameObject : null);
        if (panelStack.TryPeek(out GameObject previousPanel))
        {
            previousPanel.SetActive(false);
        }
        panel.SetActive(true);
        panelStack.Push(panel);

        SelectFirstSelectable(panel);
    }

    private GameObject ClosePanel()
    {
        if (panelStack.Count == 0)
        {
            return null;
        }

        GameObject activePanel = panelStack.Pop();
        activePanel.SetActive(false);

        GameObject previousSelected = previousSelectedStack.Count > 0 ? previousSelectedStack.Pop() : null;

        if (panelStack.Count > 0)
        {
            GameObject panel = panelStack.Peek();
            panel.SetActive(true);
            // 이전 패널로 돌아갈 때는 여기로 들어오기 전에 선택했던 버튼을 복원한다.
            if (previousSelected != null && previousSelected.activeInHierarchy)
            {
                SetSelected(previousSelected);
            }
            else
            {
                SelectFirstSelectable(panelStack.Peek());
            }
        }
        else
        {
            SetSelected(null);
        }

        return activePanel;
    }

    private void SelectFirstSelectable(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        if (panel.Equals(pausePanel) && ResumeButton != null && ResumeButton.IsActive() && ResumeButton.IsInteractable())
        {
            SetSelected(ResumeButton.gameObject);
            return;
        }

        foreach (Selectable selectable in panel.GetComponentsInChildren<Selectable>(false))
        {
            if (selectable.IsInteractable())
            {
                SetSelected(selectable.gameObject);
                return;
            }
        }
    }

    private void SetSelected(GameObject target)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        // 같은 오브젝트를 다시 지정하면 무시되므로 한 번 비운 뒤 지정해 OnSelect가 확실히 호출되도록 한다.
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(target);
    }

    private void ShowLevelUI()
    {
        timeText.gameObject.SetActive(true);
        livesText.gameObject.SetActive(true);
        CloseAllPanels();
    }

    private void HideLevelUI()
    {
        timeText.gameObject.SetActive(false);
        livesText.gameObject.SetActive(false);
        CloseAllPanels();
    }

    private void CloseAllPanels()
    {
        while (panelStack.Count > 0)
        {
            panelStack.Pop().SetActive(false);
        }

        previousSelectedStack.Clear();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        if (finalClearPanel != null)
        {
            finalClearPanel.SetActive(false);
        }

        SetSelected(null);
    }
}
