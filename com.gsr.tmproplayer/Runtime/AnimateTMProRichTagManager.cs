using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;

namespace TMPP {

    public class AnimateTMProRichTagManager : MonoBehaviour {

        static AnimateTMProRichTagManager instance;

        readonly Dictionary<string, ActionInfo> actionInfos = new Dictionary<string, ActionInfo>();

        void Awake() {
            if (instance == null) instance = this;
            else Destroy(this);
            DontDestroyOnLoad(this);
            Initialize();
            // initialized = true;
        }

        protected virtual void Initialize() {
            actionInfos.Clear();

            // 包含闭合标签的范围性动作，是播放文字的开头触发的，所以使用协程
            SetActionInfo(args => StartCoroutine(Delay((TMProPlayer)args[0], (int)args[1], (List<(int, int)>)args[2], (CancellationToken)args[3])), "Delay", true, "delay", "Delay", "d", "D");

            SetActionInfo(args => Pause((int)args[0], (CancellationToken)args[1]), "Pause", false, "pause", "Pause", "p", "P");
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected void SetActionInfo(Action<object[]> action, string methodName, bool needClosingTag, params string[] keys) {
            Type type = GetType();
            MethodInfo methodInfo = null;

            while (methodInfo == null) {
                methodInfo = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
                type = type?.BaseType;
            }

            ActionInfo actionInfo = new ActionInfo(action, methodInfo, needClosingTag);
            foreach (string key in keys) {
                instance.actionInfos[key] = actionInfo;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected void SetActionInfo(Func<object[], IEnumerator> func, string methodName, bool needClosingTag, params string[] keys) {
            Type type = GetType();
            MethodInfo methodInfo = null;

            while (methodInfo == null) {
                methodInfo = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
                type = type?.BaseType;
            }

            ActionInfo actionInfo = new ActionInfo(func, methodInfo, needClosingTag);
            foreach (string key in keys) {
                instance.actionInfos[key] = actionInfo;
            }
        }

        internal static ActionInfo GetActionInfo(string key) {
            return instance.actionInfos[key];
        }

        internal static bool TryGetActionInfo(string key, out ActionInfo actionInfo) {
            if (instance.actionInfos.ContainsKey(key)) {
                actionInfo = instance.actionInfos[key];
                return true;
            }
            actionInfo = null;
            return false;
        }

        internal bool ContainsRichTag(string key) {
            return actionInfos.ContainsKey(key);
        }

        /// <summary>
        /// 以 List 的形式返回索引范围内的文字在 characterInfo 中的索引
        /// </summary>
        /// <param name="textInfo">用于解析的 textInfo</param>
        /// <param name="ranges">范围</param>
        /// <param name="isLeftOpen">左开区间</param>
        /// <param name="isRightOpen">右开区间</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        protected static List<int> IndicesInRange(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true) {
            List<int> indexInRange = new List<int>();

            if (ranges == null) return indexInRange;

            // indexInRange.Clear();

            for (int i = 0; i < textInfo.characterCount; i++) {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                foreach (var tuple in ranges) {
                    int start = tuple.start;
                    int end = tuple.end;

                    if (!isLeftOpen) start--;
                    if (!isRightOpen) end++;

                    if (characterInfo.index > start && characterInfo.index < end) {
                        indexInRange.Add(i);
                        break;
                    }

                }
            }
            return indexInRange;
        }

        /// <summary>
        /// 以 HashSet 的形式返回索引范围内的文字在 characterInfo 中的索引
        /// </summary>
        /// <param name="textInfo">用于解析的 textInfo</param>
        /// <param name="ranges">范围</param>
        /// <param name="isLeftOpen">左开区间</param>
        /// <param name="isRightOpen">右开区间</param>
        /// <returns></returns>
        protected static HashSet<int> IndicesInRangeHashSet(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true) {
            HashSet<int> indexInRange = new HashSet<int>();

            if (ranges == null) return indexInRange;

            for (int i = 0; i < textInfo.characterCount; i++) {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                foreach (var tuple in ranges) {
                    int start = tuple.start;
                    int end = tuple.end;

                    if (!isLeftOpen) start--;
                    if (!isRightOpen) end++;

                    if (characterInfo.index > start && characterInfo.index < end) {
                        indexInRange.Add(i);
                        break;
                    }

                }
            }
            return indexInRange;
        }

        /// <summary>
        /// 以 Dictionary 的形式返回索引范围内的文字在 characterInfo 中的索引
        /// </summary>
        /// <param name="textInfo">用于解析的 textInfo</param>
        /// <param name="ranges">范围</param>
        /// <param name="isLeftOpen">左开区间</param>
        /// <param name="isRightOpen">右开区间</param>
        /// <returns></returns>
        protected static Dictionary<int, T> IndicesInRangeDictionary<T>(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true) {
            Dictionary<int, T> indexInRange = new Dictionary<int, T>();

            if (ranges == null) return indexInRange;

            // indexInRange.Clear();

            for (int i = 0; i < textInfo.characterCount; i++) {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                foreach (var tuple in ranges) {
                    int start = tuple.start;
                    int end = tuple.end;

                    if (!isLeftOpen) start--;
                    if (!isRightOpen) end++;

                    if (characterInfo.index > start && characterInfo.index < end) {
                        indexInRange.Add(i, default(T));
                        break;
                    }

                }
            }
            return indexInRange;
        }
        
    #region Action Region
        static IEnumerator Pause(int time = 500, CancellationToken token = default) {
            float startTime = Time.time;
            while ((Time.time - startTime) * 1000 < time && !token.IsCancellationRequested) {
                yield return null;
            }
        }

        readonly List<int> changedIndex = new List<int>();
        static IEnumerator Delay(TMProPlayer tmpp, int time = 50, List<(int start, int end)> ranges = null, CancellationToken token = default) {

            if (!tmpp.isTypeWriter) yield break;
            
            List<int> indexInRange = IndicesInRange(tmpp.TextMeshPro.textInfo, ranges, false, false);

            int charaIndex = 0;
            while (!token.IsCancellationRequested && charaIndex < tmpp.TextMeshPro.textInfo.characterCount) {
                if (!tmpp.isActiveAndEnabled) {
                    yield return null;
                    continue;
                }

                charaIndex = tmpp.VisibleCount;

                if (indexInRange.Contains(charaIndex)) {
                    tmpp.Delay = time;
                    instance.changedIndex.Add(charaIndex);
                } else if (!instance.changedIndex.Contains(charaIndex)) // 防止把其他范围的delay覆盖了
                    tmpp.Delay = tmpp.defaultDelay;

                yield return Pause(tmpp.Delay);

            }

        }
    #endregion

    }

    public class ActionInfo {
        readonly Type[] argTypes;
        readonly object[] defaultArgValues;
        internal bool IsPaired { get; }

        // readonly bool needAnimateTMPro;
        readonly Action<object[]> action;
        readonly Func<object[], IEnumerator> func;

        public ActionInfo(Action<object[]> action, MethodInfo methodInfo, bool isPaired) {
            this.action = action;
            IsPaired = isPaired;
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            argTypes = new Type[parameterInfos.Length];
            defaultArgValues = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++) {
                argTypes[i] = parameterInfos[i].ParameterType;
                defaultArgValues[i] = parameterInfos[i].HasDefaultValue ? parameterInfos[i].DefaultValue : null;
                /*if (i == 0)
                    needAnimateTMPro = argTypes[0] == typeof(TMProPlayer);*/
            }

        }

        // 需要返回协程的时候改用 Func 
        public ActionInfo(Func<object[], IEnumerator> func, MethodInfo methodInfo, bool isPaired) {
            this.func = func;
            IsPaired = isPaired;
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            argTypes = new Type[parameterInfos.Length];
            defaultArgValues = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++) {
                argTypes[i] = parameterInfos[i].ParameterType;
                defaultArgValues[i] = parameterInfos[i].HasDefaultValue ? parameterInfos[i].DefaultValue : null;
                /*if (i == 0)
                    needAnimateTMPro = argTypes[0] == typeof(TMProPlayer);*/
            }

        }

        internal IEnumerator Invoke(TMProPlayer tmpp, CancellationToken token, List<(int, int)> range, params string[] argStrings) {

            object[] args = new object[argTypes.Length];

            int offset = 0;
            for (int i = 0; i < argTypes.Length; i++) {

                if (argTypes[i] == typeof(TMProPlayer)) {
                    args[i] = tmpp;
                    offset++;
                } else if (argTypes[i] == typeof(CancellationToken)) {
                    args[i] = token;
                    offset++;
                } else if (argTypes[i] == typeof(List<(int, int)>)) {
                    args[i] = range;
                    offset++;
                } else if (i < argStrings.Length + offset) {
                    args[i] = Convert.ChangeType(argStrings[i - offset], argTypes[i]);
                } else {
                    args[i] = defaultArgValues[i];
                }
            }

            if (action == null) return func.Invoke(args);

            action.Invoke(args);
            return null;
        }
    }
    internal struct RichTagInfo {
        internal string type;
        internal int startIndex;
        internal int endIndex;
        internal string[] value;

    }

}
