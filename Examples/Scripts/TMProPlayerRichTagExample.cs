using System.Collections;
using System.Collections.Generic;
using TMPPlayer;
using System.Threading;
using TMPro;
using UnityEngine;

public class TMProPlayerRichTagExample : TMPPlayerRichTagManager {

    protected override void Initialize() {
        base.Initialize();
        SetActionInfo(args => StartCoroutine(Shake((TMProPlayer)args[0], (float)args[1], (float)args[2], (float)args[3], (CancellationToken)args[4], (List<(int, int)>)args[5])), "Shake", true, "shake", "Shake", "sh", "Sh");
        SetActionInfo(args => StartCoroutine(Appear((TMProPlayer)args[0], (int)args[1], (CancellationToken)args[2], (List<(int, int)>)args[3])), "Appear", true, "appear", "Appear", "Ap", "ap");
        SetActionInfo(args => StartCoroutine(Wave((TMProPlayer)args[0], (float)args[1], (float)args[2], (float)args[3], (CancellationToken)args[4], (List<(int, int)>)args[5])), "Wave", true, "Wave", "wave", "Wa", "wa");
    }

    /* --- Action Region --- */
#region Action Region
    static IEnumerator Appear(TMProPlayer tmpp, int time = 500, CancellationToken token = default, List<(int start, int end)> ranges = null) {
        TMP_TextInfo textInfo = tmpp.TextMeshPro.textInfo;

        HashSet<int> isAppearInRange = IndicesInRangeHashSet(textInfo, ranges);

        int lastVisibleCount = 0;
        while (isAppearInRange.Count > 0 && !token.IsCancellationRequested) {
            if (!tmpp.isActiveAndEnabled || !tmpp.TextMeshPro.isActiveAndEnabled) {
                yield return null;
                continue;
            }

            while (lastVisibleCount < tmpp.VisibleCount) {
                lastVisibleCount++;
                if (isAppearInRange.Contains(lastVisibleCount - 1)) {
                    isAppearInRange.Remove(lastVisibleCount - 1);
                    tmpp.StartCoroutine(AppearAnimation(lastVisibleCount - 1));
                }
            }
            yield return null;
        }

        IEnumerator AppearAnimation(int i) {
            if (!textInfo.characterInfo[i].isVisible) yield break;

            float startTime = Time.time;
            float pausedTime = 0;

            while (!token.IsCancellationRequested) {

                if (!tmpp.isActiveAndEnabled || !tmpp.TextMeshPro.isActiveAndEnabled) {
                    yield return null;
                    pausedTime += Time.deltaTime;
                    continue;
                }

                float alpha = Mathf.Clamp01((Time.time - startTime - pausedTime) * 1000 / time);

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                Color32[] dstColors = textInfo.meshInfo[materialIndex].colors32;
                Color32[] srcColors = tmpp.CachedMeshInfo[materialIndex].colors32;

                dstColors[vertexIndex].a = (byte)Mathf.Clamp(srcColors[vertexIndex].a * alpha, 0, 255);
                dstColors[vertexIndex + 1].a = (byte)Mathf.Clamp(srcColors[vertexIndex + 1].a * alpha, 0, 255);
                dstColors[vertexIndex + 2].a = (byte)Mathf.Clamp(srcColors[vertexIndex + 2].a * alpha, 0, 255);
                dstColors[vertexIndex + 3].a = (byte)Mathf.Clamp(srcColors[vertexIndex + 3].a * alpha, 0, 255);

                tmpp.AddUpdateFlags(TMP_VertexDataUpdateFlags.Colors32, materialIndex, vertexIndex);

                if (alpha >= 1) yield break;

                yield return null;
            }

        }

    }

    static IEnumerator Shake(TMProPlayer tmpp, float frequency = 15f, float amplitude = 1f, float phaseshift = 10f, CancellationToken token = default, List<(int start, int end)> ranges = null) {

        TMP_TextInfo textInfo = tmpp.TextMeshPro.textInfo;

        float startTime = Time.time;
        float angle = Random.value * 2f * Mathf.PI; //把 0 ~ 1 的随机数映射到 0 ~ 2PI 的弧度

        List<int> indexInRange = IndicesInRange(textInfo, ranges);

        while (!token.IsCancellationRequested) {
            // 当 disable 时暂停本协程
            if (!tmpp.isActiveAndEnabled || !tmpp.TextMeshPro.isActiveAndEnabled || textInfo.characterCount == 0) {
                yield return null;
                continue;
            }

            foreach (int i in indexInRange) {
                if (ranges == null || token.IsCancellationRequested)
                    break;

                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible) continue;

                Vector3 direction = new Vector3(Mathf.Cos(angle + i * phaseshift), Mathf.Sin(angle + i * phaseshift), 0f);
                float theta = (Time.time - startTime) * frequency * 2f * Mathf.PI; // 时间 * 频率 = 进度， 进度再映射到弧度，用作正弦函数的自变量

                float distance = Mathf.Sin(theta) * amplitude;

                Vector3 offset = direction * distance;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                Vector3[] srcVertices = tmpp.CachedMeshInfo[materialIndex].vertices;

                // if (srcVertices == null) break;

                Vector3[] dstVertices = textInfo.meshInfo[materialIndex].vertices;

                dstVertices[vertexIndex] = srcVertices[vertexIndex] + offset;
                dstVertices[vertexIndex + 1] = srcVertices[vertexIndex + 1] + offset;
                dstVertices[vertexIndex + 2] = srcVertices[vertexIndex + 2] + offset;
                dstVertices[vertexIndex + 3] = srcVertices[vertexIndex + 3] + offset;

            }

            if (tmpp.VisibleCount > 0) tmpp.AddUpdateFlags(TMP_VertexDataUpdateFlags.Vertices);

            // 每次完成一个频次就重新生成一个弧度
            if (Time.time - startTime > 1f / frequency) {
                angle = Random.value * 2f * Mathf.PI;
                startTime = Time.time;
            }
            yield return null;
        }
    }

    static IEnumerator Wave(TMProPlayer tmpp, float frequency = 1f, float amplitude = 10f, float phaseshift = 0.1f, CancellationToken token = default, List<(int start, int end)> ranges = null) {
        if (ranges == null) yield break;

        TMP_TextInfo textInfo = tmpp.TextMeshPro.textInfo;

        float startTime = Time.time;
        float pausedTime = 0;

        List<int> indexInRange = IndicesInRange(textInfo, ranges);

        while (!token.IsCancellationRequested) {

            if (!tmpp.isActiveAndEnabled || !tmpp.TextMeshPro.isActiveAndEnabled || textInfo.characterCount == 0) {
                yield return null;
                pausedTime += Time.deltaTime;
                continue;
            }

            HashSet<(int, int)> backUpIndices = new HashSet<(int, int)>();

            foreach (int i in indexInRange) {
                if (token.IsCancellationRequested) break;

                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible) continue;

                float theta = ((Time.time - startTime - pausedTime) * frequency - phaseshift * i) * 2f * Mathf.PI;
                float distance = Mathf.Sin(theta) * amplitude;
                Vector3 offset = Vector3.up * distance;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                Vector3[] srcVertices = tmpp.CachedMeshInfo[materialIndex].vertices;
                Vector3[] dstVertices = textInfo.meshInfo[materialIndex].vertices;

                dstVertices[vertexIndex] = srcVertices[vertexIndex] + offset;
                dstVertices[vertexIndex + 1] = srcVertices[vertexIndex + 1] + offset;
                dstVertices[vertexIndex + 2] = srcVertices[vertexIndex + 2] + offset;
                dstVertices[vertexIndex + 3] = srcVertices[vertexIndex + 3] + offset;

                backUpIndices.Add((materialIndex, vertexIndex));
            }

            if (tmpp.VisibleCount > 0) tmpp.AddUpdateFlags(TMP_VertexDataUpdateFlags.Vertices, backUpIndices);


            if (Time.time - startTime - pausedTime > 1f / frequency) {
                startTime = Time.time;
                pausedTime = 0;
            }
            yield return null;
        }
    }
#endregion

}
