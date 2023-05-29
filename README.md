TMPro Player
===
[![Releases](https://img.shields.io/github/v/release/Giresharu/TMPro-Player.svg)](https://github.com/Giresharu/TMPro-Player/releases/tag/1.0.0)

TMPro Player 是一款基于 TextMeshPro 的富文本标签管理插件，实现自定义富文本标签功能。

* 内置打字机效果，可以通过标签控制打字机效果的暂停与速度；
* 实现解析标签功能，用户只需要手动添加标签的定义以及标签要执行的效果的实现；
* 与 TextMeshPro 的内置标签兼容，当用户定义的标签与其相同时，则以用户定义为优先。

## 目录

- [依赖](#依赖)
- [安装](#安装)
  - [通过 git URL 安装](#通过-git-url-安装)
- [TMProPlayer 类](#tmproplayer-类)
  - [SetText](#settext)
  - [Skip](#skip)
- [TMPPlayerRichTagManager 类](#tmpplayerrichtagmanager-类)
  - [SetActionInfo](#setactioninfo)
  - [Initialize](#initialize)
- [许可证](#许可证)


依赖
---
本工具依赖于 [com.unity.textmeshpro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) 或者其他基于它的魔改版本。众所周知官方原版的 TextMeshPro 有着 GC 问题，所以许多用户会选择对其进行魔改。基于这个原因，我没有在本插件的 package.json 中强制要求对 TextMeshPro 的依赖，以便用户自己选择使用。

安装
---

### 通过 git URL 安装

在 Unity 引擎中，打开 `Package Manager` ，点击左上角的 `+` ，选择 `Add package from git URL` ，然后输入本项目的 git 地址即可： 
`https://github.com/Giresharu/TMPro-Player.git?path=Assets` 。


TMProPlayer 类
---

作为组件可以挂在拥有 TextMeshPro(UGUI) 的 GameObject 上。用于实现打字机效果以及对标签的解析与执行。

### SetText

```cs
 public void SetText(string text, bool isAdditive = false, bool newline = false)
```

代替 TextMeshPro 中的 SetText 函数，用于设置想要被解析并显示的文本内容。Start 时会自动调用 SetText 来解析显示 TextMeshPro 的 text 。

|    | ||
|--------------|-----|-----------|
| **text**   | string | 要被解析的原始文本                                  |
| **isAddtive**   | bool   | 是否增量更新文本，如果为 true ，则不会清除已经显示的文字和效果，直接在后面添加 |
| **newLine**   | bool   | 是否另起一行                                     |


### Skip
```cs
public void Skip()
```
跳过当前打字机效果，直接显示全部文字。

TMPPlayerRichTagManager 类
---

TMProPlayer 解析标签所需要的一个单例，当 TMPPlayerRichTagManager 类在场景上存在时， TMProPlayer 才能根据它来解析标签。

可以通过继承该类来自定义标签。

### SetActionInfo

```cs
protected void SetActionInfo(Action<object[]> action, string methodName, bool needClosingTag, params string[] keys)
protected void SetActionInfo(Func<object[], IEnumerator> func, string methodName, bool needClosingTag, params string[] keys)
```

在管理器里注册一个标签以及它所执行的事件。

|    | ||
|--------------|-----|-----------|
|**action**  | Action<object[]> | 触发标签时会执行的 Action                                  |
|**func**    | Func<object[], IEnumerator> | 触发标签时会执行的 Func，返回值为 IEnumerator 。 用于直在打字机协程中直接执行协程相关的标签，而不另起一个协程|                                |
|**methodName**| string   | Action/Func 中调用的函数的名称，用于反射查询其参数的默认值，以供用户填写标签时省略默认参数 |
| **needClosingTag**       | bool   | 是否需要闭合标签。拥有闭合标签的标签会在解析后立刻执行，并且会对事件提供标签的范围；而没有闭合标签的标签会在打字机效果执行到的时候执行  |
| **keys** |string[]|用户在文本里填写的标签关键字，同一个 Action/Func 可以有多个不同的关键字。当关键字冲突时，后注册的会覆盖先注册的|

### Initialize
```cs
protected override void Initialize()
```
初始化函数，会在 Awake 的时候自动调用。可以在此处调用 SetActionInfo 。
继承 TMPPlayerRichTagManager 时，若需要保留基类实现的标签，请写上下列代码：
```cs
base.Initialize();
```
以调用基类的初始化函数。


许可证
---

本项目使用 Apache 协议 

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