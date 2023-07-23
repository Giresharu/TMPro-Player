TMPro-Player
===
[View in English](./CHANGELOG-en.md)

[1.1.5](https://github.com/Giresharu/TMPro-Player/releases/1.1.5)
---

### 修复
* 修复了正则不匹配省略靠前参数的问题等匹配问题
* 修复了执行标签时因省略前置参数而报错的问题

[1.1.4](https://github.com/Giresharu/TMPro-Player/releases/1.1.4)
---

### 修复
* 修复了在 unity 2020.x 版本中的各种不兼容问题

[1.1.3](https://github.com/Giresharu/TMPro-Player/releases/1.1.3)
---

### 修复
* 修复了嵌套或交错的成对标签被错误优化合并的问题

[1.1.2](https://github.com/Giresharu/TMPro-Player/releases/1.1.2)
---

### 修复
* 犯了个低级错误，依赖多打了一个字符，改好了。

[1.1.1](https://github.com/Giresharu/TMPro-Player/releases/1.1.1)
---

### 特性
* 增加组件图标。

### 修改
* 去除没啥用的 EditorWindow 脚本。

[1.1.0](https://github.com/Giresharu/TMPro-Player/releases/1.1.0)
---

### 特性

* 增加软跳过 `SoftSkip` 方法以及设置持续软跳 `SetSoftSkip` 方法。与 `Skip` 不同，软跳过只会跳到下一个执行 Func 类型委托的标签；
* 给 TMProPlayer 类型添加 `timeScale` 字段，用于方便快进。

### 修复

* 修复 `Skip` 不执行标签的问题；
* 修复打字机调用 Action 时返回 null 导致延迟一帧的问题；
* 优化 `Delay` 标签的实现；
* 修复 `Delay` 为 0 时老不更新文字的bug；
* 修复 TMProPlayer 实例被销毁时不会正确 Dispose 内部的 CancellationTokenSource 的问题；
* 修复更新 `NextChar` 时超出索引的问题。


[1.0.2](https://github.com/Giresharu/TMPro-Player/releases/1.0.2) | [1.0.0](https://github.com/Giresharu/TMPro-Player/releases/1.0.0)
---

### 已知Bug

* 当包含 TMProPlayer 且正在执行打字机效果的 GameObject 被设为非 Active 状态后，再恢复时将会直接显示所有文字。但打字机效果实际上还在继续。（可能因为是 TextMeshPro 在检测到 Enable 时会重设网格为初始状态） 



