using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace ATMPro {

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class AnimateTMProUGUI : MonoBehaviour {
        [FormerlySerializedAs("typeWriter")]
        public bool isTypeWriter;
        public string openStyle;
        public string closeStyle;
        public int defaultDelay;
        public char[] delayBlackList;

        readonly Dictionary<int, List<(ActionInfo actionInfo, string[] value)>> singleActions = new Dictionary<int, List<(ActionInfo actionInfo, string[] value)>>();
        readonly Dictionary<(ActionInfo actionInfo, string[] value), List<(int start, int end)>> pairedActions = new Dictionary<(ActionInfo, string[] ), List<(int, int)>>(new ActionInfoComparer());
        readonly Queue<IEnumerator> typeWriterQueue = new Queue<IEnumerator>();

        CancellationTokenSource actionTokenSource;
        CancellationTokenSource typeWriterTokenSource;

        Queue<RichTagInfo> richTags;

        Action test;

        void OnEnable() {
            // TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);

            if (typing) StartCoroutine(TypeWriter(typeWriterTokenSource.Token));

        }

        /*void OnDisable() {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }*/

        void Start() {
            if (textMeshPro == null) textMeshPro = GetComponent<TextMeshProUGUI>();
            if (textMeshPro.text != null) {
                SetText(textMeshPro.text);
            }

        }
        void OnDestroy() {
            if (actionTokenSource is not { IsCancellationRequested: false }) return;

            actionTokenSource.Cancel();
            actionTokenSource.Dispose();

            if (typeWriterTokenSource is not { IsCancellationRequested: false }) return;

            typeWriterTokenSource.Cancel();
            typeWriterTokenSource.Dispose();
        }

        /*void OnTextChanged(object obj) {
            if ((TextMeshProUGUI)obj == textMeshPro) {
            }
        }*/

        public TMP_MeshInfo[] cachedMeshInfo;
        public TMP_VertexDataUpdateFlags updateFlags = TMP_VertexDataUpdateFlags.None;


        void InitMeshInfo() {
            TMP_MeshInfo[] meshInfo = textMeshPro.textInfo.meshInfo;
            cachedMeshInfo ??= new TMP_MeshInfo[meshInfo.Length];

            if (cachedMeshInfo.Length < meshInfo.Length) Array.Resize(ref cachedMeshInfo, meshInfo.Length);

            for (int i = 0; i < meshInfo.Length; i++) {
                int length = meshInfo[i].vertices.Length;

                if (cachedMeshInfo[i].vertices == null || cachedMeshInfo[i].vertices.Length < length) cachedMeshInfo[i].vertices = new Vector3[length];
                if (cachedMeshInfo[i].uvs0 == null || cachedMeshInfo[i].uvs0.Length < length) cachedMeshInfo[i].uvs0 = new Vector2[length];
                if (cachedMeshInfo[i].uvs2 == null || cachedMeshInfo[i].uvs2.Length < length) cachedMeshInfo[i].uvs2 = new Vector2[length];
                if (cachedMeshInfo[i].colors32 == null || cachedMeshInfo[i].colors32.Length < length) cachedMeshInfo[i].colors32 = new Color32[length];

                Array.Copy(meshInfo[i].vertices, cachedMeshInfo[i].vertices, length);
                Array.Copy(meshInfo[i].uvs0, cachedMeshInfo[i].uvs0, length);
                Array.Copy(meshInfo[i].uvs2, cachedMeshInfo[i].uvs2, length);
                Array.Copy(meshInfo[i].colors32, cachedMeshInfo[i].colors32, length);
            }
            // 判断打字机效果隐藏文字
            if (isTypeWriter) {

                int index = visibleCount;
                while (index < textMeshPro.textInfo.characterCount) {
                    if (index > 0) {
                        if (textMeshPro.textInfo.characterInfo[index].isVisible) {

                            int materialIndex = textMeshPro.textInfo.characterInfo[index].materialReferenceIndex;
                            int vertexCount = textMeshPro.textInfo.characterInfo[index].vertexIndex;

                            meshInfo[materialIndex].colors32[vertexCount].a = 0;
                            meshInfo[materialIndex].colors32[vertexCount + 1].a = 0;
                            meshInfo[materialIndex].colors32[vertexCount + 2].a = 0;
                            meshInfo[materialIndex].colors32[vertexCount + 3].a = 0;
                        }
                    }
                    index++;
                }
                // }
                updateFlags |= TMP_VertexDataUpdateFlags.Colors32;


            }

        }

        public void LateUpdate() {
            if (updateFlags == 0) return;
            // Debug.Log("LateUpdate");
            textMeshPro.UpdateVertexData(updateFlags);
            updateFlags = 0;
        }
        
        public void SetText(string text, bool isAdditive = false, bool newline = false) {
            if (textMeshPro == null) textMeshPro = GetComponent<TextMeshProUGUI>();

            if (!isAdditive) {

                visibleCount = 0; // 因为打字机携程无法判断是否增量更新，所以初始化要放到这里
                typingIndex = 0;

                (richTags, text) = ValidateRichTags(text, newline: newline);
                textMeshPro.SetText(text);

                // 初始化actionTokenSource。重开文字的话，把原来的actionTokenSource取消了
                actionTokenSource?.Cancel();
                actionTokenSource?.Dispose();

                actionTokenSource = new CancellationTokenSource();
            } else {
                // countBeforeAdditive = textMeshPro.textInfo.characterCount;
                updateFlags = TMP_VertexDataUpdateFlags.None;

                (richTags, text) = ValidateRichTags(text, textMeshPro.text.Length, newline);
                textMeshPro.SetText(textMeshPro.text + text);
                actionTokenSource ??= new CancellationTokenSource();
            }

            PrepareActions(isAdditive);
            ShowText(isAdditive);
        }

        public void Skip() {
            if (typeWriterTokenSource is not { IsCancellationRequested: false }) return;

            typeWriterTokenSource.Cancel();
            typeWriterTokenSource.Dispose();
            
            while (visibleCount <= textMeshPro.textInfo.characterCount) {
                DisplayCharacter();
                visibleCount++;
            }

            typingIndex = textMeshPro.text.Length - 1;
            updateFlags |= TMP_VertexDataUpdateFlags.Colors32;
        }

        [HideInInspector] public TextMeshProUGUI textMeshPro;
        [HideInInspector] public int delay;
        [HideInInspector] public char currentChar, lastChar, nextChar;

        void ShowText(bool isAdditive = false) {
            // 初始化
            textMeshPro.ForceMeshUpdate();
            // if (!isAdditive)
            InitMeshInfo();
            delay = defaultDelay;

            if (!isAdditive && typeWriterTokenSource is { IsCancellationRequested: false }) {
                typeWriterTokenSource.Cancel();
                typeWriterTokenSource.Dispose();
                typeWriterTokenSource = new CancellationTokenSource();
            }

            typeWriterTokenSource ??= new CancellationTokenSource();

            if (isTypeWriter) {
                if (!isAdditive || !typing) StartCoroutine(TypeWriter(typeWriterTokenSource.Token));
            } else {
                typeWriterQueue.Clear();

                while (visibleCount <= textMeshPro.textInfo.characterCount) {
                    DisplayCharacter();
                    visibleCount++;
                }
                typingIndex = textMeshPro.text.Length - 1;
                updateFlags |= TMP_VertexDataUpdateFlags.Colors32;

            }

            // 触发成对 Action
            foreach ((ActionInfo actionInfo, string[] value) tuple in pairedActions.Keys)
                tuple.actionInfo.Invoke(this, actionTokenSource.Token, pairedActions[tuple], tuple.value);
        }
        

        void DisplayCharacter() {
            
            // 有时打字机协程会晚于其他协程执行，可能会覆盖掉其他协程对颜色的修改，所以一旦检测到已经修改过颜色，就不要继续去覆盖颜色了；
            if (visibleCount <= 0 || !textMeshPro.textInfo.characterInfo[visibleCount - 1].isVisible || (updateFlags & TMP_VertexDataUpdateFlags.Colors32) != 0)
                return;

            int materialIndex = textMeshPro.textInfo.characterInfo[visibleCount - 1].materialReferenceIndex;
            int vertexIndex = textMeshPro.textInfo.characterInfo[visibleCount - 1].vertexIndex;

            Color32[] dstColors = textMeshPro.textInfo.meshInfo[materialIndex].colors32;
            Color32[] srcColors = cachedMeshInfo[materialIndex].colors32;

            dstColors[vertexIndex] = srcColors[vertexIndex];
            dstColors[vertexIndex + 1] = srcColors[vertexIndex + 1];
            dstColors[vertexIndex + 2] = srcColors[vertexIndex + 2];
            dstColors[vertexIndex + 3] = srcColors[vertexIndex + 3];

            updateFlags |= TMP_VertexDataUpdateFlags.Colors32;

        }

        bool typing;
        public int visibleCount;
        int typingIndex;

        IEnumerator TypeWriter(CancellationToken token) {
            typing = true;
            while (visibleCount < textMeshPro.textInfo.characterCount + 1 && !token.IsCancellationRequested) {

                if (!isActiveAndEnabled) {
                    yield return null;
                    continue;
                }
                
                DisplayCharacter();

                // 解决会被自动隐藏的 tmp 自带标签不被算进 character 但是有被我们用来计算了 action 的 start 以及 end 的问题；
                // 当目前遍历到的 typingIndex 不能与当前 visible 的最后一个 character 的 index 匹配时；
                // 就一边递增 typingIndex 一边把对应的 action 执行掉
                while (textMeshPro.textInfo.characterCount > visibleCount && typingIndex <= textMeshPro.textInfo.characterInfo[visibleCount].index) {

                    if (singleActions.TryGetValue(typingIndex, out var tuples)) {
                        for (int i = 0; i < tuples.Count; i++) {
                            // 排队进行打字机效果的过程中如果非增量而切换到下一句时，会刷新掉所有 singleActions
                            // 应该终止整个携程
                            if (token.IsCancellationRequested)
                                yield break;

                            yield return tuples[i].actionInfo.Invoke(this, actionTokenSource.Token, null, tuples[i].value);
                        }
                    }
                    typingIndex++;
                }

                lastChar = currentChar;
                currentChar = visibleCount != 0 ? textMeshPro.textInfo.characterInfo[visibleCount - 1].character : '\0';
                nextChar = visibleCount != textMeshPro.textInfo.characterCount ? textMeshPro.textInfo.characterInfo[visibleCount].character : '\0';

                // visibleCount包含第一个空字符，第一个字不等待。
                if (visibleCount > 0 && ShouldDelay(currentChar) && textMeshPro.textInfo.characterInfo[visibleCount - 1].isVisible) {

                    float startTime = Time.time;
                    while ((Time.time - startTime) * 1000 < delay && !token.IsCancellationRequested)
                        yield return null;
                    if (token.IsCancellationRequested) break;
                }

                visibleCount++;

            }
            typing = false;
        }

        bool ShouldDelay(char c) {

            foreach (char black in delayBlackList)
                if (c == black)
                    return false;

            return true;

        }

        (Queue<RichTagInfo> richTagInfos, string text) ValidateRichTags(string text, int offset = 0, bool newline = false) {
            List<RichTagInfo> textTags = new List<RichTagInfo>();
            Stack<int> tagIndices = new Stack<int>();

            StringBuilder sb = new StringBuilder();

            if (newline) sb.Append('\n');
            sb.Append(openStyle);
            sb.Append(text);
            sb.Append(closeStyle);

            MatchCollection matches = Regex.Matches(sb.ToString(), @"<(/?[a-zA-Z0-9]+ *)[=]*(?<value> *[a-fA-F0-9.%]+ *)*(?:,(?<value> *[a-fA-F0-9.%]+ *))*>");
            int cutSize = 0 - offset;

            foreach (Match match in matches) {
                string tagStr = match.Value;
                // Debug.Log(tagStr);
                string effectStr = match.Groups[1].ToString().TrimEnd();
                var valuesCaptures = match.Groups[2].Captures;
                string[] valueStrs = new string[valuesCaptures.Count];

                for (int i = 0; i < valuesCaptures.Count; i++) {
                    valueStrs[i] = valuesCaptures[i].ToString();
                }

                int tagIndex = match.Index; //标签在text中的索引（以第一个字符为准）

                if (tagStr.StartsWith("</")) {
                    if (tagIndices.Count == 0) continue;

                    Stack<int> temp = new Stack<int>();

                    while (true) {
                        if (!tagIndices.TryPop(out int i)) break;

                        RichTagInfo richTagInfo = textTags[i];

                        if (richTagInfo.type != effectStr.TrimStart('/')) {
                            temp.Push(i);
                        } else {
                            richTagInfo.endIndex = tagIndex - cutSize;
                            textTags[i] = richTagInfo;
                            cutSize += tagStr.Length;
                            sb.Remove(richTagInfo.endIndex - offset, tagStr.Length);
                            break;
                        }
                    }
                    while (temp.Count > 0) {
                        tagIndices.Push(temp.Pop());
                    }
                } else {
                    RichTagInfo richTag = new RichTagInfo();
                    if (!AnimateTMProRichTagManager.TryGetActionInfo(effectStr, out ActionInfo actionInfo)) continue;

                    richTag.type = effectStr;
                    richTag.startIndex = tagIndex - cutSize;
                    richTag.endIndex = -1;

                    for (int index = 0; index < valueStrs.Length; index++) {
                        valueStrs[index] = valueStrs[index].Trim();
                    }

                    richTag.value = valueStrs;

                    if (actionInfo.IsPaired) tagIndices.Push(textTags.Count);

                    textTags.Add(richTag);
                    cutSize += tagStr.Length;
                    sb.Remove(richTag.startIndex - offset, tagStr.Length);

                }
            }
            text = sb.ToString();

            return (new Queue<RichTagInfo>(textTags), text);
        }

        void PrepareActions(bool additive = false) {
            // 清除成对的标签的 Action ，是因为他们是在播放文字前触发的，会重复触发
            pairedActions.Clear();
            //如果是不是增量更新，也要清除单个标签的 Action
            if (!additive) singleActions.Clear();


            while (richTags.Count > 0) {
                RichTagInfo richTagInfo = richTags.Dequeue();
                if (richTagInfo.endIndex == -1) {
                    if (!singleActions.ContainsKey(richTagInfo.startIndex))
                        singleActions[richTagInfo.startIndex] = new List<(ActionInfo actionInfo, string[] value)>();

                    singleActions[richTagInfo.startIndex].Add((AnimateTMProRichTagManager.GetActionInfo(richTagInfo.type), richTagInfo.value));

                } else {
                    ActionInfo actionInfo = AnimateTMProRichTagManager.GetActionInfo(richTagInfo.type);
                    if (!pairedActions.ContainsKey((actionInfo, richTagInfo.value)))
                        pairedActions.Add((actionInfo, richTagInfo.value), new List<(int start, int end)>());

                    pairedActions[(actionInfo, richTagInfo.value)].Add((richTagInfo.startIndex, richTagInfo.endIndex));
                }
            }
        }

        class ActionInfoComparer : IEqualityComparer<(ActionInfo actionInfo, string[] value)> {

            public bool Equals((ActionInfo actionInfo, string[] value) x, (ActionInfo actionInfo, string[] value) y) {
                if (x.actionInfo != y.actionInfo) return false;
                if (x.value.Equals(y.value)) return true;

                if (x.value.Length != y.value.Length) return false;

                for (var i = 0; i < x.value.Length; i++) {
                    if (!x.value[i].Equals(y.value[i], StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            }
            public int GetHashCode((ActionInfo actionInfo, string[] value) obj) {
                if (obj.actionInfo == null && obj.value == null) {
                    return 0;
                }

                int hash = 17;

                foreach (var s in obj.value) {
                    hash = hash * 23 + (s != null ? s.GetHashCode() : 0);
                }

                hash = hash * 23 + (obj.actionInfo != null ? obj.actionInfo.GetHashCode() : 0);

                return hash;
            }
        }

    }
}
