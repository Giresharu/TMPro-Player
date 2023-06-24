using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace TMPPlayer {

    [RequireComponent(typeof(TMP_Text))][Icon("Packages/com.gsr.tmproplayer/Icons/player.png")]
    public partial class TMProPlayer : MonoBehaviour {

        public bool isTypeWriter = true;
        public string openStyle;
        public string closeStyle;
        public int defaultDelay = 75;
        public float timeScale = 1;
        // public char[] delayBlackList;


        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public int Delay { get; set; }
        public TMP_Text TextMeshPro { get; private set; }
        public TMP_CharacterInfo CurrentChar { get; private set; }
        public TMP_CharacterInfo LastChar { get; private set; }
        public TMP_CharacterInfo NextChar { get; private set; }
        public TMP_MeshInfo[] CachedMeshInfo { get { return cachedMeshInfo; } }

        public bool IsTyping { get; private set; }
        public int VisibleCount { get; private set; }
        public bool IsSkipping { get { return IsSoftSkipping || IsHardSkipping; } }
        public bool IsHardSkipping { get; private set; }
        public bool IsSoftSkipping { get; private set; }

        public bool IsSuspending { get; private set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global

        /// <summary>
        /// 记录本帧要更新渲染的 flag，如果想要 OnPreRenderText 刷新 UI 时恢复到本次修改而非恢复到初始状态，请填写需要记录的 indices
        /// </summary>
        /// <param name="updateFlags">需要更新渲染的 flags</param>
        /// <param name="indices">需要记录的 materialReferenceIndex 及其材质中的 vertexIndex</param>
        // ReSharper disable once ParameterHidesMember
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
        // ReSharper disable once ParameterHidesMember
        public void AddUpdateFlags(TMP_VertexDataUpdateFlags updateFlags, int materialReferenceIndex, int vertexIndex) {
            BackUpMeshInfo(updateFlags, null, materialReferenceIndex, vertexIndex);
            this.updateFlags |= updateFlags;
        }

        /// <summary>
        /// 移除本帧要更新的 flag
        /// </summary>
        /// <param name="updateFlags">需要移除更新渲染的 flags</param>
        // ReSharper disable once ParameterHidesMember
        public void RemoveUpdateFlags(TMP_VertexDataUpdateFlags updateFlags) {
            this.updateFlags &= ~updateFlags;
        }

        /// <summary>
        /// 检查本帧是否需要更新此 flag
        /// </summary>
        /// <param name="updateFlags">需要检查的 flags</param>
        /// <returns></returns>
        // ReSharper disable once ParameterHidesMember
        public bool CheckUpdateFlags(TMP_VertexDataUpdateFlags updateFlags) {
            // return (this.updateFlags & updateFlags) != 0;
            return this.updateFlags.HasFlag(updateFlags);
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
                VisibleCount = 1; // 因为打字机携程无法判断是否增量更新，所以初始化要放到这里
                invokeTagIndex = 0;
                backUpIndices?.Clear();

                (richTags, text) = ValidateRichTags(text, newline: newline);
                TextMeshPro.SetText(text);

                // 初始化actionTokenSource。重开文字的话，把原来的actionTokenSource取消了
                actionTokenSource?.Cancel();
                actionTokenSource?.Dispose();

                actionTokenSource = new CancellationTokenSource();
            } else {
                if (!IsTyping) {
                    VisibleCount = TextMeshPro.textInfo.characterCount + 1;
                    invokeTagIndex = TextMeshPro.text.Length;
                }
                updateFlags = TMP_VertexDataUpdateFlags.None;
                (richTags, text) = ValidateRichTags(text, TextMeshPro.text.Length, newline);
                TextMeshPro.SetText(TextMeshPro.text + text);
                actionTokenSource ??= new CancellationTokenSource();
            }

            PrepareActions(isAdditive);
            ShowText(isAdditive);
        }

        /// <summary>
        /// 设置打字机效果的暂停
        /// </summary>
        /// <param name="value">是否暂停</param>
        public void SetSuspend(bool value) {
            IsSuspending = value;
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

            while (VisibleCount <= TextMeshPro.textInfo.characterCount + 1) {
                if (VisibleCount <= TextMeshPro.textInfo.characterCount)
                    SetCharacterLog();

                while (invokeSingleActions && CheckInvokeTagIndex()) {
                    if (singleActions.TryGetValue(invokeTagIndex, out var tuples)) {
                        for (int i = 0; i < tuples.Count; i++) {

                            IEnumerator coroutine = tuples[i].actionInfo.Invoke(this, actionTokenSource.Token, null, tuples[i].value);
                            if (coroutine != null) StartCoroutine(coroutine);

                        }
                    }
                    invokeTagIndex++;
                }
                
                if (VisibleCount > TextMeshPro.textInfo.characterCount) break;

                RemoveUpdateFlags(TMP_VertexDataUpdateFlags.Colors32);
                DisplayCharacter();
                
                VisibleCount++;
            }

            IsTyping = false;
            IsHardSkipping = false;
            backUpIndices?.Clear(); // 清除所有需要复原的
            singleActions.Clear();  // 防止 additive 后执行没有触发的，所以提前干掉

            VisibleCount--;
            invokeTagIndex = TextMeshPro.text.Length;
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

    }

}
