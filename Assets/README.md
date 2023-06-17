![LOGO](./logo.png)
===
[![Releases](https://img.shields.io/github/v/release/Giresharu/TMPro-Player.svg)](https://github.com/Giresharu/TMPro-Player/releases/latest) [![openupm](https://img.shields.io/npm/v/com.gsr.tmproplayer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.gsr.tmproplayer/) [![LICENSE](https://img.shields.io/github/license/Giresharu/TMPro-Player)](./LICENSE.md) ![Unity](https://img.shields.io/badge/Unity%20Supported-2020.1%2B-ff69b4.svg)

[View in English](./README-en.md)

TMPro Player 是一款基于 TextMeshPro 的富文本标签管理插件，实现自定义富文本标签功能。

* 内置打字机效果，可以通过标签控制打字机效果的暂停与速度；
* 实现解析标签功能，用户只需要手动添加标签的定义以及标签要执行的效果的实现；
* 与 TextMeshPro 的内置标签兼容，当用户定义的标签与其相同时，则以用户定义为优先。

更新日志
---
点击查看 [CHANGELOG](./CHANGELOG.md)

## 目录

- [依赖](#依赖)
- [安装](#安装)
  - [通过 OpenUPM 安装](#通过-openupm-安装)
  - [通过 git URL 安装](#通过-git-url-安装)
  - [安装范例](#安装范例)
- [用法](#用法)
- [标签的执行规则](#标签的执行规则)
- [自定义标签](#自定义标签)
- [TMProPlayer 类](#tmproplayer-类)
  - [SetText](#settext)
  - [Skip](#skip)
  - [SoftSkip](#softskip)
  - [SetSoftSkip](#setsoftskip)
  - [AddUpdateFlags](#addupdateflags)
  - [RemoveUpdateFlags](#removeupdateflags)
  - [CheckUpdateFlags](#checkupdateflags)
- [TMPPlayerRichTagManager 类](#tmpplayerrichtagmanager-类)
  - [SetActionInfo](#setactioninfo)
    - [Action/Func 可用参数](#actionfunc-可用参数)
    - [成对标签的写法](#成对标签的写法)
  - [Initialize](#initialize)
  - [IndicesInRange](#indicesinrange)
  - [IndicesInRangeHashSet](#indicesinrangehashset)
  - [IndicesInRangeDictionary](#indicesinrangedictionary)
- [许可证](#许可证)


依赖
---
本工具依赖于 [com.unity.textmeshpro@3.0.6](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) 以上版本。暂不支持 4.0 以上预览版本。

安装
---

### 通过 OpenUPM 安装

在项目根目录使用命令：

```shell
# 安装最新版本
openupm add com.gsr.tmproplayer
# 安装指定版本
openupm add com.gsr.tmproplayer@1.1.3
```
如果你使用了魔改的 `com.unity.textmeshpro` ，并且无法正确被识别为依赖时，请使用如下命令安装：

```shell
openupm add com.gsr.tmproplayer -f
```

### 通过 git URL 安装

在 Unity 引擎中，打开 `Package Manager` ，点击左上角的 `+` ，选择 `Add package from git URL` ，然后输入本项目的 git 地址即可： 

```shell
https://github.com/Giresharu/TMPro-Player.git?path=Assets#1.1.3
```
建议使用 git URL 安装时指定版本号，否则会默认更新为最新仓库，而非最新版本。有可能会安装不稳定的仓库。

### 安装范例

安装完插件后，可在 `Packages/com.gsr.tmproplayer/Example/` 目录下找到范例 `Examples.unitypackage` 。
范例中包含了`文字振动`、`文字波浪`以及`文字淡入`功能的演示。

用法
---

1. 在场景上添加一个 [TMPPlayerRichTagManager](#tmpplayerrichtagmanager-类) 单例；
2. 在拥有 TextMeshPro 组件的 GameObject 上添加 [TMProPlayer](#tmproplayer-类) 组件；
3. 直接在 Inspector 中 TextMeshPro 的 `text` 字段里填写内容，或者使用 [TMProPlayer](#tmproplayer-类) 类型的 [`SetText`](#settext) 方法来添加内容。

标签的执行规则
---

* 标签分为成对标签以及单个标签；
* 类似 `<tag>` `</tag>` 这样有头有尾的标签就是成对标签，他们会在文字初始化时就按照先后顺序执行，并通过协程在后台循环，处理范围内的文字；
* 只有 `<tag>` 一个的即为单个标签，他们将会在打字机效果中，右侧的文字被显示之前（同一帧内）调用，同一个文字左侧有多个标签时，将会按照先后顺序依次调用；
* 如果单个标签返回了一个 IEnumerator 类型，则会导致打字机效果被阻塞，通过这个规则可以实现打字机效果的暂停。

自定义标签
---

要自定义标签，就需要编写 [TMPPlayerRichTagManager](#tmpplayerrichtagmanager-类) 的派生类，以此代替之放在场景上作单例。

要注册标签，我们需要在派生类覆写 [TMPPlayerRichTagManager](#tmpplayerrichtagmanager-类) 的 `Initialize` 方法，并在其注册标签：

```cs
protected override void Initialize() {
    base.Initialize(); // 如果你不需要默认的标签，可以不调用基类的 Initialize
    SetActionInfo(args => ExampleTag((string)args[0]), "ExampleTag", false, "ExampleTag", "et");
}

static void ExampleTag(string value = "?"){
    Debug.log(value);
}
```
其中 [SetActionInfo](#setactioninfo) 便是注册标签的方法。更详细的说明请参考 [SetActionInfo](#setactioninfo) 的介绍。

TMProPlayer 类
---

作为组件可以挂在拥有 TextMeshPro(UGUI) 或 TextMeshPro 的 GameObject 上。用于实现打字机效果以及对标签的解析与执行。

| 属性 / 字段   | 类型 | 描述 |
|--------------|-----|-----------|
| **Delay**   | int | 打字机效果中当前每个文字的间隔时间（毫秒） |
| **TextMeshPro**   | TMP_Text | 返回 GameObject 上挂载的 TextMeshPro 类型的组件（只读） |
| **CurrentChar**   | TMP_CharacterInfo | 返回当前打字机效果已经输出的最后一个文字的信息（只读） |
| **LastChar**   | TMP_CharacterInfo | 返回当前打字机效果已经输出的上一个文字的信息（只读） |
| **NextChar**   | TMP_CharacterInfo | 返回当前打字机效果还未输出的下一个文字的信息（只读） |
| **IsTyping**   | bool | 当前是否正在进行打字机协程（只读） |
| **IsSkipping**   | bool | 当前是否正在进行跳过（包括软跳）（只读） |
| **IsHardSkipping**   | bool | 当前是否正在进行跳过（只读） |
| **IsSoftSkipping**   | bool | 当前是否正在进行软跳（只读） |
| **VisibleCount**   | int | 当前已显示的文字数量（包括转义字符等不可视字符，但不包括被解析过的标签）（只读）|
| **isTypeWriter**   | bool | 是否使用打字机效果，修改后要下次 SetText 才会生效 |
| **openStyle**   | string | 自动添加在每次 SetText 的文字之前的文字，可以填写标签，用于方便地设置一些长时间不会改用的效果 |
| **closeStyle**   | string | 自动添加在每次 SetText 的文字之后的文字，作用同上 |
| **defaultDelay**   | int | 打字机效果中，默认的文字间隔时间（毫秒） |
| **timeScale**   | float | 对打字机效果的时间缩放，用于快进。也可凭个人喜好用作标签动作的时间缩放 |


### SetText

```cs
 public void SetText(string text, bool isAdditive = false, bool newline = false)
```

代替 TextMeshPro 中的 `SetText` 方法，用于设置想要被解析并显示的文本内容。Start 时会自动调用 `SetText` 来解析显示 TextMeshPro 的 `text` 。

| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
| **text**   | string | 要被解析的原始文本                                  |
| **isAdditive**   | bool   | 是否增量更新文本，如果为 true ，则不会清除已经显示的文字和效果，直接在后面添加 |
| **newLine**   | bool   | 是否另起一行                                     |


### Skip
```cs
public void Skip(bool invokeSingleActions = true) 
```

跳过当前打字机效果，直接显示全部文字。

| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
| **invokeSingleActions**   | bool | 是否执行被跳过的文字之中的单个标签 |

### SoftSkip

```cs
public void SoftSkip() 
```

跳过文字，直到触发了 `Func` 类型的标签（如内置的 `<pause>` 标签这类会对打字机效果启用暂停的标签），之后结束跳过，恢复打字机效果。该效果别名软跳。

### SetSoftSkip
```cs
public void SetSoftSkip(bool value)
```

设置持续软跳的开关。如果开启，则会一直软跳，直到手动关闭。


| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
| **value**   | bool | 开启/关闭软跳 |

### AddUpdateFlags

```cs
public void AddUpdateFlags(TMP_VertexDataUpdateFlags updateFlag)
```

添加本帧需要更新渲染的 TextMeshPro 网格的信息，如：顶点位置、定点颜色、uv等，被添加的 flag 会在 `LateUpdate` 时通过 `TextMeshPro.UpdateVertexData` 统一更新。
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

[TMProPlayer](#tmproplayer-类) 解析标签所需要的单例，当 TMPPlayerRichTagManager 类在场景上存在时， [TMProPlayer](#tmproplayer-类) 才能根据它来解析标签。

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

#### Action/Func 可用参数

委托类型所用参数，是在 [TMProPlayer](#tmproplayer-类) 中被调用时，由 [TMProPlayer](#tmproplayer-类) 提供的。首先 [TMProPlayer](#tmproplayer-类) 解析并提供了标签的参数（以 `object` 类型），并按照索引顺序并转换类型提供给 `Action` / `Func` 要执行的方法。

```cs
SetActionInfo(args => ExampleTag((string)args[0], (int)args[1], (float)args[2]), "ExampleTag", false, "ExampleTag", "et");
// 这样就可以解析 <et=str,10,22.2> 的标签并执行对应的委托
```

[TMProPlayer](#tmproplayer-类) 还提供了以下非标签参数的参数，你可以按任意顺序插入在方法的形参中，以便形参调用：

| **类型** | **描述** |
|-----|-----------|
| TMProPlayer | 执行标签的 [TMProPlayer](#tmproplayer-类) 自身 |
| CancellationToken | [TMProPlayer](#tmproplayer-类) 的 actionTokenSource ，会在非增量更新以及销毁的时候取消 |
| List<(int, int)> | 成对标签的范围列表，列表内每个元组分别为起始和结束的标签所在索引 |

例如：

```cs
SetActionInfo(args => ExampleTag((TMProPlayer)args[0], (string)args[1], (int)args[2], (float)args[3] ,(CancellationToken)args[4], (List<(int,int)>)args[5]), "ExampleTag", true, "ExampleTag", "et");
// 这样就可以提供这些非标签参数给 ExampleTag 方法
```
不过成对标签通常不会直接这么写，因为它们通常牵涉到循环，如一些文字动画效果。

#### 成对标签的写法

如上所述，成对标签通常牵涉到循环，所以我们会在文本初始化时就执行他们，而通过 CancellationToken 在文本被销毁或非增量更新的时候结束它们。并结合标签的索引来实现效果。

那么我们就应该使用协程来实现成对标签（如果你选择使用 Unitask 或其他异步方案，也是可以的，这里只描述协程的做法）：

```cs
// 改成用 StartCoroutine 来调用 IEnumerator 类型的方法
SetActionInfo(args => StartCoroutine(ExampleTag((TMProPlayer)args[0], (string)args[1], (int)args[2], (float)args[3] ,(CancellationToken)args[4], (List<(int,int)>)args[5])), "ExampleTag", true, "ExampleTag", "et");

static IEnumerator ExampleTag(TMProPlayer tmpp, string value1, int value2, float value3, CancellationToken token, List<(int start, int end)> ranges){
    while(!token.IsCancellationRequested){
      // 在其中对范围内的文字进行处理
    }
}
```

### Initialize
```cs
protected override void Initialize()
```
初始化方法，会在 `Awake` 的时候自动调用。可以在此处调用 [SetActionInfo](#setactioninfo) 。
继承 [TMPPlayerRichTagManager](#tmpplayerrichtagmanager-类) 时，若需要保留基类实现的标签，请写上下列代码：
```cs
base.Initialize();
```
以调用基类的初始化方法。

### IndicesInRange

```cs
protected static List<int> IndicesInRange(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

以 List 的形式返回索引范围内的文字在 `characterInfo` 中的索引集合。通俗的解释就是把标签提供的范围信息转换为能够满足该范围条件的所有文字。由于标签的索引以右侧文字为准，所以默认为左闭右开区间。

`characterInfo` 即被 `TextMeshPro` 解析内置标签后的文字信息，而我们解析并自定义标签的信息是在其之前，所以一旦文本中内包含 `TextMeshPro` 的内置标签，就会导致自定义标签所对应的索引错位。调用这个方法能够获得最终的索引的集合。

| **参数** | **类型** | **描述** |
|--------------|-----|-----------|
|**textInfo**  | TMP_TextInfo | TextMeshPro 类型的 `textInfo` 属性，包含了文本的一系列信息 |
|**ranges**    | List<(int start, int end)> | 标签的范围，可以一次解析多个范围，所以以元组类型的 List 的形式存在 |
|**isLeftOpen**| bool | range 所代表的区间是否左开？默认为 `false` |
|**isRightOpen**| bool | range 所代表的区间是否右开？默认为 `true` |

### IndicesInRangeHashSet

```cs
protected static HashSet<int> IndicesInRangeHashSet(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

以 HashSet 的形式返回索引范围内的文字在 `characterInfo` 中的索引集合。

### IndicesInRangeDictionary

```cs
protected static Dictionary<int, T> IndicesInRangeDictionary<T>(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

以 Dictionary 的形式返回索引范围内的文字在 `characterInfo` 中的索引集合。value 的类型 T 可以是任意类型，用于记录某个 `characterInfo` 的状态或着其他信息。比如使用 bool 类型来表示当前文字是否已经触发过某个效果。

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
