# 새 타일, 프롭, 기믹 추가하기

| 추가할 것 | 누가 | 코드 필요 | 방법 |
|---|---|---|---|
| 새 타일 이미지 | 기획자 | 없음 | 레벨 에디터 > 타일 > `PNG 추가...` |
| 새 프롭 (박스, 장식 같은 물건) | 기획자 | 없음 | 레벨 에디터 > 프롭 > `+ 새 프롭 만들기` |
| 새 기믹 (특수한 동작) | 개발자 | 있음 | `LevelProp` 을 상속한 스크립트 + 프리팹 + 카탈로그 등록 |

---

## 1. 새 타일 이미지 (기획자)

### 이미지 준비

- 배경이 투명한 PNG 한 장에 타일을 **같은 크기의 칸에 맞춰** 그립니다. (지금 게임의 타일은 16 x 16 픽셀)
- 칸 사이에 여백을 두지 않습니다. 비어 있는(완전히 투명한) 칸은 자동으로 건너뜁니다.
- 여러 칸으로 이루어진 그림(기둥, 큰 바위 등)은 이미지 안에서 붙어 있는 그대로 두면 됩니다.
  팔레트가 이미지 배치를 그대로 보여주므로, 끌어서 영역을 고르면 모양 그대로 찍힙니다.

### 추가

1. 레벨 에디터에서 레벨을 열고 **타일** 모드로 갑니다.
2. 아래쪽 `새 타일 이미지` 에서 칸 크기(기본 16)를 확인하고 `PNG 추가...` 를 누릅니다.
3. PNG 를 고르면 자동으로 아래 일이 처리되고, 팔레트 위쪽 목록에 이미지 이름으로 새 묶음이 생깁니다.
   - `Assets/_Project/Art/Tiles/<이미지 이름>/` 으로 복사
   - 픽셀 아트 설정 (선명하게, 압축 없음)
   - 칸 크기대로 자르기
   - 칸마다 타일 에셋 만들기

### 알아둘 점

- 타일의 부딪히는 모양은 **그림의 불투명한 부분**을 따라갑니다. '지형'에 칠하면 그 모양대로 밟고 부딪힙니다.
- 같은 이미지를 다시 추가하면 새 묶음이 하나 더 생깁니다. 그림을 고친 경우에는 원래 PNG 파일을 덮어쓰세요. (칸 위치가 같으면 이미 칠한 타일도 바뀐 그림으로 보입니다)
- 이미 프로젝트 안에 있는 이미지는 Project 창에서 고른 뒤 메뉴 **RatLoop > 타일 > 선택한 이미지로 타일 만들기** 로도 추가할 수 있습니다.

---

## 2. 새 프롭 (기획자, 코드 없이)

1. 레벨 에디터 **프롭** 모드에서 `+ 새 프롭 만들기` 를 누릅니다.
2. 이미지를 고릅니다. 프로젝트의 스프라이트를 끌어다 놓거나, `컴퓨터에서 PNG 고르기...` 로 가져옵니다.
3. 이름과 종류를 고릅니다.

   | 종류 | 동작 |
   |---|---|
   | 물리 프롭 | 밀고 쌓을 수 있습니다. 무게를 정합니다. |
   | 고정 프롭 | 움직이지 않는 발판입니다. |
   | 장식 | 부딪히지 않고 통과합니다. |

4. 물리/고정 프롭은 부딪히는 모양(사각형/원)을 고릅니다. 크기는 이미지 크기에 맞춰 자동으로 정해집니다.
5. `만들기` 를 누르면 팔레트에 추가되고 바로 놓을 수 있는 상태가 됩니다.

만든 뒤에는 '선택·이동' 모드에서 크기, 마찰, 튕김, 물에 뜨는 정도, 회전 고정 같은 값을 바꿀 수 있습니다.
팔레트에 보이는 이름, 설명, 순서는 `Assets/_Project/Data/PropCatalog` 에셋에서 바꿉니다.

---

## 3. 새 기믹 (개발자)

밟으면 튕기는 점프대, 스위치로 열리는 문처럼 **특수한 동작**이 필요한 오브젝트는 코드로 만듭니다.
한 번 만들어 카탈로그에 올리면, 그 뒤로는 기획자가 팔레트에서 골라 놓고 값만 바꿉니다.

### 순서

1. `Assets/_Project/Scripts/Props/` 에 `LevelProp` 을 상속한 스크립트를 만듭니다.
   - 물리 프롭에 기능을 더하는 경우 `PhysicsProp`, 고정 발판이면 `StaticProp` 을 상속해도 됩니다.
2. 기획자가 바꿀 값에 `[PlannerField("표시 이름", 순서)]` 를 붙입니다. `[Range]`, `[Tooltip]` 과 같이 쓸 수 있습니다.
   레벨 에디터와 인스펙터에 자동으로 나타납니다.
   - 기본은 **종류 공통 값**입니다. 프리팹에 저장되어 같은 프롭은 모든 레벨에서 같은 값을 씁니다.
   - 크기, 길이처럼 **놓은 자리마다 달라야 하는 값**만 `[PlannerField("가로 칸 수", 0, PerInstance = true)]` 로 표시합니다.
   - 동작이 크게 다른 설정(예: 투명벽의 "물체도 막기")은 값 하나로 두기보다 **프리팹 변형(Variant)을 따로 만들어** 다른 종류로 카탈로그에 올리는 것을 권장합니다.
3. 아래 세 가지를 정합니다. 레이어와 그리는 순서는 이 값으로 자동 지정되므로 직접 설정하지 않습니다.
   - `Category`: 팔레트 분류
   - `SortingLayerName`: 그리는 순서
   - `PhysicsLayerName`: 물리 레이어
4. 필요하면 `Validate` 를 구현해 세이프티 체크를 추가합니다. 결과는 레벨 에디터의 '검사' 화면에 나옵니다.
5. 프리팹을 만듭니다. 루트에 스크립트를 붙이고, 그림(SpriteRenderer)과 콜라이더를 자식이나 루트에 둡니다.
   `Assets/_Project/Prefabs/Props/` 에 저장합니다.
6. Project 창에서 프리팹을 우클릭하고 **RatLoop > 프롭 카탈로그에 추가** 를 누르면 팔레트에 나타납니다.

### 예시: 점프대

```csharp
using UnityEngine;

// 예시 기믹: 위에서 밟으면 위로 튕겨 올리는 점프대
[RequireComponent(typeof(BoxCollider2D))]
public class SpringProp : LevelProp
{
    // 기획자가 레벨 에디터에서 바꿀 값
    [PlannerField("튀어오르는 힘", 0)]
    [SerializeField, Range(1f, 30f)] private float launchSpeed = 14f;

    [PlannerField("물체도 튕기기", 1)]
    [SerializeField] private bool launchProps = true;

    // 팔레트 분류, 그리는 순서, 물리 레이어 (기획자가 고르지 않는다)
    public override PropCategory Category => PropCategory.Gimmick;
    protected override string SortingLayerName => SortingLayers.Props;
    protected override string PhysicsLayerName => PhysicsLayers.Ground;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.rigidbody;
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        bool isCharacter = collision.collider.CompareTag("Player") || collision.collider.CompareTag("Ghost");
        if (!isCharacter && !launchProps)
        {
            return;
        }

        // 위에서 밟았을 때만 튕긴다.
        if (rb.position.y < transform.position.y)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, launchSpeed);
    }

    // 세이프티 체크: 레벨 에디터의 '검사' 화면에 나온다.
    public override void Validate(PropValidationReport report)
    {
        if (GetComponentInChildren<Collider2D>() == null)
        {
            report.Error($"{name}: 콜라이더가 없어 밟을 수 없습니다.", this);
        }
    }
}
```

이 예시는 실제로 프리팹 → 카탈로그 등록 → 레벨 배치 → 플레이까지 확인했습니다. (쥐가 점프대 위에서 튀어 오름)

### 주의할 점

- **리플레이**: 고스트는 녹화된 입력만 다시 재생합니다. 기믹 상태(열린 문, 사라진 블록)는 리플레이해도 되돌아가지 않으므로, 이를 전제로 퍼즐을 설계합니다.
- **`Awake` / `OnValidate`**: `LevelProp` 이 이미 쓰고 있으므로 직접 쓸 때는 `protected override` 로 만들고 `base.Awake()` / `base.OnValidate()` 를 먼저 부릅니다.
- **에디터에서만 보여야 하는 것**: 예를 들어 투명벽 표시는 `OnDrawGizmos` 로 그립니다. 게임 화면에는 나오지 않습니다.
- **칸 단위 영역 기믹**: 물이나 투명벽처럼 칸 수로 크기를 정하는 기믹은 `IAreaProp` 을 구현하고 `width`, `height` 필드를 둡니다.
  그러면 팔레트에서 칸에 맞춰 놓이고, 선택했을 때 레벨 에디터가 크기 손잡이(`AreaHandles`)를 자동으로 그립니다.
- **새 물리 레이어**: 기존 레이어로 충돌 조합이 안 맞을 때만 추가합니다. 추가했다면 `PhysicsLayers` 상수와 충돌 매트릭스를 함께 고칩니다.
