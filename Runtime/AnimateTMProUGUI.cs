using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;

namespace ATMPro {

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class AnimateTMProUGUI : MonoBehaviour { 
        public bool typeWriter;
        public int defaultDelay;
        public char[] delayBlackList;

        readonly Dictionary<int, List<(ActionInfo actionInfo, string[] value)>> singleActions = new Dictionary<int, List<(ActionInfo actionInfo, string[] value)>>();
        readonly Dictionary<(ActionInfo actionInfo, string[] value), List<(int start, int end)>> pairedActions = new Dictionary<(ActionInfo, string[] ), List<(int, int)>>(new ActionInfoComparer());
        readonly Queue<IEnumerator> typeWriterQueue = new Queue<IEnumerator>();

        int visibleCount;

        CancellationTokenSource actionTokenSource;
        CancellationTokenSource typeWriterTokenSource;

        Queue<RichTagInfo> richTags;

        Action test;

        void OnEnable() {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);

            if (typing) StartCoroutine(TypeWriter(typeWriterTokenSource.Token));
        }

        void OnDisable() {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }
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

        void OnTextChanged(object obj) {
            if ((TextMeshProUGUI)obj == textMeshPro) hasTextChanged = true;
        }

        // CancellationToken t;
        public void SetText(string text, bool additive = false) {
            if (textMeshPro == null) textMeshPro = GetComponent<TextMeshProUGUI>();

            if (!additive) {
                // visibleCount = 0;
                textMeshPro.maxVisibleCharacters = visibleCount = 0; // 因为打字机携程无法判断是否增量更新，所以初始化要放到这里

                (richTags, text) = ValidateRichTags(text);
                textMeshPro.SetText(text);

                // 初始化actionTokenSource。重开文字的话，把原来的actionTokenSource取消了
                actionTokenSource?.Cancel();
                actionTokenSource?.Dispose();

                actionTokenSource = new CancellationTokenSource();
            } else {
                (richTags, text) = ValidateRichTags(text, textMeshPro.text.Length);
                textMeshPro.SetText(textMeshPro.text + text);
                actionTokenSource ??= new CancellationTokenSource();
            }

            PrepareActions(additive);
            ShowText();
        }

        public void Skip() {
            if (typeWriterTokenSource is not { IsCancellationRequested: false }) return;

            typeWriterTokenSource.Cancel();
            typeWriterTokenSource.Dispose();

            textMeshPro.maxVisibleCharacters = visibleCount = textMeshPro.textInfo.characterCount;
        }

        public void SetVisibleCount(int count) => textMeshPro.maxVisibleCharacters = visibleCount = count;


        [HideInInspector] public TextMeshProUGUI textMeshPro;
        [HideInInspector] public bool hasTextChanged;
        [HideInInspector] public int delay;
        [HideInInspector] public char currentChar, lastChar, nextChar;

        void ShowText() {
            // 初始化
            textMeshPro.ForceMeshUpdate();
            delay = defaultDelay;

            // 触发成对 Action
            foreach ((ActionInfo actionInfo, string[] value) tuple in pairedActions.Keys)
                tuple.actionInfo.Invoke(this, actionTokenSource.Token, pairedActions[tuple], tuple.value);

            if (typeWriterTokenSource is { IsCancellationRequested: false }) {
                typeWriterTokenSource.Cancel();
                typeWriterTokenSource.Dispose();
            }

            typeWriterTokenSource = new CancellationTokenSource();

            if (typeWriter) {
                StartCoroutine(TypeWriter(typeWriterTokenSource.Token));
            } else {
                typeWriterQueue.Clear();
                textMeshPro.maxVisibleCharacters = visibleCount = textMeshPro.textInfo.characterCount;
            }
        }

        bool typing;

        IEnumerator TypeWriter(CancellationToken token) {
            typing = true;
            // int index = 0;
            while (visibleCount < textMeshPro.textInfo.characterCount + 1 && !token.IsCancellationRequested) {

                if (!isActiveAndEnabled) {
                    yield return null;
                    continue;
                }

                textMeshPro.maxVisibleCharacters = visibleCount;

                if (singleActions.TryGetValue(visibleCount, out var tuples)) {
                    for (int index = 0; index < tuples.Count; index++) {
                        // 排队进行打字机效果的过程中如果非增量而切换到下一句时，会刷新掉所有 singleActions
                        // 应该终止整个携程
                        if (token.IsCancellationRequested)
                            break;

                        yield return tuples[index].actionInfo.Invoke(this, actionTokenSource.Token, null, tuples[index].value);
                    }
                }

                lastChar = currentChar;
                currentChar = visibleCount != 0 ? textMeshPro.textInfo.characterInfo[visibleCount - 1].character : '\0';
                nextChar = visibleCount != textMeshPro.textInfo.characterCount ? textMeshPro.textInfo.characterInfo[visibleCount].character : '\0';

                // visibleCount包含第一个空字符，第一个字不等待。
                if (visibleCount > 0 && ShouldDelay(currentChar)) {
                    // Debug.Log(currentChar + " : " + delay + " s");
                    float startTime = Time.time;
                    while ((Time.time - startTime) * 1000 < delay && !token.IsCancellationRequested)
                        yield return null;
                }

                visibleCount++;
                // yield return null;
            }
            typing = false;
        }

        bool ShouldDelay(char c) {

            foreach (char black in delayBlackList)
                if (c == black)
                    return false;

            return true;

        }

        (Queue<RichTagInfo> richTagInfos, string text) ValidateRichTags(string text, int offset = 0) {
            List<RichTagInfo> textTags = new List<RichTagInfo>();
            Stack<int> tagIndices = new Stack<int>();

            StringBuilder sb = new StringBuilder();
            sb.Append(text);

            // MatchCollection matches = Regex.Matches(text, @"<(/?[a-z]+)[=]*([a-fA-F0-9]*)>");
            MatchCollection matches = Regex.Matches(text, @"<(/?[a-zA-Z0-9]+ *)[=]*(?<value> *[a-fA-F0-9.]+ *)*(?:,(?<value> *[a-fA-F0-9.]+ *))*>");
            int cutSize = 0 - offset;

            foreach (Match match in matches) {
                string tagStr = match.Value;
                string effectStr = match.Groups[1].ToString().TrimEnd();
                var valuesCaptures = match.Groups[2].Captures;
                string[] valueStrs = new string[valuesCaptures.Count];

                for (var i = 0; i < valuesCaptures.Count; i++) {
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

                    /*int i = tagIndices.Pop();

                    RichTagInfo richTag = textTags[i];
                    
                    if (richTag.type == effectStr.TrimStart('/')) {
                        richTag.endIndex = tagIndex - cutSize;
                        textTags[i] = richTag;
                        cutSize += tagStr.Length;
                        sb.Remove(richTag.endIndex, tagStr.Length);
                    } else {
                        tagIndices.Push(i);
                    }*/
                } else {
                    RichTagInfo richTag = new RichTagInfo();
                    if (!AnimateTMProRichTagManager.TryGetActionInfo(effectStr, out ActionInfo actionInfo)) continue;

                    richTag.type = effectStr;
                    richTag.startIndex = tagIndex - cutSize;
                    richTag.endIndex = -1;

                    for (var index = 0; index < valueStrs.Length; index++) {
                        valueStrs[index] = valueStrs[index].Trim();
                    }

                    richTag.value = valueStrs;

                    if (actionInfo.IsPaired) tagIndices.Push(textTags.Count);

                    textTags.Add(richTag);
                    cutSize += tagStr.Length;
                    sb.Remove(richTag.startIndex - offset, tagStr.Length);
                    // text = text.Remove(richTag.startIndex, tagStr.Length);

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
