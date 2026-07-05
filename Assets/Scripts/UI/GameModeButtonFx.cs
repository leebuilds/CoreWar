using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bullet-hole burst and looping smoke on a game mode button while matchmaking runs.
/// </summary>
public class GameModeButtonFx : MonoBehaviour
{
    static readonly Color HoleColor = new Color(0.015f, 0.015f, 0.015f, 1f);
    static readonly Color SmokeColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);

    readonly List<RectTransform> _holeAnchors = new List<RectTransform>();
    RectTransform _fxRoot;
    bool _active;
    Coroutine _smokeRoutine;

    public static GameModeButtonFx Attach(Button button)
    {
        if (button == null)
        {
            return null;
        }

        var fx = button.gameObject.GetComponent<GameModeButtonFx>();
        if (fx == null)
        {
            fx = button.gameObject.AddComponent<GameModeButtonFx>();
        }

        fx.Initialize(button);
        return fx;
    }

    void Initialize(Button button)
    {
        if (_fxRoot != null)
        {
            return;
        }

        _fxRoot = new GameObject("Mode Button Fx").AddComponent<RectTransform>();
        _fxRoot.SetParent(button.transform, false);
        MenuUiFactory.StretchFull(_fxRoot);
        _fxRoot.SetAsLastSibling();
    }

    public void PlayBurst()
    {
        if (_fxRoot == null)
        {
            return;
        }

        StopFx();
        _active = true;

        int holeCount = Random.Range(3, 6);
        for (int i = 0; i < holeCount; i++)
        {
            CreateHole(RandomAnchor());
        }

        _smokeRoutine = StartCoroutine(SmokeLoop());
    }

    public void StopFx()
    {
        _active = false;
        if (_smokeRoutine != null)
        {
            StopCoroutine(_smokeRoutine);
            _smokeRoutine = null;
        }

        _holeAnchors.Clear();
        if (_fxRoot != null)
        {
            for (int i = _fxRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_fxRoot.GetChild(i).gameObject);
            }
        }
    }

    Vector2 RandomAnchor()
    {
        return new Vector2(Random.Range(0.12f, 0.88f), Random.Range(0.18f, 0.82f));
    }

    void CreateHole(Vector2 anchor)
    {
        var holeGo = new GameObject("Bullet Hole");
        holeGo.transform.SetParent(_fxRoot, false);
        var rect = holeGo.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(Random.Range(8f, 14f), Random.Range(8f, 14f));
        rect.anchoredPosition = Vector2.zero;

        var image = holeGo.AddComponent<Image>();
        image.color = HoleColor;
        image.raycastTarget = false;

        _holeAnchors.Add(rect);
    }

    IEnumerator SmokeLoop()
    {
        while (_active)
        {
            for (int i = 0; i < _holeAnchors.Count; i++)
            {
                if (_holeAnchors[i] != null)
                {
                    StartCoroutine(AnimateSmokePuff(_holeAnchors[i]));
                }
            }

            yield return new WaitForSecondsRealtime(Random.Range(0.18f, 0.32f));
        }
    }

    IEnumerator AnimateSmokePuff(RectTransform anchor)
    {
        if (anchor == null)
        {
            yield break;
        }

        var puffGo = new GameObject("Smoke Puff");
        puffGo.transform.SetParent(_fxRoot, false);
        var rect = puffGo.AddComponent<RectTransform>();
        rect.anchorMin = anchor.anchorMin;
        rect.anchorMax = anchor.anchorMax;
        rect.pivot = anchor.pivot;
        rect.sizeDelta = new Vector2(16f, 16f);
        rect.anchoredPosition = Vector2.zero;

        var image = puffGo.AddComponent<Image>();
        image.color = SmokeColor;
        image.raycastTarget = false;

        float duration = Random.Range(0.55f, 0.9f);
        float elapsed = 0f;
        Vector2 drift = new Vector2(Random.Range(-6f, 6f), Random.Range(8f, 18f));

        while (elapsed < duration && _active)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rect.anchoredPosition = drift * t;
            rect.localScale = Vector3.one * (1f + t * 0.8f);
            var color = SmokeColor;
            color.a = SmokeColor.a * (1f - t);
            image.color = color;
            yield return null;
        }

        Destroy(puffGo);
    }

    void OnDestroy()
    {
        StopFx();
    }
}
