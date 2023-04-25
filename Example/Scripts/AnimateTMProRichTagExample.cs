using System.Collections;
using System.Collections.Generic;
using ATMPro;
using System;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimateTMProRichTagExample : AnimateTMProRichTagManager {

    protected override void Initialize() {
        base.Initialize();
        SetActionInfo(args => StartCoroutine(Shake((AnimateTMProUGUI)args[0], (float)args[1], (float)args[2], (float)args[3], (CancellationToken)args[4], (List<(int, int)>)args[5])), "Shake", true, "shake", "Shake", "sh", "Sh");
        SetActionInfo(args => StartCoroutine(Appear((AnimateTMProUGUI)args[0], (int)args[1], (CancellationToken)args[2], (List<(int, int)>)args[3])), "Appear", true, "appear", "Appear");
    }

    /* --- Action Region --- */
#region Action Region
    static IEnumerator Appear(AnimateTMProUGUI atmp, int time = 500, CancellationToken token = default, List<(int start, int end)> ranges = null) {
        TMP_TextInfo textInfo = atmp.textMeshPro.textInfo;

        List<int> isAppearInRange = IndicesInRange(textInfo, ranges);

        while (true) {
            foreach (int i in isAppearInRange) {

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                Color32[] dstColors = textInfo.meshInfo[materialIndex].colors32;
                Color32[] srcColors = atmp.cachedMeshInfo[materialIndex].colors32;

                dstColors[vertexIndex].a = (byte)(srcColors[vertexIndex].a * 0.5f);
                dstColors[vertexIndex + 1].a = (byte)(srcColors[vertexIndex + 1].a * 0.5f);
                dstColors[vertexIndex + 2].a = (byte)(srcColors[vertexIndex + 2].a * 0.5f);
                dstColors[vertexIndex + 3].a = (byte)(srcColors[vertexIndex + 3].a * 0.5f);
            }
            if (atmp.textMeshPro.maxVisibleCharacters > 1)
                atmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            // atmp.textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            yield return null;

        }

    }



    static IEnumerator Shake(AnimateTMProUGUI atmp, float frequency = 15f, float amplitude = 1f, float phaseshift = 10f, CancellationToken token = default, List<(int start, int end)> ranges = null) {

        TMP_TextInfo textInfo = atmp.textMeshPro.textInfo;

        float startTime = Time.time;
        float angle = Random.value * 2f * Mathf.PI; //把 0 ~ 1 的随机数映射到 0 ~ 2PI 的弧度

        List<int> indexInRange = IndicesInRange(textInfo, ranges);

        while (!token.IsCancellationRequested) {
            // 当 disable 时暂停本协程
            if (!atmp.isActiveAndEnabled || textInfo.characterCount == 0) {
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

                Vector3[] srcVertices = atmp.cachedMeshInfo[materialIndex].vertices;

                // if (srcVertices == null) break;

                Vector3[] dstVertices = textInfo.meshInfo[materialIndex].vertices;

                dstVertices[vertexIndex] = srcVertices[vertexIndex] + offset;
                dstVertices[vertexIndex + 1] = srcVertices[vertexIndex + 1] + offset;
                dstVertices[vertexIndex + 2] = srcVertices[vertexIndex + 2] + offset;
                dstVertices[vertexIndex + 3] = srcVertices[vertexIndex + 3] + offset;

            }
            if (atmp.textMeshPro.maxVisibleCharacters > 1)
                atmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            // atmp.textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            // 每次完成一个频次就重新生成一个弧度
            if (Time.time - startTime > 1f / frequency) {
                angle = Random.value * 2f * Mathf.PI;
                startTime = Time.time;
            }
            yield return null;
        }
    }
#endregion

}
