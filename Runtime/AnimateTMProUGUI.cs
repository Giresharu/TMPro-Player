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
        public bool typeWriter;
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
        public void SetText(string text, bool additive = false, bool newline = false) {
            if (textMeshPro == null) textMeshPro = GetComponent<TextMeshProUGUI>();

            /*if (textMeshPro.textStyle.styleOpeningDefinition != "")
                openString = (textMeshPro.textStyle.styleOpeningDefinition);

            if (textMeshPro.textStyle.styleClosingDefinition != "")
                closeString = (textMeshPro.textStyle.styleClosingDefinition);

            textMeshPro.textStyle = TMP_Style.NormalStyle;*/

            if (!additive) {
                // visibleCount = 0;
                textMeshPro.maxVisibleCharacters = visibleCount = 0; // 因为打字机携程无法判断是否增量更新，所以初始化要放到这里
                typingIndex = 0;

                (richTags, text) = ValidateRichTags(text, newline: newline);
                textMeshPro.SetText(text);

                // 初始化actionTokenSource。重开文字的话，把原来的actionTokenSource取消了
                actionTokenSource?.Cancel();
                actionTokenSource?.Dispose();

                actionTokenSource = new CancellationTokenSource();
            } else {
                (richTags, text) = ValidateRichTags(text, textMeshPro.text.Length, newline);
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
            typingIndex = textMeshPro.text.Length - 1;
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
                typingIndex = textMeshPro.text.Length - 1;
            }
        }

        bool typing;
        int visibleCount;
        int typingIndex;

        IEnumerator TypeWriter(CancellationToken token) {
            typing = true;
            while (visibleCount < textMeshPro.textInfo.characterCount + 1 && !token.IsCancellationRequested) {

                if (!isActiveAndEnabled) {
                    yield return null;
                    continue;
                }

                textMeshPro.maxVisibleCharacters = visibleCount;

                // 解决会被自动隐藏的 tmp 自带标签不被算进 character 但是有被我们用来计算了 action 的 start 以及 end 的问题；
                // 当目前遍历到的 typingIndex 不能与当前 visible 的最后一个 character 的 index 匹配时；
                // 就一边递增 typingIndex 一边把对应的 action 执行掉
                while (typingIndex <= textMeshPro.textInfo.characterInfo[visibleCount].index) {
                    // indices.Add(index);
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
                if (visibleCount > 0 && ShouldDelay(currentChar)) {
                    // Debug.Log(currentChar + " : " + delay + " s");
                    float startTime = Time.time;
                    while ((Time.time - startTime) * 1000 < delay && !token.IsCancellationRequested)
                        yield return null;
                    if (token.IsCancellationRequested) yield break;
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

        (Queue<RichTagInfo> richTagInfos, string text) ValidateRichTags(string text, int offset = 0, bool newline = false) {
            List<RichTagInfo> textTags = new List<RichTagInfo>();
            Stack<int> tagIndices = new Stack<int>();

            StringBuilder sb = new StringBuilder();

            if (newline) sb.Append('\n');
            sb.Append(openStyle);
            sb.Append(text);
            sb.Append(closeStyle);
            // if (delayForLastChar) sb.Append("<#00000000>0</color>");

            // MatchCollection matches = Regex.Matches(text, @"<(/?[a-z]+)[=]*([a-fA-F0-9]*)>");
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
