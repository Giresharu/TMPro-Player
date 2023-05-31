using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace TMPPlayer {
    internal class ActionInfo {
        readonly Type[] argTypes;
        readonly object[] defaultArgValues;
        internal bool IsPaired { get; }

        // readonly bool needAnimateTMPro;
        readonly Action<object[]> action;
        readonly Func<object[], IEnumerator> func;

        internal ActionInfo(Action<object[]> action, MethodInfo methodInfo, bool isPaired) {
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
        internal ActionInfo(Func<object[], IEnumerator> func, MethodInfo methodInfo, bool isPaired) {
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
    
}
