using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// UI 애니메이션 공통 Lerp 코루틴.
/// 두 컴포넌트(UIPanelCloser, UIIconFx)가 공유.
/// </summary>
public static class UIAnimUtil
{
    public static IEnumerator LerpColor(Image img, Color target, float speed)
    {
        while (img && Vector4.Distance(img.color, target) > 0.001f)
        {
            img.color = Color.Lerp(img.color, target, Time.unscaledDeltaTime * speed);
            yield return null;
        }
        if (img) img.color = target;
    }

    public static IEnumerator LerpScale(RectTransform rt, float target, float speed)
    {
        var tv = Vector3.one * target;
        while (rt && Vector3.Distance(rt.localScale, tv) > 0.001f)
        {
            rt.localScale = Vector3.Lerp(rt.localScale, tv, Time.unscaledDeltaTime * speed);
            yield return null;
        }
        if (rt) rt.localScale = tv;
    }
}
