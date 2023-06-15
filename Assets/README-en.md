![LOGO](./logo.png)
===
[![Releases](https://img.shields.io/github/v/release/Giresharu/TMPro-Player.svg)](https://github.com/Giresharu/TMPro-Player/releases/latest) [![openupm](https://img.shields.io/npm/v/com.gsr.tmproplayer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.gsr.tmproplayer/) [![LICENSE](https://img.shields.io/github/license/Giresharu/TMPro-Player)](./LICENSE.md) ![Unity](https://img.shields.io/badge/Unity%20Supported-2020.1%2B-ff69b4.svg)

[查看中文版](./README.md)

TMPro Player is a rich text tag management plugin based on TextMeshPro, enabling custom rich text tag functionality.

Features of TMPro Player include:

* Built-in typewriter effect, allowing control over the pause and speed of the typewriter effect using tags;
* Tag parsing functionality, where users simply need to manually add tag definitions and implement the desired effects for the tags；
* Compatibility with TextMeshPro's built-in tags, prioritizing user-defined tags when they overlap with the built-in ones.

Change Log
---
Click to view [CHANGELOG](./CHANGELOG-en.md)

## Table of Contents

- [Dependency](#dependency)
- [Installation](#installation)
  - [Install via OpenUPM](#install-via-openupm)
  - [Install via git URL](#install-via-git-url)
  - [Installing Examples](#installing-examples)
- [Usage](#usage)
- [Tag Execution Rules](#tag-execution-rules)
- [Custom Tags](#custom-tags)
- [TMProPlayer Class](#tmproplayer-class)
  - [SetText](#settext)
  - [Skip](#skip)
  - [SoftSkip](#softskip)
  - [SetSoftSkip](#setsoftskip)
  - [AddUpdateFlags](#addupdateflags)
  - [RemoveUpdateFlags](#removeupdateflags)
  - [CheckUpdateFlags](#checkupdateflags)
- [TMPPlayerRichTagManager Class](#tmpplayerrichtagmanager-class)
  - [SetActionInfo](#setactioninfo)
    - [Parameters for Action/Func](#parameters-for-actionfunc)
    - [Writing Pair Tags](#writing-pair-tags)
  - [Initialize](#initialize)
  - [IndicesInRange](#indicesinrange)
  - [IndicesInRangeHashSet](#indicesinrangehashset)
  - [IndicesInRangeDictionary](#indicesinrangedictionary)
- [License](#License)


Dependency
---
This tool relies on [com.unity.textmeshpro@3.0.6](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) or higher versions. It does not currently support preview versions 4.0 and above.

Installation
---

### Install via OpenUPM

In your project's root directory, use the following command:

```shell
# Install the latest version
openupm add com.gsr.tmproplayer
# Install a specific version
openupm add com.gsr.tmproplayer@1.1.2
```
If you are using a modified version of `com.unity.textmeshpro` and it is not recognized as a dependency, please use the following command to install:

```shell
openupm add com.gsr.tmproplayer -f
```

### Install via git URL

In the Unity engine, open the `Package Manager` and click on the `+` button in the top left corner. Choose `Add package from git URL` and enter the git URL of this project:

```shell
https://github.com/Giresharu/TMPro-Player.git?path=Assets#1.1.2
```
It is recommended to specify the version number when installing via Git URL to avoid automatically updating to the latest repository instead of the latest version. This ensures that you install a stable repository.

### Installing Examples

After installing the plugin, you can find the example package `Examples.unitypackage` in the path `Packages/com.gsr.tmproplayer/Example/`. The examples showcase the functionalities of "Text Shake," "Text Wave," and "Text Fade-in Appear."

Usage
---

1. Add a [TMPPlayerRichTagManager](#tmpplayerrichtagmanager-class) singleton to your scene;
2. Add a [TMProPlayer](#tmproplayer-class) component to a GameObject that has a TextMeshPro component;
3. Fill in the content directly in the `text` field of TextMeshPro in the Inspector, or use the [`SetText`](#settext) method of type [TMProPlayer](#tmproplayer-class) to add content.

Tag Execution Rules
---

* Tags are divided into paired tags and single tags;
* Paired tags, such as `<tag>` `</tag>` , have both opening and closing tags. They are executed in the order they appear during text initialization and are processed in the background through coroutines, for processing the text within the tagged range;
* Single tags consist of only `<tag>` . They are called just before the character to the right of the tag is displayed in the typewriter effect, within the same frame. If there are multiple tags to the left of the same character, they will be called in the order they appear;
* If a single tag returns an IEnumerator type, it will cause the typewriter effect to be blocked, allowing for pauses in the typewriter effect using this rule.

Custom Tags
---

To create custom tags, you'll need to create a derived class of [TMPPlayerRichTagManager](#tmpplayerrichtagmanager-class) and use it as a singleton in your scene.

To register custom tags, you need to override the `Initialize` method of the derived class and register the tags within it:

```cs
protected override void Initialize() {
    base.Initialize(); // If you don't need the default tags, you can omit calling the base class's Initialize
    SetActionInfo(args => ExampleTag((string)args[0]), "ExampleTag", false, "ExampleTag", "et");
}

static void ExampleTag(string value = "?"){
    Debug.log(value);
}
```
Here, [SetActionInfo](#setactioninfo) is the method for registering tags. For more detailed information, please refer to the description of [SetActionInfo](#setactioninfo).

TMProPlayer Class
---

The TMProPlayer class is a component that can be attached to a GameObject that has a TextMeshPro (UGUI) or TextMeshPro component. It is used to implement typewriter effects and handle the parsing and execution of tags.

| Property/Field   | Type | Description |
|--------------|-----|-----------|
| **Delay**   | int | The interval between each character in the typewriter effect (in milliseconds). |
| **TextMeshPro**   | TMP_Text | Returns the TextMeshPro component attached to the GameObject (read-only). |
| **CurrentChar**   | TMP_CharacterInfo | Returns information about the last character that has been outputted in the typewriter effect (read-only). |
| **LastChar**   | TMP_CharacterInfo | Returns information about the previous character that has been outputted in the typewriter effect (read-only). |
| **NextChar**   | TMP_CharacterInfo | Returns information about the next character that is yet to be outputted in the typewriter effect (read-only). |
| **IsTyping**   | bool | Indicates whether the typewriter coroutine is currently running (read-only). |
| **IsSkipping**   | bool | Indicates whether the skipping (including soft skipping) is currently in progress (read-only). |
| **IsHardSkipping**   | bool | Indicates whether the hard skipping is currently in progress (read-only). |
| **IsSoftSkipping**   | bool | Indicates whether the soft skipping is currently in progress (read-only). |
| **VisibleCount**   | int | The number of characters currently visible (including escape characters and invisible characters, but excluding parsed tags) (read-only). |
| **isTypeWriter**   | bool | Determines whether the typewriter effect is enabled. Changes take effect on the next SetText call. |
| **openStyle**   | string | The text automatically added before each SetText call. It can contain tags for conveniently setting effects that won't be changed frequently. |
| **closeStyle**   | string | The text automatically added after each SetText call. It serves the same purpose as openStyle. |
| **defaultDelay**   | int | The default delay between characters in the typewriter effect (in milliseconds). |
| **timeScale**   | float | The time scale applied to the typewriter effect, used for fast-forwarding. It can also be used to adjust the time scale of tag actions according to personal preference. |


### SetText

```cs
 public void SetText(string text, bool isAdditive = false, bool newline = false)
```

This method replaces the `SetText` method in TextMeshPro and is used to set the text content that you want to be parsed and displayed. It is automatically called during Start to parse and display the `text` field of TextMeshPro.

| **Parameter** | **Type** | **Description** |
|--------------|-----|-----------|
| **text**   | string | 	The original text to be parsed.  |
| **isAdditive**   | bool   | Whether to update the text incrementally. If set to true, the displayed characters and effects won't be cleared, and the new text will be added at the end. |
| **newLine**   | bool   | 	Whether to start a new line. |


### Skip
```cs
public void Skip(bool invokeSingleActions = true) 
```

Skips the current typewriter effect and displays all the text immediately.

| **Parameter** | **Type** | **Description** |
|--------------|-----|-----------|
| **invokeSingleActions**   | bool | Determines whether to invoke single actions within the skipped text. |

### SoftSkip

```cs
public void SoftSkip() 
```

Skips the text until a `Func`  type tag is triggered (such as the built-in `<pause>` tag that pauses the typewriter effect), and then stops skipping and resumes the typewriter effect. 

### SetSoftSkip
```cs
public void SetSoftSkip(bool value)
```

Enables or disables continuous soft skipping. If enabled, it will continue to soft skip until manually disabled.


| **Parameter** | **Type** | **Description** |
|--------------|-----|-----------|
| **value**   | bool | Enables or disables soft skipping. |

### AddUpdateFlags

```cs
public void AddUpdateFlags(TMP_VertexDataUpdateFlags updateFlag)
```

Adds the flag for the TextMeshPro mesh information that needs to be updated in the current frame, such as vertex positions, vertex colors, UVs, etc. The added flags will be updated using `TextMeshPro.UpdateVertexData` during `LateUpdate`. This is used for creating text mesh animation-related events.

| **Parameter** | **Type** | **Description** |
|--------------|-----|-----------|
| **updateFlag**  | TMP_VertexDataUpdateFlags | The enum value representing the TMP mesh information to be updated. |

### RemoveUpdateFlags

```cs
public void RemoveUpdateFlags(TMP_VertexDataUpdateFlags updateFlag)
```
Removes the flag for the specified mesh information that needs to be updated in the current frame.

### CheckUpdateFlags

```cs
public bool CheckUpdateFlags(TMP_VertexDataUpdateFlags updateFlags)
```

Checks whether the specified mesh information has been marked for update in the current frame.

TMPPlayerRichTagManager Class
---

The TMPPlayerRichTagManager class is a singleton required for parsing tags in  [TMProPlayer](#tmproplayer-class). When the TMPPlayerRichTagManager class exists in the scene, [TMProPlayer](#tmproplayer-class) can use it to parse and execute tags.

You can inherit from this class to customize tags.

### SetActionInfo

```cs
protected void SetActionInfo(Action<object[]> action, string methodName, bool needClosingTag, params string[] keys)
protected void SetActionInfo(Func<object[], IEnumerator> func, string methodName, bool needClosingTag, params string[] keys)
```

Registers a tag in the manager and binds it to the action to be executed.

| **Parameter** | **Type** | **Description** |
|--------------|-----|-----------|
|**action**  | Action<object[]> | The Action to be executed when the tag is triggered. |
|**func**    | Func<object[], IEnumerator> | The Func to be executed when the tag is triggered. The return value should be an IEnumerator. This is used to directly execute coroutine-related tags within the typewriter coroutine, without starting a separate coroutine. |
|**methodName**| string   | The name of the method called in the Action/Func. It is used for reflection to query the default values of its parameters, allowing users to omit default parameters when writing tags. |
| **needClosingTag**       | bool   | Whether the tag requires a closing tag. Tags with closing tags are immediately executed after parsing, and provide the range of the tag as an argument to the event. Tags without closing tags are executed when the typewriter effect reaches them. |
| **keys** |string[]| The keywords of the tags written by the user in the text. The same action/func can have multiple different keywords. When there is a keyword conflict, the later registered one will overwrite the earlier one. |

#### Parameters for Action/Func

The parameters used by the delegate type are provided by [TMProPlayer](#tmproplayer-class) when it is called. [TMProPlayer](#tmproplayer-class) first parses and provides the arguments of the tag (as `object` type), and then converts and provides them to the method to be executed in the `Action` / `Func` in the order of their indices.

```cs
SetActionInfo(args => ExampleTag((string)args[0], (int)args[1], (float)args[2]), "ExampleTag", false, "ExampleTag", "et");
// This allows you to parse the <et=str,10,22.2> tag and execute the corresponding delegate.
```

[TMProPlayer](#tmproplayer-class) also provides the following additional parameters that are not part of the tags. You can insert them in any order in the method's parameters to access them:

| **Type** | **Description** |
|-----|-----------|
| TMProPlayer | The [TMProPlayer](#tmproplayer-class) instance executing the tag. |
| CancellationToken | The actionTokenSource of [TMProPlayer](#tmproplayer-class), which can be used to cancel it when there is a non-additive update or during destruction. |
| List<(int, int)> | A list of pairs of tag ranges, where each tuple represents the start and end indices of a tag. |

For example:

```cs
SetActionInfo(args => ExampleTag((TMProPlayer)args[0], (string)args[1], (int)args[2], (float)args[3] ,(CancellationToken)args[4], (List<(int,int)>)args[5]), "ExampleTag", true, "ExampleTag", "et");
// This allows you to provide these non-tag-parameters to the ExampleTag method.
```
However, pair tags are typically not written directly in this way because they usually involve loops, such as some text animation effects.

#### Writing Pair Tags

As mentioned above, pair tags usually involve loops, so we execute them during text initialization and end them when the text is destroyed or during non-additive updates.

We use a combination of coroutines and CancellationToken to achieve this, assuming you are using coroutines (if you choose to use Unitask or other asynchronous solutions, it's also possible, but here we'll only describe the coroutine approach):

```cs
// Use StartCoroutine to call methods of IEnumerator type
SetActionInfo(args => StartCoroutine(ExampleTag((TMProPlayer)args[0], (string)args[1], (int)args[2], (float)args[3] ,(CancellationToken)args[4], (List<(int,int)>)args[5])), "ExampleTag", true, "ExampleTag", "et");

static IEnumerator ExampleTag(TMProPlayer tmpp, string value1, int value2, float value3, CancellationToken token, List<(int start, int end)> ranges){
    while(!token.IsCancellationRequested){
      // Process the text within the specified ranges
    }
}
```

### Initialize
```cs
protected override void Initialize()
```
The Initialize method is automatically called during `Awake`. You can call [SetActionInfo](#setactioninfo)in this method. When inheriting from [TMPPlayerRichTagManager](#tmpplayerrichtagmanager-class)  and you want to preserve the base class's implementation of tags, include the following code:

```cs
base.Initialize();
```

This calls the initialization method of the base class.

### IndicesInRange

```cs
protected static List<int> IndicesInRange(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

Returns a list of indices of characters within the specified range in the `characterInfo` of the parsed text. In simple terms, it converts the range information provided by the tags into a collection of indices that satisfy the range conditions. By default, the range is left-inclusive and right-exclusive since tag indices are based on the right-side characters.

The `characterInfo` refers to the information of the text after the built-in tags of `TextMeshPro` have been parsed. The parsing and customization of tags occur before this stage, so if the text contains built-in `TextMeshPro` tags, it can cause the indices of the custom tags to be misaligned. Calling this method can obtain the final collection of indices.

| **Parameter** | **Type** | **Description** |
|--------------|-----|-----------|
|**textInfo**  | TMP_TextInfo | The `textInfo` property of the TextMeshPro, which contains various information about the text. |
|**ranges**    | List<(int start, int end)> | The ranges of the tags. Multiple ranges can be parsed at once using a list of tuples. |
|**isLeftOpen**| bool | Is the range left-open? Default is `false` |
|**isRightOpen**| bool | Is the range right-open? Default is `true` |

### IndicesInRangeHashSet

```cs
protected static HashSet<int> IndicesInRangeHashSet(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

Returns a HashSet of indices of characters within the specified range in the `characterInfo` of the parsed text.

### IndicesInRangeDictionary

```cs
protected static Dictionary<int, T> IndicesInRangeDictionary<T>(TMP_TextInfo textInfo, List<(int start, int end)> ranges, bool isLeftOpen = false, bool isRightOpen = true)
```

Returns a Dictionary of indices of characters within the specified range in the `characterInfo` of the parsed text. The value type T can be any type used to store the state or other information of a `characterInfo`. For example, using the bool type to indicate whether a certain effect has been triggered for the current character.

License
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
