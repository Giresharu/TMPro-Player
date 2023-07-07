using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;

namespace TMPPlayer {
    public partial class TMProPlayer {
        //TODO 让TagManager的实现变得更优雅
        //TODO 在执行的时候要判断参数类型是否正确，否则输出bug到面板
        //TODO 测试跳过与标签有没有问题
        //BUG Appear还是有问题，干脆等新版
        readonly Dictionary<int, List<(ActionInfo actionInfo, string[] value)>> singleActions = new Dictionary<int, List<(ActionInfo actionInfo, string[] value)>>();
        readonly Dictionary<(ActionInfo actionInfo, string[] value, int nestLayer), List<(int start, int end)>> pairedActions = new Dictionary<(ActionInfo, string[], int), List<(int, int)>>(new ActionInfoComparer());

        CancellationTokenSource actionTokenSource;
        CancellationTokenSource typeWriterTokenSource;
        Queue<RichTagInfo> richTags;
        TMP_MeshInfo[] cachedMeshInfo;
        TMP_MeshInfo[] backedUpMeshInfo;

        Dictionary<TMP_VertexDataUpdateFlags, HashSet<(int materialReferenceIndex, int vertexIndex)>> backUpIndices;
        TMP_VertexDataUpdateFlags updateFlags = TMP_VertexDataUpdateFlags.None;

        void Start() {
            if (TextMeshPro == null) TextMeshPro = GetComponent<TMP_Text>();
            if (TextMeshPro.text != null) {
                SetText(TextMeshPro.text);
            }
            // 在 UI 被各种因素刷新的时候恢复已经改变的网格信息
            TextMeshPro.OnPreRenderText += RecoverMeshInfo;
        }

        void LateUpdate() {
            if (updateFlags != 0) {
                TextMeshPro.UpdateVertexData(updateFlags);
                updateFlags = 0;
            }

            hasRecoverInFrame = false;
        }

        void OnDestroy() {
            // if (actionTokenSource is not { IsCancellationRequested: false }) return;
            if (actionTokenSource != null) {
                if (!actionTokenSource.IsCancellationRequested) actionTokenSource.Cancel();
                actionTokenSource.Dispose();
            }

            if (typeWriterTokenSource != null) {
                if (!typeWriterTokenSource.IsCancellationRequested) typeWriterTokenSource.Cancel();
                typeWriterTokenSource.Dispose();
            }
        }

        // 根据索引备份网格
        void BackUpMeshInfo(TMP_VertexDataUpdateFlags updateFlag, HashSet<(int, int)> indices, int materialReferenceIndex = -1, int vertexIndex = -1) {
            TMP_MeshInfo[] meshInfo = TextMeshPro.textInfo.meshInfo;
            backUpIndices ??= new Dictionary<TMP_VertexDataUpdateFlags, HashSet<(int, int)>>();
            backedUpMeshInfo ??= new TMP_MeshInfo[meshInfo.Length];
            if (backedUpMeshInfo.Length < meshInfo.Length) Array.Resize(ref backedUpMeshInfo, meshInfo.Length);

            if (indices == null) {
                BackUpMeshInfoProcess(materialReferenceIndex, vertexIndex);
                return;
            }

            foreach ((int materialReferenceIndex, int vertexIndex) tuple in indices) {
                // backUpIndices[TMP_VertexDataUpdateFlags.Colors32].Add(tuple);
                materialReferenceIndex = tuple.materialReferenceIndex;
                vertexIndex = tuple.vertexIndex;
                BackUpMeshInfoProcess(materialReferenceIndex, vertexIndex, indices);
            }

            void BackUpMeshInfoProcess(int mrIndex, int vtIndex, HashSet<(int, int)> hashSet = null) {
                int length = meshInfo[mrIndex].vertices.Length;

                if (updateFlag.HasFlag(TMP_VertexDataUpdateFlags.Colors32)) {
                    bool freshHashSet = backUpIndices.TryAdd(TMP_VertexDataUpdateFlags.Colors32, hashSet ?? new HashSet<(int, int)>());
                    if (!freshHashSet || hashSet == null)
                        backUpIndices[TMP_VertexDataUpdateFlags.Colors32].Add((mrIndex, vtIndex));

                    if (backedUpMeshInfo[mrIndex].colors32 == null || backedUpMeshInfo[mrIndex].colors32.Length < length)
                        backedUpMeshInfo[mrIndex].colors32 = new Color32[length];

                    backedUpMeshInfo[mrIndex].colors32[vtIndex] = meshInfo[mrIndex].colors32[vtIndex];
                    backedUpMeshInfo[mrIndex].colors32[vtIndex + 1] = meshInfo[mrIndex].colors32[vtIndex + 1];
                    backedUpMeshInfo[mrIndex].colors32[vtIndex + 2] = meshInfo[mrIndex].colors32[vtIndex + 2];
                    backedUpMeshInfo[mrIndex].colors32[vtIndex + 3] = meshInfo[mrIndex].colors32[vtIndex + 3];
                }

                if (updateFlag.HasFlag(TMP_VertexDataUpdateFlags.Vertices)) {
                    bool freshHashSet = backUpIndices.TryAdd(TMP_VertexDataUpdateFlags.Vertices, hashSet ?? new HashSet<(int, int)>());
                    if (!freshHashSet || hashSet == null)
                        backUpIndices[TMP_VertexDataUpdateFlags.Vertices].Add((mrIndex, vtIndex));

                    if (backedUpMeshInfo[mrIndex].vertices == null || backedUpMeshInfo[mrIndex].vertices.Length < length)
                        backedUpMeshInfo[mrIndex].vertices = new Vector3[length];

                    backedUpMeshInfo[mrIndex].vertices[vtIndex] = meshInfo[mrIndex].vertices[vtIndex];
                    backedUpMeshInfo[mrIndex].vertices[vtIndex + 1] = meshInfo[mrIndex].vertices[vtIndex + 1];
                    backedUpMeshInfo[mrIndex].vertices[vtIndex + 2] = meshInfo[mrIndex].vertices[vtIndex + 2];
                    backedUpMeshInfo[mrIndex].vertices[vtIndex + 3] = meshInfo[mrIndex].vertices[vtIndex + 3];
                }

                if (updateFlag.HasFlag(TMP_VertexDataUpdateFlags.Uv0)) {
                    bool freshHashSet = backUpIndices.TryAdd(TMP_VertexDataUpdateFlags.Uv0, hashSet ?? new HashSet<(int, int)>());
                    if (!freshHashSet || hashSet == null)
                        backUpIndices[TMP_VertexDataUpdateFlags.Uv0].Add((mrIndex, vtIndex));

                    if (backedUpMeshInfo[mrIndex].uvs0 == null || backedUpMeshInfo[mrIndex].uvs0.Length < length)
                        backedUpMeshInfo[mrIndex].uvs0 = new Vector2[length];

                    backedUpMeshInfo[mrIndex].uvs0[vtIndex] = meshInfo[mrIndex].uvs0[vtIndex];
                    backedUpMeshInfo[mrIndex].uvs0[vtIndex + 1] = meshInfo[mrIndex].uvs0[vtIndex + 1];
                    backedUpMeshInfo[mrIndex].uvs0[vtIndex + 2] = meshInfo[mrIndex].uvs0[vtIndex + 2];
                    backedUpMeshInfo[mrIndex].uvs0[vtIndex + 3] = meshInfo[mrIndex].uvs0[vtIndex + 3];
                }

                if (updateFlag.HasFlag(TMP_VertexDataUpdateFlags.Uv2)) {
                    bool freshHashSet = backUpIndices.TryAdd(TMP_VertexDataUpdateFlags.Uv2, hashSet ?? new HashSet<(int, int)>());
                    if (!freshHashSet || hashSet == null)
                        backUpIndices[TMP_VertexDataUpdateFlags.Uv2].Add((mrIndex, vtIndex));

                    if (backedUpMeshInfo[mrIndex].uvs2 == null || backedUpMeshInfo[mrIndex].uvs2.Length < length)
                        backedUpMeshInfo[mrIndex].uvs2 = new Vector2[length];

                    backedUpMeshInfo[mrIndex].uvs2[vtIndex] = meshInfo[mrIndex].uvs2[vtIndex];
                    backedUpMeshInfo[mrIndex].uvs2[vtIndex + 1] = meshInfo[mrIndex].uvs2[vtIndex + 1];
                    backedUpMeshInfo[mrIndex].uvs2[vtIndex + 2] = meshInfo[mrIndex].uvs2[vtIndex + 2];
                    backedUpMeshInfo[mrIndex].uvs2[vtIndex + 3] = meshInfo[mrIndex].uvs2[vtIndex + 3];
                }
            }
        }

        // 根据备份的索引去还原需要复原的网格
        void RecoverMeshInfo(TMP_TextInfo textInfo) {

            RefreshCachedMeshInfo(true);
            TMP_MeshInfo[] meshInfo = textInfo.meshInfo;

            HideMeshInfo(VisibleCount);
            if (backUpIndices == null) return;

            if (backUpIndices.TryGetValue(TMP_VertexDataUpdateFlags.Colors32, out HashSet<(int, int)> colorIndices)) {
                foreach ((int i, int vertexIndex) in colorIndices) {
                    //TODO 判断meshInfo长度防止变短（应该是不会变短吧暂时不写了）
                    meshInfo[i].colors32[vertexIndex] = backedUpMeshInfo[i].colors32[vertexIndex];
                    meshInfo[i].colors32[vertexIndex + 1] = backedUpMeshInfo[i].colors32[vertexIndex + 1];
                    meshInfo[i].colors32[vertexIndex + 2] = backedUpMeshInfo[i].colors32[vertexIndex + 2];
                    meshInfo[i].colors32[vertexIndex + 3] = backedUpMeshInfo[i].colors32[vertexIndex + 3];
                }
            }

            if (backUpIndices.TryGetValue(TMP_VertexDataUpdateFlags.Vertices, out HashSet<(int, int)> vertexIndices)) {
                foreach ((int i, int vertexIndex) in vertexIndices) {
                    meshInfo[i].vertices[vertexIndex] = backedUpMeshInfo[i].vertices[vertexIndex];
                    meshInfo[i].vertices[vertexIndex + 1] = backedUpMeshInfo[i].vertices[vertexIndex + 1];
                    meshInfo[i].vertices[vertexIndex + 2] = backedUpMeshInfo[i].vertices[vertexIndex + 2];
                    meshInfo[i].vertices[vertexIndex + 3] = backedUpMeshInfo[i].vertices[vertexIndex + 3];
                }
            }

            if (backUpIndices.TryGetValue(TMP_VertexDataUpdateFlags.Uv0, out HashSet<(int, int)> uv0Indices)) {
                foreach ((int i, int vertexIndex) in uv0Indices) {
                    meshInfo[i].uvs0[vertexIndex] = backedUpMeshInfo[i].uvs0[vertexIndex];
                    meshInfo[i].uvs0[vertexIndex + 1] = backedUpMeshInfo[i].uvs0[vertexIndex + 1];
                    meshInfo[i].uvs0[vertexIndex + 2] = backedUpMeshInfo[i].uvs0[vertexIndex + 2];
                    meshInfo[i].uvs0[vertexIndex + 3] = backedUpMeshInfo[i].uvs0[vertexIndex + 3];
                }
            }

            if (backUpIndices.TryGetValue(TMP_VertexDataUpdateFlags.Uv2, out HashSet<(int, int)> uv2Indices)) {
                foreach ((int i, int vertexIndex) in uv2Indices) {
                    meshInfo[i].uvs2[vertexIndex] = backedUpMeshInfo[i].uvs2[vertexIndex];
                    meshInfo[i].uvs2[vertexIndex + 1] = backedUpMeshInfo[i].uvs2[vertexIndex + 1];
                    meshInfo[i].uvs2[vertexIndex + 2] = backedUpMeshInfo[i].uvs2[vertexIndex + 2];
                    meshInfo[i].uvs2[vertexIndex + 3] = backedUpMeshInfo[i].uvs2[vertexIndex + 3];
                }
            }
        }

        void HideMeshInfo(int indexBegin = 0) {
            TMP_MeshInfo[] meshInfo = TextMeshPro.textInfo.meshInfo;
            int index = indexBegin;
            // 防止暂停的过程中触发网格刷新，导致已经++的 VisibleCount 被刷出来
            //TODO !IsTyping 是干嘛的来着？忘了有机会查一下
            if (!IsTyping || IsSuspending || IsPausing) index--;

            while (index < TextMeshPro.textInfo.characterCount) {
                if (index >= 0) {
                    if (TextMeshPro.textInfo.characterInfo[index].isVisible) {

                        int materialIndex = TextMeshPro.textInfo.characterInfo[index].materialReferenceIndex;
                        int vertexCount = TextMeshPro.textInfo.characterInfo[index].vertexIndex;

                        meshInfo[materialIndex].colors32[vertexCount].a = 0;
                        meshInfo[materialIndex].colors32[vertexCount + 1].a = 0;
                        meshInfo[materialIndex].colors32[vertexCount + 2].a = 0;
                        meshInfo[materialIndex].colors32[vertexCount + 3].a = 0;

                        // 下面的注释是用于测试，以黑色来表示还未显示的文字
                        /*meshInfo[materialIndex].colors32[vertexCount] = new Color32(0, 0, 0, 255);
                        meshInfo[materialIndex].colors32[vertexCount + 1] = new Color32(0, 0, 0, 255);
                        meshInfo[materialIndex].colors32[vertexCount + 2] = new Color32(0, 0, 0, 255);
                        meshInfo[materialIndex].colors32[vertexCount + 3] = new Color32(0, 0, 0, 255);*/
                    }
                }
                index++;
            }
        }

        void RefreshCachedMeshInfo(bool isRecover = false) {
            int index = 0;
            //TODO 好像没机会 !isRecover 啊？只要 cachedMeshInfo的长度变了 就必须要从头复制（要不用Array.Resize）另外，之前为什么希望 isRecover 的时候从 0 呢？大概是因为因为有很多会改变排版的东西吧。
            if (!isRecover) index = (VisibleCount - 1) * 4;

            TMP_MeshInfo[] meshInfo = TextMeshPro.textInfo.meshInfo;
            cachedMeshInfo ??= new TMP_MeshInfo[meshInfo.Length];

            if (cachedMeshInfo.Length < meshInfo.Length) Array.Resize(ref cachedMeshInfo, meshInfo.Length);

            for (int i = 0; i < meshInfo.Length; i++) {
                int length = meshInfo[i].vertices.Length;
                //TODO 想想能不能把明显没有字的长度排除了 不要去复制
                if (cachedMeshInfo[i].vertices == null || cachedMeshInfo[i].vertices.Length < length) cachedMeshInfo[i].vertices = new Vector3[length];
                if (cachedMeshInfo[i].uvs0 == null || cachedMeshInfo[i].uvs0.Length < length) cachedMeshInfo[i].uvs0 = new Vector2[length];
                if (cachedMeshInfo[i].uvs2 == null || cachedMeshInfo[i].uvs2.Length < length) cachedMeshInfo[i].uvs2 = new Vector2[length];
                if (cachedMeshInfo[i].colors32 == null || cachedMeshInfo[i].colors32.Length < length) cachedMeshInfo[i].colors32 = new Color32[length];

                Array.Copy(meshInfo[i].vertices, index, cachedMeshInfo[i].vertices, index, length - index);
                Array.Copy(meshInfo[i].uvs0, index, cachedMeshInfo[i].uvs0, index, length - index);
                Array.Copy(meshInfo[i].uvs2, index, cachedMeshInfo[i].uvs2, index, length - index);
                Array.Copy(meshInfo[i].colors32, index, cachedMeshInfo[i].colors32, index, length - index);
            }
            hasRecoverInFrame = true;
        }

        bool hasRecoverInFrame;
        void InitMeshInfo() {
            if (hasRecoverInFrame) return;
            RefreshCachedMeshInfo();
            // 判断打字机效果隐藏文字
            if (isTypeWriter) {
                HideMeshInfo();
                TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }

        }

        bool softSkipOn;
        int tupleIndexHasInvoke = -1;

        // bool isSoftSkipping;
        IEnumerator SoftSkipCoroutine(bool oneShot = false) {
            IsSoftSkipping = true;
            while (IsPausing || !isActiveAndEnabled) {
                if (softSkipOn) yield return null;
                else {
                    IsSoftSkipping = false;
                    yield break;
                }
            }

            while (VisibleCount <= TextMeshPro.textInfo.characterCount + 1) {
                if (VisibleCount <= TextMeshPro.textInfo.characterCount)
                    SetCharacterLog();

                while (CheckInvokeTagIndex()) {
                    if (singleActions.TryGetValue(invokeTagIndex, out var tuples)) {
                        for (int i = 0; i < tuples.Count; i++) {

                            IEnumerator coroutine = tuples[i].actionInfo.Invoke(this, actionTokenSource.Token, null, tuples[i].value);
                            tupleIndexHasInvoke = i; //记录已经被触发的索引

                            if (coroutine != null) {

                                yield return coroutine;
                                // 从 Func 回来后万一已经不需要跳过了就标记退出
                                if (!softSkipOn || oneShot) {
                                    softSkipOn = false;
                                    IsSoftSkipping = false;
                                    yield break;
                                }

                            }

                        }
                        tupleIndexHasInvoke = -1; //全部执行完了恢复 -1 表示下一次循环还未触发任何索引
                    }
                    invokeTagIndex++;
                }

                if (VisibleCount > TextMeshPro.textInfo.characterCount) break;

                RemoveUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
                DisplayCharacter();

                VisibleCount++;
            }
            // 软跳最终会回归打字机，所以后事都在打字机处理
            IsSoftSkipping = false;
        }
        
        void ShowText(bool isAdditive = false) {
            // 初始化
            TextMeshPro.ForceMeshUpdate();

            if (!isAdditive && typeWriterTokenSource is { IsCancellationRequested: false }) {
                typeWriterTokenSource.Cancel();
                typeWriterTokenSource.Dispose();
                typeWriterTokenSource = new CancellationTokenSource();
            }

            typeWriterTokenSource ??= new CancellationTokenSource();

            InitMeshInfo();
            Delay = defaultDelay;

            if (isTypeWriter) {
                if (softSkipOn && !IsSoftSkipping) StartCoroutine(SoftSkipCoroutine());
                if (!isAdditive || !IsTyping) StartCoroutine(TypeWriter(typeWriterTokenSource.Token));
            } else {
                // typeWriterQueue.Clear();
                while (VisibleCount <= TextMeshPro.textInfo.characterCount) {
                    // 防止被不显示第二个起的字
                    RemoveUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
                    DisplayCharacter();
                    VisibleCount++;
                }
                singleActions.Clear();
                VisibleCount--;
                invokeTagIndex = TextMeshPro.text.Length;
            }

            // 触发成对 Action
            foreach (var tuple in pairedActions.Keys)
                tuple.actionInfo.Invoke(this, actionTokenSource.Token, pairedActions[tuple], tuple.value);

        }

        void DisplayCharacter() {
            // 有时打字机协程会晚于其他协程执行，可能会覆盖掉其他协程对颜色的修改，所以一旦检测到已经修改过颜色，就不要继续去覆盖颜色了；
            if (VisibleCount <= 0 || !TextMeshPro.textInfo.characterInfo[VisibleCount - 1].isVisible || updateFlags.HasFlag(TMP_VertexDataUpdateFlags.Colors32))
                return;

            int materialIndex = TextMeshPro.textInfo.characterInfo[VisibleCount - 1].materialReferenceIndex;
            int vertexIndex = TextMeshPro.textInfo.characterInfo[VisibleCount - 1].vertexIndex;

            Color32[] dstColors = TextMeshPro.textInfo.meshInfo[materialIndex].colors32;
            Color32[] srcColors = cachedMeshInfo[materialIndex].colors32;

            dstColors[vertexIndex] = srcColors[vertexIndex];
            dstColors[vertexIndex + 1] = srcColors[vertexIndex + 1];
            dstColors[vertexIndex + 2] = srcColors[vertexIndex + 2];
            dstColors[vertexIndex + 3] = srcColors[vertexIndex + 3];

            AddUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);

        }

        void SetCharacterLog() {
            LastChar = VisibleCount > 1 ? CurrentChar : new TMP_CharacterInfo { character = '\0' };
            CurrentChar = VisibleCount > 0 ? TextMeshPro.textInfo.characterInfo[VisibleCount - 1] : new TMP_CharacterInfo { character = '\0' };
            NextChar = VisibleCount < TextMeshPro.textInfo.characterCount ? TextMeshPro.textInfo.characterInfo[VisibleCount] : new TMP_CharacterInfo { character = '\0' };
        }
        
        IEnumerator TypeWriter(CancellationToken token) {
            IsTyping = true;
            while (VisibleCount <= TextMeshPro.textInfo.characterCount + 1 && !token.IsCancellationRequested) {

                if (!isActiveAndEnabled || !TextMeshPro.isActiveAndEnabled || IsSuspending) {
                    yield return null;
                    continue;
                }

                if (VisibleCount <= TextMeshPro.textInfo.characterCount)
                    SetCharacterLog();

                while (CheckInvokeTagIndex()) {
                    if (singleActions.TryGetValue(invokeTagIndex, out var tuples)) {
                        for (int i = tupleIndexHasInvoke + 1; i < tuples.Count; i++) {

                            //防止返回null的时候被yield return 延迟一帧
                            IEnumerator coroutine = tuples[i].actionInfo.Invoke(this, actionTokenSource.Token, null, tuples[i].value);
                            if (coroutine != null) {
                                IsPausing = true;
                                yield return coroutine;
                                IsPausing = false;
                                // 防止暂停的过程中被取消，导致后续还被执行，所以再检查一次
                                if (token.IsCancellationRequested) yield break;
                            }
                        }
                    }
                    invokeTagIndex++;
                }

                if (VisibleCount > TextMeshPro.textInfo.characterCount) break;

                // 正在软跳的时候暂停打字机效果
                while (IsSoftSkipping) {
                    yield return null;
                    if (token.IsCancellationRequested) yield break;
                }

                // 防止因为存在 Color32 的 flag 而不会显示新的文字
                if (Delay <= 0) RemoveUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
                DisplayCharacter();

                if (TextMeshPro.textInfo.characterInfo[VisibleCount - 1].isVisible) {
                    // Debug.Log(CurrentChar);
                    float startTime = Time.time;
                    while ((Time.time - startTime) * 1000 < Delay / timeScale && !token.IsCancellationRequested) yield return null;
                    if (token.IsCancellationRequested) yield break;
                }
                VisibleCount++;
            }

            // 由于取消打字机效果有滞后性，会晚于新的打字机效果，所以必须判断是否是自然结束再来处理后事，否则会覆盖新打字机效果的运行
            if (token.IsCancellationRequested) yield break;

            IsTyping = false;
            backUpIndices?.Clear(); // 清除所有需要复原的
            singleActions.Clear();  // 防止 additive 后执行没有触发的，所以提前干掉
            VisibleCount--;
            invokeTagIndex = TextMeshPro.text.Length;
        }

        bool CheckInvokeTagIndex() {
            // 当目前遍历到的 invokeTagIndex 还在下一个文字的索引之前，就要执行到至今为止的标签；
            // 就一边递增 invokeTagIndex 一边把对应的 action 执行掉;
            bool inRange = VisibleCount - 1 < TextMeshPro.textInfo.characterCount && invokeTagIndex <= TextMeshPro.textInfo.characterInfo[VisibleCount - 1].index;
            bool afterRange = VisibleCount == TextMeshPro.textInfo.characterCount + 1 && invokeTagIndex <= TextMeshPro.text.Length;
            return inRange || afterRange;
        }

        (Queue<RichTagInfo> richTagInfos, string text) ValidateRichTags(string text, int offset = 0, bool newline = false) {
            List<RichTagInfo> textTags = new List<RichTagInfo>();
            Stack<int> tagIndices = new Stack<int>();

            StringBuilder sb = new StringBuilder();

            if (newline) sb.Append('\n');
            sb.Append(openStyle);
            sb.Append(text);
            sb.Append(closeStyle);

            MatchCollection matches = Regex.Matches(sb.ToString(), @"<(/?[a-zA-Z0-9]+ *)[=]*(?<value> *[a-zA-Z0-9.%]+ *)*(?:,(?<value> *[a-zA-Z0-9.%]+ *))*>");
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
                        if (tagIndices.Count <= 0) break;
                        int i = tagIndices.Pop();
                        // if (!tagIndices.TryPop(out int i)) break;

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
                    if (!TMPPlayerRichTagManager.TryGetActionInfo(effectStr, out ActionInfo actionInfo)) continue;

                    richTag.type = effectStr;
                    richTag.startIndex = tagIndex - cutSize;
                    richTag.endIndex = -1;

                    for (int index = 0; index < valueStrs.Length; index++) {
                        valueStrs[index] = valueStrs[index].Trim();
                    }

                    richTag.value = valueStrs;

                    if (actionInfo.IsPaired) {
                        richTag.nestLayer = tagIndices.Count;
                        tagIndices.Push(textTags.Count);
                    }

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
            //如果不是增量更新，也要清除单个标签的 Action
            if (!additive) singleActions.Clear();


            while (richTags.Count > 0) {
                RichTagInfo richTagInfo = richTags.Dequeue();
                if (richTagInfo.endIndex == -1) {
                    if (!singleActions.ContainsKey(richTagInfo.startIndex))
                        singleActions[richTagInfo.startIndex] = new List<(ActionInfo actionInfo, string[] value)>();

                    singleActions[richTagInfo.startIndex].Add((TMPPlayerRichTagManager.GetActionInfo(richTagInfo.type), richTagInfo.value));

                } else {
                    ActionInfo actionInfo = TMPPlayerRichTagManager.GetActionInfo(richTagInfo.type);
                    if (!pairedActions.ContainsKey((actionInfo, richTagInfo.value, richTagInfo.nestLayer)))
                        pairedActions.Add((actionInfo, richTagInfo.value, richTagInfo.nestLayer), new List<(int start, int end)>());

                    pairedActions[(actionInfo, richTagInfo.value, richTagInfo.nestLayer)].Add((richTagInfo.startIndex, richTagInfo.endIndex));
                }
            }
        }

        class ActionInfoComparer : IEqualityComparer<(ActionInfo actionInfo, string[] value, int nestLayer)> {
            public bool Equals((ActionInfo actionInfo, string[] value, int nestLayer) x, (ActionInfo actionInfo, string[] value, int nestLayer) y) {
                if (x.nestLayer != y.nestLayer) return false;
                if (x.actionInfo != y.actionInfo) return false;
                if (x.value.Equals(y.value)) return true;

                if (x.value.Length != y.value.Length) return false;

                for (var i = 0; i < x.value.Length; i++) {
                    if (!x.value[i].Equals(y.value[i])) return false;
                }
                return true;
            }
            public int GetHashCode((ActionInfo actionInfo, string[] value, int nestLayer) obj) {
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

        int invokeTagIndex;
        internal struct RichTagInfo {
            internal string type;
            internal int startIndex;
            internal int endIndex;
            internal string[] value;
            internal int nestLayer;
        }
    }
}
