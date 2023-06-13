TMPro-Player
===
[查看中文版本](./CHANGELOG.md)

[1.1.0](https://github.com/Giresharu/TMPro-Player/releases/1.1.0)
---

### Feature

* Added [`SoftSkip`](README-en.md/#softskip) method for soft skipping and [`SetSoftSkip`](README-en.md/#setsoftskip) method for setting continuous soft skipping. Unlike `Skip`, soft skipping jumps only to the next tag that executes a Func-type delegate.
* Added `timeScale` field to TMProPlayer class for convenient fast-forwarding.

### Fixed

* Fixed issue where `Skip` was not executing tags.
* Fixed delay of one frame when invoking Action in typewriter effect due to null return.
* Optimized implementation of `Delay` tag.
* Fixed bug where text was not updating when `Delay` was set to 0.
* Fixed issue where the TMProPlayer instance was not correctly disposing the internal CancellationTokenSource when destroyed.
* Fixed issue of going beyond the index when updating `NextChar`.


[1.0.2](https://github.com/Giresharu/TMPro-Player/releases/1.0.2) | [1.0.0](https://github.com/Giresharu/TMPro-Player/releases/1.0.0)
---

### Known Bug

* When a GameObject containing TMProPlayer with an ongoing typewriter effect coroutine is set to a non-active state and then reactivated, all text will be displayed instantly. However, the typewriter effect is still ongoing. (Possibly because TextMeshPro resets the mesh to its initial state upon Enable.)


