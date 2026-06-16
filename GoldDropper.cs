using UnityEngine;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// 몬스터에 붙이는 골드 드랍 컴포넌트.
/// 몬스터 사망 시 Drop() 또는 DropAtPosition() 호출.
/// (구버전 GoldDropper)
/// </summary>
public class GoldDropper : MonoBehaviour
{
    [Header("골드 드랍 프리팹 (GoldDrop 컴포넌트 포함)")]
    [SerializeField] private GameObject goldDropPrefab;

    [Header("드랍 골드 범위")]
    [SerializeField] private int minGold = 5;
    [SerializeField] private int maxGold = 20;

    [Header("드랍 확률 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 1f;

    [Header("드랍 분산 (골드가 여러 개로 나뉘어 떨어짐)")]
    [SerializeField] private bool scatterDrop = false;
    [SerializeField] private int scatterCount = 3;
    [SerializeField] private float scatterRadius = 0.5f;

    public void Drop() => DropAtPosition(transform.position);

    public void DropAtPosition(Vector3 position)
    {
        if (Random.value > dropChance) return;

        int total = Random.Range(minGold, maxGold + 1);
        if (total <= 0) return;

        if (scatterDrop && scatterCount > 1) SpawnScattered(position, total);
        else                                  SpawnPickup(position, total);
    }

    void SpawnPickup(Vector3 position, int amount)
    {
        if (goldDropPrefab == null)
        {
            Debug.LogWarning($"[GoldDropper] goldDropPrefab이 없어 즉시 지급합니다. ({amount}G)");
            GoldSystem.Instance?.AddGold(amount, GoldChangeReason.MonsterDrop);
            return;
        }

        GameObject obj = Instantiate(goldDropPrefab, position, Quaternion.identity);
        if (obj.TryGetComponent(out GoldDrop pickup))
            pickup.SetAmount(amount);
    }

    void SpawnScattered(Vector3 center, int total)
    {
        int remaining = total;
        for (int i = 0; i < scatterCount; i++)
        {
            int portion = (i == scatterCount - 1)
                ? remaining
                : Random.Range(1, remaining - (scatterCount - i - 1) + 1);

            remaining -= portion;

            Vector2 rand2D = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = center + new Vector3(rand2D.x, 0f, rand2D.y);
            SpawnPickup(spawnPos, portion);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!scatterDrop) return;
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, scatterRadius);
    }
#endif
}
