jkljsdlkfjlksjdfklj lksdjflk sdf sdf 


TMPro Player 是一款基于 TextMeshPro 的富文本标签管理插件，实现自定义富文本标签功能。

* 内置打字机效果，可以通过标签控制打字机效果的暂停与速度；
* 实现解析标签功能，用户只需要手动添加标签的定义以及标签要执行的效果的实现；
* 与 TextMeshPro 的内置标签兼容，当用户定义的标签与其相同时，则以用户定义为优先。

更新日志
---
点击查看[CHANGELOG](./CHANGELOG.md)

## 目录

- [依赖](#依赖)
- [安装](#安装)
  - [通过 OpenUPM 安装](#通过-openupm-安装)
  - [通过 git URL 安装](#通过-git-url-安装)
- [TMProPlayer 类](#tmproplayer-类)
  - [SetText](#settext)
  - [Skip](#skip)
  - [AddUpdateFlags](#addupdateflags)
  - [RemoveUpdateFlags](#removeupdateflags)
  - [CheckUpdateFlags](#checkupdateflags)
- [TMPPlayerRichTagManager 类](#tmpplayerrichtagmanager-类)
  - [SetActionInfo](#setactioninfo)
  - [Initialize](#initialize)
- [许可证](#许可证)


依赖
---
本工具依赖于 [com.unity.textmeshpro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) 或者其他基于它的魔改版本。众所周知官方原版的 TextMeshPro 有着 GC 问题，所以许多用户会选择对其进行魔改。基于这个原因，我没有在本插件的 package.json 中强制要求对 TextMeshPro 的依赖，以便用户自己选择使用。

安装
---

### 通过 OpenUPM 安装

在项目根目录使用命令：

```shell
openupm add com.gsr.tmproplayer
```
若要指定版本，请在指令 package 名称后加上 \`@tag号\` ，如 \`@1.0.0\` 。

### 通过 git URL 安装

在 Unity 引擎中，打开 `Package Manager` ，点击左上角的 `+` ，选择 `Add package from git URL` ，然后输入本项目的 git 地址即可： 
`https://github.com/Giresharu/TMPro-Player.git?path=Assets` 。

若要指定版本，请在地址最后加上 \`@tag号\` ，如 \`#1.0.0\` 。 

TMProPlayer 类
---

作为组件可以挂在拥有 TextMeshPro(UGUI) 的 GameObject 上。用于实现打字机效果以及对标签的解析与执行。

| 属性 / 字段   | 类型 | 描述 |
|--------------|-----|-----------|
| **Delay**   | int | 打字机效果中当前每个文字的间隔时间（毫秒） |
| **TextMeshPro**   | TMP_Text | 返回 GameObject 上挂载的 TextMeshPro 类型的组件（只读） |
| **CurrentChar**   | TMP_CharacterInfo | 返回当前打字机效果已经输出的最后一个文字的信息（只读） |
| **LastChar**   | TMP_CharacterInfo | 返回当前打字机效果已经输出的上一个文字的信息（只读） |
| **NextChar**   | TMP_CharacterInfo | 返回当前打字机效果还未输出的下一个文字的信息（只读） |
| **IsTyping**   | bool | 当前是否正在进行打字机协程（只读） |
| **VisibleCount**   | int | 当前已显示的文字数量（包括转义字符等不可视字符，但不包括被解析过的标签）（只读）|
| **isTypeWriter**   | bool | 是否使用打字机效果，修改后要下次 SetText 才会生效 |
| **openStyle**   | string | 自动添加在每次 SetText 的文字之前的文字，可以填写标签，用于方便地设置一些长时间不会改用的效果 |
| **closeStyle**   | string | 自动添加在每次 SetText 的文字之后的文字，作用同上 |
| **defaultDelay**   | int | 打字机效果中，默认的文字间隔时间（毫秒） |


### SetText

```cs
 public void SetText(string text, bool isAdditive = false, bool newline = false)
```

代替 TextMeshPro 中的 SetText 方法，用于设置想要被解析并显示的文本内容。Start 时会自动调用 SetText 来解析显示 TextMeshPro 的 text 。

| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
| **text**   | string | 要被解析的原始文本                                  |
| **isAddtive**   | bool   | 是否增量更新文本，如果为 true ，则不会清除已经显示的文字和效果，直接在后面添加 |
| **newLine**   | bool   | 是否另起一行                                     |


### Skip
```cs
public void Skip()
```

跳过当前打字机效果，直接显示全部文字。

### AddUpdateFlags

```cs
public void AddUpdateFlags(TMP_VertexDataUpdateFlags updateFlag)
```

添加本帧需要更新渲染的 TextMeshPro 网格的信息，如：顶点位置、定点颜色、uv等，被添加的 flag 会在 LateUpdate 时通过 TextMeshPro.UpdateVertexData 统一更新。
用于制作文字网格动画相关的事件。

| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
| **updateFlag**  | TMP_VertexDataUpdateFlags | 表示需要更新的 TMP 网格信息的枚举类型 |

### RemoveUpdateFlags

```cs
public void RemoveUpdateFlags(TMP_VertexDataUpdateFlags updateFlag)
```
移除本帧需要更新渲染的某个网格信息。

### CheckUpdateFlags

```cs
public bool CheckUpdateFlags(TMP_VertexDataUpdateFlags updateFlags)
```

检查某个网格信息是否已经被标记为本帧需要更新。

TMPPlayerRichTagManager 类
---

TMProPlayer 解析标签所需要的一个单例，当 TMPPlayerRichTagManager 类在场景上存在时， TMProPlayer 才能根据它来解析标签。

可以通过继承该类来自定义标签。

### SetActionInfo

```cs
protected void SetActionInfo(Action<object[]> action, string methodName, bool needClosingTag, params string[] keys)
protected void SetActionInfo(Func<object[], IEnumerator> func, string methodName, bool needClosingTag, params string[] keys)
```

在管理器里注册一个标签，并绑定需要执行的事件。

| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
|**action**  | Action<object[]> | 触发标签时会执行的 Action |
|**func**    | Func<object[], IEnumerator> | 触发标签时会执行的 Func，返回值为 IEnumerator 。 用于直在打字机协程中直接执行协程相关的标签，而不另起一个协程 |
|**methodName**| string   | Action/Func 中调用的方法的名称，用于反射查询其参数的默认值，以供用户填写标签时省略默认参数 |
| **needClosingTag**       | bool   | 是否需要闭合标签。拥有闭合标签的标签会在解析后立刻执行，并且会对事件提供标签的范围；而没有闭合标签的标签会在打字机效果执行到的时候执行  |
| **keys** |string[]|用户在文本里填写的标签关键字，同一个 Action/Func 可以有多个不同的关键字。当关键字冲突时，后注册的会覆盖先注册的|

### Initialize
```cs
protected override void Initialize()
```
初始化方法，会在 Awake 的时候自动调用。可以在此处调用 SetActionInfo 。
继承 TMPPlayerRichTagManager 时，若需要保留基类实现的标签，请写上下列代码：
```cs
base.Initialize();
```
以调用基类的初始化方法。

### IndicesInRange

```cs
protected static List<int> IndicesInRange(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

以 List 的形式返回索引范围内的文字在 characterInfo 中的索引集合。通俗的解释就是把标签提供的范围信息转换为能够满足该范围条件的所有文字。由于标签的索引以右侧文字为准，所以默认为左闭右开区间。

characterInfo 即被 TextMeshPro 解析内置标签后的文字信息，而我们解析并自定义标签的信息是在其之前，所以一旦文本中内包含 TextMeshPro 的内置标签，就会导致自定义标签所对应的索引错位。调用这个方法能够获得最终的索引的集合。

| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
|**textInfo**  | TMP_TextInfo | TextMeshPro 类型的 textInfo 属性，包含了文本的一系列信息 |
|**ranges**    | List<(int start, int end)> | 标签的范围，可以一次解析多个范围，所以以元组类型的 List 的形式存在 |
|**isLeftOpen**| bool | range 所代表的区间是否左开？默认为 false |
|**isRightOpen**| bool | range 所代表的区间是否右开？默认为 true |

### IndicesInRangeHashSet

```cs
protected static HashSet<int> IndicesInRangeHashSet(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

以 HashSet 的形式返回索引范围内的文字在 characterInfo 中的索引集合。

### IndicesInRangeDictionary

```cs
protected static Dictionary<int, T> IndicesInRangeDictionary<T>(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

以 Dictionary 的形式返回索引范围内的文字在 characterInfo 中的索引集合。value 的类型 T 可以是任意类型，用于记录某个 characterInfo 的状态或着其他信息。比如使用 bool 类型来表示当前文字是否已经触发过某个效果。

许可证
---
Apache License 2.0

Copyright 2023 Giresharu

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
