using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    // 남은 리플레이 횟수 표시
    [FormerlySerializedAs("livesText")]
    [SerializeField] private Text replayCountText;
    [SerializeField] private Text timeText;
    
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject finalClearPanel;
    [Tooltip("스테이지를 깼을 때 나오는 화면. 문구, 글꼴, 위치는 UIManager 프리팹의 ClearPanel 에서 바로 고친다.")]
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private Button ResumeButton;
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ReTryButton;
    [SerializeField] private Button InfoButton;

    private Stack<GameObject> panelStack = new();
    private Stack<GameObject> previousSelectedStack = new();

    // 기존 FinalClearPanel 을 복제해서 만드는 패널들
    private SettingsPanel settingsPanel;
    private GameObject settingsHint;

    // 메뉴 패널이 열려 있으면 타이틀/스테이지 선택 화면은 입력을 받지 않는다.
    public bool IsMenuOpen => panelStack.Count > 0;
    
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
            BuildExtraPanels();
        }

        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }

        GameManager.instance.OnReplayCountChanged += UpdateLivesText;
        GameManager.instance.OnTimeChanged += UpdateTimeText;
        GameManager.instance.OnEnterLevel += ShowLevelUI;
        GameManager.instance.OnExitLevel += HideLevelUI;
        GameManager.instance.OnPressPause += OnPressPause;
        GameManager.instance.OnFinalLevelClear += ShowFinalClearPanel;
        GameManager.instance.OnLevelClear += ShowClearPanel;
        
        ResumeButton.onClick.AddListener(GameManager.instance.TogglePause);
        ExitButton.onClick.AddListener(GameManager.instance.ExitLevel);
        ReTryButton.onClick.AddListener(GameManager.instance.Retry);
        InfoButton.onClick.AddListener(ActiveInfoPanel);
    }

    private void OnDestroy()
    {
        if (instance != this || GameManager.instance == null)
        {
            return;
        }

        GameManager.instance.OnLevelClear -= ShowClearPanel;
        GameManager.instance.OnReplayCountChanged -= UpdateLivesText;
        GameManager.instance.OnTimeChanged -= UpdateTimeText;
        GameManager.instance.OnEnterLevel -= ShowLevelUI;
        GameManager.instance.OnExitLevel -= HideLevelUI;
        GameManager.instance.OnPressPause -= OnPressPause;
        GameManager.instance.OnFinalLevelClear -= ShowFinalClearPanel;
    }

    private void UpdateLivesText(int lives)
    {
        replayCountText.text = "X "+ lives;
    }

    private void UpdateTimeText(int time)
    {
        timeText.text = time.ToString();
    }

    // 패널이 열려있는 동안에는 항상 최상단 패널 안의 버튼이 선택된 상태를 유지한다.
    private void Update()
    {
        if (panelStack.Count == 0)
        {
            // 타이틀/스테이지 선택 화면에서 설정 키로 설정을 연다.
            if (GameManager.instance.gameState == GameState.Lobby && GameInput.SettingsPressed)
            {
                OpenSettings();
            }
            return;
        }

        GameObject topPanel = panelStack.Peek();

        // 엔딩 패널은 버튼 없이 확인/취소로만 닫고 스테이지 선택 화면으로 돌아간다.
        if (topPanel == finalClearPanel && (GameInput.SubmitPressed || GameInput.CancelPressed))
        {
            GameManager.instance.ExitLevel();
            return;
        }

        // 클리어 패널: 확인 = 다음 스테이지, 재시작 키 = 다시 하기, 취소 = 스테이지 선택
        if (topPanel == clearPanel)
        {
            if (GameInput.SubmitPressed) GameManager.instance.EnterNextLevel();
            else if (GameInput.RetryPressed) GameManager.instance.Retry();
            else if (GameInput.CancelPressed) GameManager.instance.ExitLevel();
            return;
        }

        if (settingsPanel != null && topPanel == settingsPanel.gameObject)
        {
            // 일시정지 중에는 취소 키를 GameManager 가 처리(패널 하나 닫기)하므로 여기서는 닫지 않는다.
            bool closeOnCancel = GameManager.instance.gameState != GameState.Pause;
            if (settingsPanel.HandleInput(closeOnCancel))
            {
                ClosePanel();
            }
            return;
        }

        if (topPanel == pausePanel && GameInput.SettingsPressed)
        {
            OpenSettings();
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

    private void ShowClearPanel()
    {
        if (clearPanel != null)
        {
            OpenPanel(clearPanel);
        }
        else
        {
            GameManager.instance.ExitLevel();
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            OpenPanel(settingsPanel.gameObject);
        }
    }

    // 설정 패널, 설정 안내 문구를 기존 엔딩 패널을 복제해서 만든다. (같은 배경과 폰트)
    // 클리어 패널은 프리팹에 직접 들어 있다. (clearPanel)
    private void BuildExtraPanels()
    {
        Transform parent = finalClearPanel.transform.parent;

        settingsPanel = SettingsPanel.Create(finalClearPanel, parent);

        // 일시정지 화면과 타이틀/스테이지 선택 화면에 보이는 "설정" 안내
        Text hintTemplate = finalClearPanel.GetComponentsInChildren<Text>(true)[1];
        settingsHint = CreateHint(hintTemplate, parent, "설정: Tab");
        if (pausePanel != null)
        {
            CreateHint(hintTemplate, pausePanel.transform, "설정: Tab");
        }
    }

    private static GameObject CreateHint(Text template, Transform parent, string message)
    {
        Text hint = Instantiate(template, parent);
        hint.name = "SettingsHint";
        hint.text = message;
        hint.fontSize = Mathf.RoundToInt(template.fontSize * 0.7f);
        hint.horizontalOverflow = HorizontalWrapMode.Overflow;
        hint.alignment = TextAnchor.LowerRight;
        RectTransform rt = hint.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 30f);
        hint.gameObject.SetActive(true);
        return hint.gameObject;
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
        replayCountText.gameObject.SetActive(true);
        if (settingsHint != null) settingsHint.SetActive(false);
        CloseAllPanels();
    }

    private void HideLevelUI()
    {
        timeText.gameObject.SetActive(false);
        replayCountText.gameObject.SetActive(false);
        if (settingsHint != null) settingsHint.SetActive(true);
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

        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.gameObject.SetActive(false);
        }

        SetSelected(null);
    }
}
