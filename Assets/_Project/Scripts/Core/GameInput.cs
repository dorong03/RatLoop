using UnityEngine;
using UnityEngine.InputSystem;

// 메뉴/시스템 입력을 한 곳에서 읽는다. Keyboard.current 를 직접 읽지 말고 이걸 쓴다.
// 프로젝트 기본 Input Actions(InputSystem_Actions) 의 UI / System / Player 맵을 사용하므로
// 키보드와 게임패드가 함께 동작하고, 키를 바꿀 때는 .inputactions 파일만 고치면 된다.
public static class GameInput
{
    private const float NavigateThreshold = 0.5f;

    private static InputAction navigate;
    private static InputAction submit;
    private static InputAction cancel;
    private static InputAction pause;
    private static InputAction settings;
    private static InputAction retry;

    private static int lastNavigateFrame = -1;
    private static Vector2 previousNavigate;
    private static Vector2 currentNavigate;

    // 도메인 리로드를 끈 상태에서도 플레이할 때마다 다시 찾도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        navigate = submit = cancel = pause = settings = retry = null;
        lastNavigateFrame = -1;
        previousNavigate = currentNavigate = Vector2.zero;
    }

    private static InputAction Find(ref InputAction cache, string path)
    {
        if (cache == null && InputSystem.actions != null)
        {
            cache = InputSystem.actions.FindAction(path);
            if (cache != null && !cache.enabled)
            {
                cache.Enable();
            }
        }
        return cache;
    }

    private static bool Pressed(ref InputAction cache, string path)
    {
        InputAction action = Find(ref cache, path);
        return action != null && action.WasPressedThisFrame();
    }

    public static bool SubmitPressed => Pressed(ref submit, "UI/Submit");
    public static bool CancelPressed => Pressed(ref cancel, "UI/Cancel");
    public static bool PausePressed => Pressed(ref pause, "System/Pause");
    public static bool SettingsPressed => Pressed(ref settings, "System/Settings");
    public static bool RetryPressed => Pressed(ref retry, "Player/Retry");

    // 이번 프레임에 새로 눌린 방향. 대각선이면 더 크게 기운 축 하나만 돌려준다.
    public static Vector2Int NavigatePressed
    {
        get
        {
            UpdateNavigate();
            int x = Edge(previousNavigate.x, currentNavigate.x);
            int y = Edge(previousNavigate.y, currentNavigate.y);
            if (x != 0 && y != 0)
            {
                if (Mathf.Abs(currentNavigate.x) >= Mathf.Abs(currentNavigate.y)) y = 0;
                else x = 0;
            }
            return new Vector2Int(x, y);
        }
    }

    private static void UpdateNavigate()
    {
        if (lastNavigateFrame == Time.frameCount)
        {
            return;
        }
        lastNavigateFrame = Time.frameCount;
        previousNavigate = currentNavigate;
        InputAction action = Find(ref navigate, "UI/Navigate");
        currentNavigate = action != null ? action.ReadValue<Vector2>() : Vector2.zero;
    }

    private static int Edge(float previous, float current)
    {
        if (current > NavigateThreshold && previous <= NavigateThreshold) return 1;
        if (current < -NavigateThreshold && previous >= -NavigateThreshold) return -1;
        return 0;
    }
}
