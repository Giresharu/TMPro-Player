using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;

namespace TMPPlayer {

    [RequireComponent(typeof(TMP_Text))][Icon("Packages/com.gsr.tmproplayer/Icons/player.png")]
    public class TMProPlayer : MonoBehaviour {

        public bool isTypeWriter = true;
        public string openStyle;
        public string closeStyle;
        public int defaultDelay = 75;
        public float timeScale = 1;
        // public char[] delayBlackList;

        readonly Dictionary<int, List<(ActionInfo actionInfo, string[] value)>> singleActions = new Dictionary<int, List<(ActionInfo actionInfo, string[] value)>>();
        readonly Dictionary<(ActionInfo actionInfo, string[] value, int nestLayer), List<(int start, int end)>> pairedActions = new Dictionary<(ActionInfo, string[], int), List<(int, int)>>(new ActionInfoComparer());
        // readonly Queue<IEnumerator> typeWriterQueue = new Queue<IEnumerator>();

        CancellationTokenSource actionTokenSource;
        CancellationTokenSource typeWriterTokenSource;

        Queue<RichTagInfo> richTags;

        TMP_MeshInfo[] cachedMeshInfo;
        public TMP_MeshInfo[] CachedMeshInfo { get { return cachedMeshInfo; } }

        TMP_MeshInfo[] backedUpMeshInfo;
        Dictionary<TMP_VertexDataUpdateFlags, HashSet<(int materialReferenceIndex, int vertexIndex)>> backUpIndices;

        TMP_VertexDataUpdateFlags updateFlags = TMP_VertexDataUpdateFlags.None;

        int lastInvokeIndex;

        public int Delay { get; set; }
        public TMP_Text TextMeshPro { get; private set; }
        public TMP_CharacterInfo CurrentChar { get; private set; }
        public TMP_CharacterInfo LastChar { get; private set; }
        public TMP_CharacterInfo NextChar { get; private set; }
        // public bool UseCustomCharacterDisplay { get; set; }

        public bool IsTyping { get; private set; }
        public int VisibleCount { get; private set; }


        void OnEnable() {
            if (IsTyping) StartCoroutine(TypeWriter(typeWriterTokenSource.Token));
        }

        void Start() {
            if (TextMeshPro == null) TextMeshPro = GetComponent<TMP_Text>();
            if (TextMeshPro.text != null) {
                SetText(TextMeshPro.text);
            }
            // 在 UI 被各种因素刷新的时候恢复已经改变的网格信息
            TextMeshPro.OnPreRenderText += RecoverMeshInfo;
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

        /// <summary>
        /// 记录本帧要更新渲染的 flag，如果想要 OnPreRenderText 刷新 UI 时恢复到本次修改而非恢复到初始状态，请填写需要记录的 indices
        /// </summary>
        /// <param name="updateFlags">需要更新渲染的 flags</param>
        /// <param name="indices">需要记录的 materialReferenceIndex 及其材质中的 vertexIndex</param>
        public void AddUpdateFlags(TMP_VertexDataUpdateFlags updateFlags, HashSet<(int materialReferenceIndex, int vertexIndex)> indices = null) {
            if (indices is { Count: > 0 }) BackUpMeshInfo(updateFlags, indices);
            this.updateFlags |= updateFlags;
        }

        /// <summary>
        /// 记录本帧要更新渲染的 flag，如果想要 OnPreRenderText 刷新 UI 时恢复到本次修改而非恢复到初始状态，请填写需要记录的 indices
        /// </summary>
        /// <param name="updateFlags">需要更新渲染的 flags</param>
        /// <param name="materialReferenceIndex">需要记录的 materialReferenceIndex</param>
        /// <param name="vertexIndex">materialReferenceIndex 所代表的材质中需要记录的 vertexIndex</param>
        public void AddUpdateFlags(TMP_VertexDataUpdateFlags updateFlags, int materialReferenceIndex, int vertexIndex) {
            BackUpMeshInfo(updateFlags, null, materialReferenceIndex, vertexIndex);
            this.updateFlags |= updateFlags;
        }

        /// <summary>
        /// 移除本帧要更新的 flag
        /// </summary>
        /// <param name="updateFlags">需要移除更新渲染的 flags</param>
        public void RemoveUpdateFlags(TMP_VertexDataUpdateFlags updateFlags) {
            this.updateFlags &= ~updateFlags;
        }

        /// <summary>
        /// 检查本帧是否需要更新此 flag
        /// </summary>
        /// <param name="updateFlags">需要检查的 flags</param>
        /// <returns></returns>
        public bool CheckUpdateFlags(TMP_VertexDataUpdateFlags updateFlags) {
            // return (this.updateFlags & updateFlags) != 0;
            return this.updateFlags.HasFlag(updateFlags);
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

            RefreshCachedMeshInfo();
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

        void RefreshCachedMeshInfo() {
            TMP_MeshInfo[] meshInfo = TextMeshPro.textInfo.meshInfo;
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
        }

        void InitMeshInfo(bool isAdditive = false) {
            // TMP_MeshInfo[] meshInfo = TextMeshPro.textInfo.meshInfo;
            RefreshCachedMeshInfo();
            // 判断打字机效果隐藏文字
            if (isTypeWriter) {
                int index = isAdditive ? VisibleCount : 0;
                HideMeshInfo(index);
                TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                // AddUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
            }

        }

        public void LateUpdate() {
            if (updateFlags == 0) return;

            TextMeshPro.UpdateVertexData(updateFlags);
            updateFlags = 0;
        }

        /// <summary>
        /// 设置文本
        /// </summary>
        /// <param name="text">设置的文本内容</param>
        /// <param name="isAdditive">是否增量更新</param>
        /// <param name="newline">是否另起一行</param>
        public void SetText(string text, bool isAdditive = false, bool newline = false) {
            if (TextMeshPro == null) TextMeshPro = GetComponent<TextMeshProUGUI>();

            if (!isAdditive) {

                VisibleCount = 0; // 因为打字机携程无法判断是否增量更新，所以初始化要放到这里
                lastInvokeIndex = 0;

                (richTags, text) = ValidateRichTags(text, newline: newline);
                TextMeshPro.SetText(text);

                // 初始化actionTokenSource。重开文字的话，把原来的actionTokenSource取消了
                actionTokenSource?.Cancel();
                actionTokenSource?.Dispose();

                actionTokenSource = new CancellationTokenSource();
            } else {
                // countBeforeAdditive = textMeshPro.textInfo.characterCount;
                updateFlags = TMP_VertexDataUpdateFlags.None;

                (richTags, text) = ValidateRichTags(text, TextMeshPro.text.Length, newline);
                TextMeshPro.SetText(TextMeshPro.text + text);
                actionTokenSource ??= new CancellationTokenSource();
            }

            PrepareActions(isAdditive);
            ShowText(isAdditive);
        }

        /// <summary>
        /// 跳过当前打字机效果
        /// </summary>
        /// <param name="invokeSingleActions"> 是否需要执行跳过的文字中的单个标签 </param>
        public void Skip(bool invokeSingleActions = true) {
            if (typeWriterTokenSource is not { IsCancellationRequested: false }) return;

            typeWriterTokenSource.Cancel();
            typeWriterTokenSource.Dispose();
            typeWriterTokenSource = new CancellationTokenSource();

            IsHardSkipping = true;
            /*bool needDisplay = !CheckUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
            if (needDisplay) {*/
            while (VisibleCount <= TextMeshPro.textInfo.characterCount) {
                SetCharacterLog();

                if (VisibleCount > 0 && TextMeshPro.textInfo.characterInfo[VisibleCount - 1].isVisible) {
                    RemoveUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
                    DisplayCharacter();
                }

                //TODO 可能需要更多测试
                while (invokeSingleActions && (VisibleCount < TextMeshPro.textInfo.characterCount && lastInvokeIndex <= TextMeshPro.textInfo.characterInfo[VisibleCount].index) || (VisibleCount == TextMeshPro.textInfo.characterCount && lastInvokeIndex <= TextMeshPro.text.Length)) {

                    if (singleActions.TryGetValue(lastInvokeIndex, out var tuples)) {
                        for (int i = 0; i < tuples.Count; i++) {

                            IEnumerator coroutine = tuples[i].actionInfo.Invoke(this, actionTokenSource.Token, null, tuples[i].value);
                            if (coroutine != null) StartCoroutine(coroutine);

                        }
                    }
                    lastInvokeIndex++;
                }


                VisibleCount++;
            }
            /*} else VisibleCount = TextMeshPro.textInfo.characterCount;*/

            IsTyping = false;
            IsHardSkipping = false;
            backUpIndices?.Clear(); // 清除所有需要复原的

            if (VisibleCount > TextMeshPro.textInfo.characterCount) VisibleCount = TextMeshPro.textInfo.characterCount;
            if (lastInvokeIndex >= TextMeshPro.text.Length) lastInvokeIndex = TextMeshPro.text.Length;
        }

        /// <summary>
        /// 设置软跳过，将会在 Func 类型的标签中正确暂停，随时可以设置 false 停止
        /// </summary>
        /// <param name="value">设置软跳过是否启动</param>
        public void SetSoftSkip(bool value) {
            if (softSkipOn == value) return;

            softSkipOn = value;
            if (softSkipOn) StartCoroutine(SoftSkipCoroutine());
        }

        /// <summary>
        /// 设置一次软跳过，然后立马取消。将会跳过到 Func 类型的标签。
        /// </summary>
        public void SoftSkip() {
            if (softSkipOn) return;
            softSkipOn = true;
            StartCoroutine(SoftSkipCoroutine(true));
        }

        public bool IsSkipping { get { return IsSoftSkipping || IsHardSkipping; } }
        public bool IsHardSkipping { get; private set; }
        public bool IsSoftSkipping { get; private set; }
        bool softSkipOn;
        int tupleIndexHasInvoke = -1;

        // bool isSoftSkipping;
        IEnumerator SoftSkipCoroutine(bool oneShot = false) {
            IsSoftSkipping = true;
            while (isFuncWaiting || !isActiveAndEnabled) {
                if (softSkipOn) yield return null;
                else {
                    IsSoftSkipping = false;
                    yield break;
                }
            }

            while (VisibleCount <= TextMeshPro.textInfo.characterCount) {
                SetCharacterLog();

                if (VisibleCount > 0 && TextMeshPro.textInfo.characterInfo[VisibleCount - 1].isVisible) {
                    RemoveUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
                    DisplayCharacter();
                }

                while ((VisibleCount < TextMeshPro.textInfo.characterCount && lastInvokeIndex <= TextMeshPro.textInfo.characterInfo[VisibleCount].index) || (VisibleCount == TextMeshPro.textInfo.characterCount && lastInvokeIndex <= TextMeshPro.text.Length)) {
                    if (singleActions.TryGetValue(lastInvokeIndex, out var tuples)) {
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
                    lastInvokeIndex++;
                }
                VisibleCount++;
            }

            if (VisibleCount > TextMeshPro.textInfo.characterCount) VisibleCount = TextMeshPro.textInfo.characterCount;
            if (lastInvokeIndex >= TextMeshPro.text.Length) lastInvokeIndex = TextMeshPro.text.Length;
            // softSkipOn = false;
            IsSoftSkipping = false;
        }


        void ShowText(bool isAdditive = false) {
            // 初始化
            TextMeshPro.ForceMeshUpdate();
            if (!isAdditive) backUpIndices?.Clear();

            if (!isAdditive && typeWriterTokenSource is { IsCancellationRequested: false }) {
                typeWriterTokenSource.Cancel();
                typeWriterTokenSource.Dispose();
                typeWriterTokenSource = new CancellationTokenSource();
            }

            typeWriterTokenSource ??= new CancellationTokenSource();

            InitMeshInfo(isAdditive);
            Delay = defaultDelay;

            if (isTypeWriter) {
                if (softSkipOn && !IsSoftSkipping) StartCoroutine(SoftSkipCoroutine());
                if (!isAdditive || !IsTyping) StartCoroutine(TypeWriter(typeWriterTokenSource.Token));
            } else {
                // typeWriterQueue.Clear();
                while (VisibleCount <= TextMeshPro.textInfo.characterCount) {
                    if (VisibleCount > 0 && TextMeshPro.textInfo.characterInfo[VisibleCount - 1].isVisible) DisplayCharacter();
                    VisibleCount++;
                }
                lastInvokeIndex = TextMeshPro.text.Length - 1;
                // AddUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
            }

            // 触发成对 Action
            foreach (var tuple in pairedActions.Keys)
                tuple.actionInfo.Invoke(this, actionTokenSource.Token, pairedActions[tuple], tuple.value);

        }


        void DisplayCharacter() {
            // 有时打字机协程会晚于其他协程执行，可能会覆盖掉其他协程对颜色的修改，所以一旦检测到已经修改过颜色，就不要继续去覆盖颜色了；
            if (VisibleCount <= 0 || !TextMeshPro.textInfo.characterInfo[VisibleCount - 1].isVisible || (updateFlags & TMP_VertexDataUpdateFlags.Colors32) != 0)
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

        bool isFuncWaiting;
        IEnumerator TypeWriter(CancellationToken token) {
            IsTyping = true;
            while (VisibleCount <= TextMeshPro.textInfo.characterCount && !token.IsCancellationRequested) {

                if (!isActiveAndEnabled) {
                    yield return null;
                    continue;
                }

                SetCharacterLog();

                // 防止因为存在 Color32 的 flag 而不会显示新的文字
                if (Delay <= 0) RemoveUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
                DisplayCharacter();

                if (VisibleCount > 0 /*&& ShouldDelay(CurrentChar)*/ && TextMeshPro.textInfo.characterInfo[VisibleCount - 1].isVisible) {
                    // Debug.Log(CurrentChar);
                    float startTime = Time.time;
                    while ((Time.time - startTime) * 1000 < Delay / timeScale && !token.IsCancellationRequested)
                        // yield return null;
                        yield return null /*new WaitForEndOfFrame()*/;

                    if (token.IsCancellationRequested) {
                        IsTyping = false;
                        yield break;
                    }
                }

                // 正在软跳的时候暂停打字机效果
                while (IsSoftSkipping) {
                    yield return null;
                }

                if (token.IsCancellationRequested) {
                    IsTyping = false;
                    yield break;
                }

                // 解决会被自动隐藏的 tmp 自带标签不被算进 character 但是有被我们用来计算了 action 的 start 以及 end 的问题；
                // 当目前遍历到的 lastInvokeIndex 不能与当前 visible 的最后一个 character 的 index 匹配时；
                // 就一边递增 lastInvokeIndex 一边把对应的 action 执行掉
                while ((VisibleCount < TextMeshPro.textInfo.characterCount && lastInvokeIndex <= TextMeshPro.textInfo.characterInfo[VisibleCount].index) ||
                       (VisibleCount == TextMeshPro.textInfo.characterCount && lastInvokeIndex <= TextMeshPro.text.Length)) { // 当所有文字都显示完毕后，还要继续触发后面的标签

                    if (singleActions.TryGetValue(lastInvokeIndex, out var tuples)) {
                        for (int i = tupleIndexHasInvoke + 1; i < tuples.Count; i++) {

                            //防止返回null的时候被yield return 延迟一帧
                            IEnumerator coroutine = tuples[i].actionInfo.Invoke(this, actionTokenSource.Token, null, tuples[i].value);
                            if (coroutine != null) {
                                isFuncWaiting = true;
                                yield return coroutine;
                                isFuncWaiting = false;
                            }
                            // 防止暂停的过程中被取消，导致后续还被执行，所以再检查一次
                            if (token.IsCancellationRequested) {
                                IsTyping = false;
                                yield break;
                            }
                        }
                    }
                    lastInvokeIndex++;
                }

                VisibleCount++;
            }

            if (VisibleCount > TextMeshPro.textInfo.characterCount) VisibleCount = TextMeshPro.textInfo.characterCount;
            if (lastInvokeIndex >= TextMeshPro.text.Length) lastInvokeIndex = TextMeshPro.text.Length;

            IsTyping = false;
            backUpIndices?.Clear();
        }

        /*bool ShouldDelay(char c) {

            foreach (char black in delayBlackList)
                if (c == black)
                    return false;

            return true;

        }*/

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
    }

    internal struct RichTagInfo {
        internal string type;
        internal int startIndex;
        internal int endIndex;
        internal string[] value;
        internal int nestLayer;
    }
}
