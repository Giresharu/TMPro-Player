using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ATMPro {

    public class AnimateTMProRichTagManager : MonoBehaviour {

        static AnimateTMProRichTagManager instance;

        readonly Dictionary<string, ActionInfo> actionInfos = new Dictionary<string, ActionInfo>();

        public static bool Initialized { get { return instance != null && instance.initialized; } }

        bool initialized;

        void Awake() {
            if (instance == null) instance = this;
            else Destroy(this);
            DontDestroyOnLoad(this);
            Initialize();
            initialized = true;
        }

        protected virtual void Initialize() {
            actionInfos.Clear();

            // 包含闭合标签的范围性动作，是播放文字的开头触发的，所以使用协程
            SetActionInfo(args => StartCoroutine(Shake((AnimateTMProUGUI)args[0], (float)args[1], (float)args[2], (float)args[3], (List<(int, int)>)args[4])), "Shake", true, "shake", "Shake", "s", "S");
            SetActionInfo(args => StartCoroutine(Delay((AnimateTMProUGUI)args[0], (int)args[1], (List<(int, int)>)args[2])), "Delay", true, "delay", "Delay", "d", "D");

            SetActionInfo(args => Pause((AnimateTMProUGUI)args[0],(int)args[1]), "Pause", false, "pause", "Pause", "p", "P");
        }

        /*void SetActionInfo(Action<object[]> action, string methodName, bool needClosingTag, params string[] keys) {
            SetActionInfo(typeof(AnimateTMProRichTagManager), action, methodName, needClosingTag, keys);
        }

        void SetActionInfo(Func<object[], IEnumerator> func, string methodName, bool needClosingTag, params string[] keys) {
            SetActionInfo(typeof(AnimateTMProRichTagManager), func, methodName, needClosingTag, keys);
            测试中文
        }*/

        // ReSharper disable once MemberCanBePrivate.Global
        protected void SetActionInfo(Action<object[]> action, string methodName, bool needClosingTag, params string[] keys) {
            Type type = GetType();
            MethodInfo methodInfo = null;

            while (methodInfo == null) {
                methodInfo = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
                type = type?.BaseType;
            }

            ActionInfo actionInfo = new ActionInfo(action, methodInfo, needClosingTag);
            foreach (var key in keys) {
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
            foreach (var key in keys) {
                instance.actionInfos[key] = actionInfo;
            }
        }

        /*protected void SetActionInfo(Type type, Action<object[]> action, string methodName, bool needClosingTag, params string[] keys) {
            ActionInfo actionInfo = new ActionInfo(action, type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic), needClosingTag);
            foreach (var key in keys) {
                instance.actionInfos.Add(key, actionInfo);
            }
        }*/

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

        // ReSharper disable once MemberCanBePrivate.Global
        protected static List<int> IndicesInRange(TMP_TextInfo textInfo, List<(int start, int end)> ranges) {
            List<int> indexInRange = new List<int>();

            if (ranges == null) return indexInRange;

            indexInRange.Clear();

            for (int i = 0; i < textInfo.characterCount; i++) {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                foreach (var tuple in ranges) {
                    if (characterInfo.index < tuple.start || characterInfo.index >= tuple.end) continue;

                    indexInRange.Add(i);
                    break;
                }
            }
            return indexInRange;

        }

        /* --- Action Region --- */
    #region Action Region
        static IEnumerator Pause(AnimateTMProUGUI atmp, int time = 500) {
            float startTime = Time.time;
            while ((Time.time - startTime) * 1000 < time && !atmp.actionTokenSource.IsCancellationRequested) {
                yield return null;
            }
            Debug.Log("???");
            /*yield return new WaitForSeconds(time * 0.001f);*/
        }

        static IEnumerator Delay(AnimateTMProUGUI atmp, int time = 50, List<(int start, int end)> ranges = null) {

            /*List<int> indexInRange = new List<int>();

            TMP_TextInfo textInfo = atmp.tmp.textInfo;
            for (int i = 0; i < textInfo.characterCount; i++) {
                if (ranges == null) {
                    break;
                }

                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                foreach ((int start, int end) range in ranges) {
                    if (characterInfo.index < range.start || characterInfo.index >= range.end) continue;

                    indexInRange.Add(i);
                    break;
                }
            }*/

            if (!atmp.typeWriter) yield break;

            atmp.textMeshPro.maxVisibleCharacters = 0; // 默认是9999
            List<int> indexInRange = IndicesInRange(atmp.textMeshPro.textInfo, ranges);

            int index = 0;
            while (!atmp.actionTokenSource.IsCancellationRequested && index < atmp.textMeshPro.textInfo.characterCount) {
                if (!atmp.isActiveAndEnabled) {
                    yield return null;

                    continue;
                }

                index = atmp.textMeshPro.maxVisibleCharacters;

                // Debug.Log(index + " : " + atmp.textMeshPro.textInfo.characterInfo[index].character);
                atmp.delay = indexInRange.Contains(index) ? time : atmp.defaultDelay;

                yield return new WaitForSeconds(atmp.delay * 0.001f);
            }

        }



        static IEnumerator Shake(AnimateTMProUGUI atmp, float frequency = 15f, float amplitude = 1f, float phaseshift = 10f, List<(int start, int end)> ranges = null) {

            TMP_TextInfo textInfo = atmp.textMeshPro.textInfo;
            TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            float startTime = Time.time;
            float angle = Random.value * 2f * Mathf.PI; //把 0 ~ 1 的随机数映射到 0 ~ 2PI 的弧度

            /*List<int> indexInRange = new List<int>();

            for (int i = 0; i < textInfo.characterCount; i++) {
                if (ranges == null) {
                    break;
                }

                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                foreach ((int start, int end) range in ranges) {
                    if (characterInfo.index < range.start || characterInfo.index >= range.end) continue;

                    indexInRange.Add(i);
                    break;
                }
            }*/

            List<int> indexInRange = IndicesInRange(textInfo, ranges);

            while (!atmp.actionTokenSource.IsCancellationRequested) {

                if (atmp.hasTextChanged) {
                    cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
                    atmp.hasTextChanged = false;
                }

                // 当 disable 时暂停本协程
                if (!atmp.isActiveAndEnabled || textInfo.characterCount == 0) {
                    yield return null;

                    continue;
                }


                // for (int i = 0; i < textInfo.characterCount; i++) {
                foreach (int i in indexInRange) {

                    if (ranges == null) {
                        break;
                    }

                    TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                    if (!characterInfo.isVisible) continue;

                    Vector3 direction = new Vector3(Mathf.Cos(angle + i * phaseshift), Mathf.Sin(angle + i * phaseshift), 0f);
                    float theta = (Time.time - startTime) * frequency * 2f * Mathf.PI; // 时间 * 频率 = 进度， 进度再映射到弧度，用作正弦函数的自变量

                    float distance = Mathf.Sin(theta) * amplitude;

                    Vector3 offset = direction * distance;

                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    Vector3[] srcVertices = cachedMeshInfo[materialIndex].vertices;

                    // if (srcVertices == null) break;

                    Vector3[] dstVertices = textInfo.meshInfo[materialIndex].vertices;

                    dstVertices[vertexIndex] = srcVertices[vertexIndex]         + offset;
                    dstVertices[vertexIndex + 1] = srcVertices[vertexIndex + 1] + offset;
                    dstVertices[vertexIndex + 2] = srcVertices[vertexIndex + 2] + offset;
                    dstVertices[vertexIndex + 3] = srcVertices[vertexIndex + 3] + offset;

                }

                atmp.textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

                // 每次完成一个频次就重新生成一个弧度
                if (Time.time - startTime > 1f / frequency) {
                    angle = Random.value * 2f * Mathf.PI;
                    startTime = Time.time;
                }

                yield return null;
            }
            Debug.Log("真的取消了！");
        }
    #endregion

    }

    public class ActionInfo {
        readonly Type[] argTypes;
        readonly object[] defaultArgValues;
        internal bool IsPaired { get; }

        readonly bool needAnimateTMPro;
        readonly Action<object[]> action;
        readonly Func<object[], IEnumerator> func;

        public ActionInfo(Action<object[]> action, MethodInfo methodInfo, bool isPaired) {
            this.action = action;
            IsPaired = isPaired;
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            argTypes = new Type[parameterInfos.Length];
            defaultArgValues = new object[parameterInfos.Length];

            for (var i = 0; i < parameterInfos.Length; i++) {
                argTypes[i] = parameterInfos[i].ParameterType;
                defaultArgValues[i] = parameterInfos[i].HasDefaultValue ? parameterInfos[i].DefaultValue : null;
                if (i == 0)
                    needAnimateTMPro = argTypes[0] == typeof(AnimateTMProUGUI);
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
                if (i == 0)
                    needAnimateTMPro = argTypes[0] == typeof(AnimateTMProUGUI);
            }

        }

        IEnumerator Invoke(params string[] argStrings) {
            object[] args = new object[argTypes.Length];
            for (int i = 0; i < argTypes.Length; i++) {
                // 转换标签填写时提供的参数
                if (i < argStrings.Length) {
                    //TODO 改成手动分辨基本类型
                    args[i] = Convert.ChangeType(argStrings[i], argTypes[i]);
                } else {
                    // 没有填写的参数使用函数的默认值
                    args[i] = defaultArgValues[i];
                }
            }

            if (action == null) return func.Invoke(args);

            action.Invoke(args);
            return null;

        }


        internal IEnumerator Invoke(AnimateTMProUGUI atmp, params string[] argStrings) {
            if (!needAnimateTMPro)
                return Invoke(argStrings);

            object[] args = new object[argTypes.Length];
            args[0] = atmp;
            for (var i = 1; i < argTypes.Length; i++) {
                if (i < argStrings.Length + 1) {
                    //TODO 改成手动分辨基本类型
                    args[i] = Convert.ChangeType(argStrings[i - 1], argTypes[i]);
                } else {
                    args[i] = defaultArgValues[i];
                }
            }

            if (action == null) return func.Invoke(args);

            action.Invoke(args);
            return null;
        }

        internal IEnumerator Invoke(AnimateTMProUGUI atmp, List<(int, int)> range, params string[] argStrings) {
            if (!needAnimateTMPro)
                return Invoke(argStrings);

            object[] args = new object[argTypes.Length];
            args[0] = atmp;
            for (var i = 1; i < argTypes.Length; i++) {
                if (i < argStrings.Length + 1) {
                    //TODO 改成手动分辨基本类型
                    args[i] = Convert.ChangeType(argStrings[i - 1], argTypes[i]);
                } else {
                    args[i] = defaultArgValues[i];
                }
            }
            args[^1] = range;

            if (action == null) return func.Invoke(args);

            action.Invoke(args);
            return null;
        }
    }
    struct RichTagInfo {

        internal string type;
        internal int startIndex;
        internal int endIndex;
        internal string[] value;

    }

}
