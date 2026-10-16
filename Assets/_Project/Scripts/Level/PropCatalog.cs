using System.Collections.Generic;
using UnityEngine;

// 레벨 에디터 팔레트에 보여줄 프롭 목록.
// 프롭의 기본값은 프리팹 자체에 있고, 여기에는 팔레트에 보여줄 정보만 둔다.
// 새 프롭을 추가하려면 LevelProp 이 붙은 프리팹을 만들고 여기에 한 줄 추가한다.
[CreateAssetMenu(fileName = "PropCatalog", menuName = "RatLoop/Prop Catalog")]
public class PropCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("레벨 파일이 프롭을 찾을 때 쓰는 고유 ID예요. 한 번 정하면 바꾸지 마세요.")]
        public string id;
        [Tooltip("팔레트에 보이는 이름")]
        public string displayName;
        public GameObject prefab;
        [Tooltip("비워 두면 프리팹의 첫 번째 그림을 써요.")]
        public Sprite icon;
        [TextArea] public string description;
        [Tooltip("켜면 팔레트에 보이지 않아요. 치즈, 시작 위치처럼 레벨에 하나만 있는 것에 써요.")]
        public bool hiddenInPalette;

        public LevelProp Prop => prefab != null ? prefab.GetComponent<LevelProp>() : null;
        public PropCategory Category => Prop != null ? Prop.Category : PropCategory.Physics;

        public Sprite GetIcon()
        {
            if (icon != null)
            {
                return icon;
            }

            SpriteRenderer sr = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            return sr != null ? sr.sprite : null;
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public Entry Find(string id)
    {
        return entries.Find(e => e.id == id);
    }

    public Entry FindByPrefab(GameObject prefab)
    {
        return entries.Find(e => e.prefab == prefab);
    }

    public void Validate(PropValidationReport report)
    {
        var ids = new HashSet<string>();
        foreach (Entry e in entries)
        {
            if (string.IsNullOrEmpty(e.id))
            {
                report.Error($"카탈로그: '{e.displayName}'의 ID가 비어 있어요.", this);
            }
            else if (!ids.Add(e.id))
            {
                report.Error($"카탈로그: ID '{e.id}'가 겹쳐요.", this);
            }

            if (e.prefab == null)
            {
                report.Error($"카탈로그: '{e.displayName}'의 프리팹이 비어 있어요.", this);
            }
            else if (e.Prop == null)
            {
                report.Error($"카탈로그: '{e.displayName}' 프리팹에 LevelProp이 없어요.", e.prefab);
            }
            else if (e.Prop.CatalogId != e.id)
            {
                report.Warning($"카탈로그: '{e.displayName}' 프리팹의 ID가 카탈로그와 달라요.", e.prefab);
            }
        }
    }
}
