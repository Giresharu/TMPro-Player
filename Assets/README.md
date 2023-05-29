HomoAddressable
===
[![Releases](https://img.shields.io/github/release/Giresharu/HomoAddressable.svg)](https://github.com/Giresharu/HomoAddressable/releases)

一个更人性化的 Addressables 资源读取解决方案。人的学名前缀便是 Homo ，本着以人为本的思想创建了这个工具，所以给它起名叫做 HomoAddressable。（大嘘）

* 自动管理 LoadAsset 所生成的 AsyncOperationHandle ，在读取资源的 GameObject 实例销毁时自动释放；
* 使用 Unitask 作为异步的方式，让读取资源后的回调更加简单；

## 更新日志

查看 [CHANGELOG](./CHANGELOG.md)

## 目录

- [依赖](#依赖)
- [安装](#安装)
  - [通过 OpenUPM 安装](#通过-openupm-安装)
  - [通过 git URL 安装](#通过-git-url-安装)
  - [通过 unitypackage 文件安装](#通过-unitypackage-文件安装)
- [HomoAddressableTools 命名空间](#homoaddressabletools-命名空间)
- [HomoBehaviour 类与 IOnDestroyAssetLoad 接口](#homobehaviour-类与-iondestroyassetload-接口)
  - [HomoBehaviour](#homobehaviour)
  - [IOnDestroyAssetLoad](#iondestroyassetload)
  - [关于非 MonoBehaviour 类](#关于非-monobehaviour-类)
- [HomoAddressable.LoadAssetAsync](#homoaddressableloadassetasync)
- [HomoAddressable.LoadAsset](#homoaddressableloadasset)
- [HomoAddressable.LoadAssetHandle](#homoaddressableloadassethandle)
- [HomoAddressable.InstantiateAsync](#homoaddressableinstantiateasync)
- [HomoAddressable.LoadSceneAsync](#homoaddressableloadsceneasync)
- [Debug.Log 提示与设置](#DebugLog-提示与设置)
- [许可证](#许可证)


依赖
---
本工具依赖于 [com.unity.addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.19/manual/AddressableAssetsGettingStarted.html) 与 [com.cysharp.unitask](https://github.com/Cysharp/UniTask) 运行，请确保项目已安装这两个插件。

安装
---
### 通过 OpenUPM 安装

在项目根目录使用命令：
```
openupm add com.gsr.homo_addressable
```

### 通过 git URL 安装

你也可以通过在 Unity 引擎中，打开 `Package Manager` ，点击左上角的 `+` ，选择 `Add package from git URL` ，然后输入本项目的 git 地址即可： 
`https://github.com/Giresharu/HomoAddressable.git?path=Assets` 。

### 通过 unitypackage 文件安装

通过 [Releases](https://github.com/Giresharu/HomoAddressable/releases) 页面下载 unitypackage 文件并安装。此安装方法会造成项目资源目录不整洁，故不推荐。

HomoAddressableTools 命名空间
---
本插件所提供的所有类与接口都位于 HomoAddressableTools 命名空间下，请注意。

HomoBehaviour 类与 IOnDestroyAssetLoad 接口
---

### HomoBehaviour

通常我们会在一个 MonoBehaviour 类中读取并使用资源。所以我们可以在 MonoBehaviour 类销毁时，释放它所加载的所有资源的 AsyncOperationHandle 。而 HomoBehaviour 类正是实现了这个功能的派生类。

由于我们在 HomoBehaviour 中对 OnDestroy 进行了实现，若派生类也需要实现 OnDestroy 时，请在 OnDestroy 时调用 base.OnDestroy ：
```cs
new void OnDestroy() {
	base.OnDestroy();
}
```
若要克隆 HomoBehaviour 实例，请使用其中的 Clone() 函数代替 GameObject.Instantiate() 进行克隆，这样才能保证引用的资源时安全的。

### IOnDestroyAssetLoad

我建议所有需要读取资源的 MonoBehaviour 类都改成使用 HomoBehaviour ，但如果你的项目中已经有着许多继承于 MonoBehaviour 且不方便修改的类，你可以选择手动实现 IOnDestroyAssetLoad 接口来达成等同于 HomoBehaviour 的效果：
```cs
using System.Collections.Generic;
using HomoAddressableTools;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

// 如果需要实现克隆功能请取消除词句以外的所有注释，如若不需要保持原样即刻。
public class Test : MonoBehaviour, IOnDestroyAssetLoad {

	public event OnDestroyDelegate OnDestroyEvent;
	public Dictionary<string, AsyncOperationHandle<Object>> Handles { get; set; }
	// RefCount refCount = new RefCount();

	protected void OnDestroy() {
		OnDestroyInvoke(); 
	}

	void OnDestroyInvoke() {
		// refCount.Count--;
		// if (refCount.Count <= 0)
		OnDestroyEvent?.Invoke(this);
	}

/*
	public GameObject Clone(Transform parent = null) {
		refCount.Count++;
		var obj = Instantiate(gameObject, parent);
		obj.GetComponent<HomoBehaviour>().refCount = refCount;
		return obj;
	}
*/
}
```

### 关于非 MonoBehaviour 类

非 MonoBehaviour 类不存在 OnDestroy ；而 Addressables.Release 函数在析构函数中不产生作用。对此我的建议是：
* 如果非 MonoBehaviour 类是一个需要读取资源的数据结构，请让使用此数据结构的 MonoBehaviour 类来承当绑定资源与释放的作用；
* 如果该类是一个需要读取资源并使用的静态类，则建议使用原生 Addressables 来进行手动加载与释放。

HomoAddressable.LoadAssetAsync<T>
---

```cs
async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, string path, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default);

async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default);
```
* 其中 `bindObj` 为资源所绑定的实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 实例，当我们销毁这个实例时，这次加载的资源也会自动被释放；
* `path` 即为资源在 Addressables 中的路径，通常来说，只要你不去修改，它默认就是资源在项目中的路径，可以方便地搭配诸如 Odin 等插件进行方便地选择；
* 也可以不用路径而是用重载的 AssetReference 来读取资源；
* `progress` 可以使用原生的 Progress 类；
* `timing` 表示异步操作在主线程循环的哪一步执行，具体可以参考 UniTask 的文档；
* `failedCallback` 是读取失败时的回调， `timeoutCallback` 是读取超时的回调，而 `canceledCallback` 则是读取被取消时的回调；
* `millisecondsTimeout` 是设置超时时间，单位为毫秒。当其为0时，表示不限时。
* `token` 则是取消信号，供我们从外部取消。无论我们是否填写，当绑定的 MonoBehaviour 实例被销毁时，为了防止内存泄漏，都会取消当前的读取。

是用这个函数读取资源时，如果该 MonoBehaviour 实例已经读取过同一资源，则会跳过异步任务直接获取结果。虽然 Addressables 本身就有防止重复读取的优化，但是我们连 Addressables 的过程都跳过了，实乃省中之省。

```cs
TextAsset data;
float data_progress;
	
async void Load() {
	data = await HomoAddressable.LoadAssetAsync<TextAsset>(this, "Assets/Data/data.csv",new Progress<float>(progress =>data_progress = progress),failedCallback: FailedCallback);
}

void FailedCallback(){
	Debug.log("Failed Callback.");
}
```
使用扩展方法的形式：
```cs
data = await this.LoadAssetAsync<TextAsset>("Assets/Data/data.csv",new Progress<float>(progress =>data_progress = progress),failedCallback: FailedCallback);
```

HomoAddressable.LoadAsset<T>
---
同步版本的 LoadAssetAsync<T>，由于是同步的，它不像异步版本拥有那么多参数：
```cs
T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, string path, Action failedCallback = nul);
T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference, Action failedCallback = nul);
```

大多异步加载函数都大同小异有同步的版本，不多赘述，可以自己 F12 观看。

HomoAddressable.LoadAssetHandle<T>
---

我们还提供了原本的 Addressables.LoadAssetAsync<T> 的封装，原汁原味，供希望读取资源的静态函数看起来整整齐齐的强迫症使用。
```cs
AsyncOperationHandle<T> LoadAssetHandle<T>(string path);

AsyncOperationHandle<T> LoadAssetHandle<T>(AssetReference reference);
```

使用 LoadAssetHandle<T> 的话，不会帮助你管理返回的 handle ，请你自行处理。


HomoAddressable.InstantiateAsync
---
加载一个预制体所依赖的资源，并实例化之。

```cs
async UniTask<GameObject> InstantiateAsync(string path, Transform parent = null, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default);

async UniTask<GameObject> InstantiateAsync(AssetReference reference, Transform parent = null, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default);
```

* 大部分参数都如同 LoadAssetAsync ，`parent` 则是实例化时设置的父母结点。
* 实例化的 GameObject 存在于场景树上，所以并没有绑定 HomoBehaviour 实例。所以无法同 Asset 那样从实例中的 handle 去查找已经读取的资源，避免重复创建 handle 。我建议配合对象池来使用。

使用 InstantiateAsync 实例化资源时，如你所见，并不需要绑定一个 HomoBehaviour 实例。如果要释放这个预制体实例及其依赖的资源时，请使用如下函数：
```cs
void ReleaseInstance(this GameObject obj);
```

HomoAddressable.LoadSceneAsync
---
加载一个场景实例，还可以将其生成到场景树上。

```cs
async UniTask<SceneInstance> LoadSceneAsync(string path, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default);

async UniTask<SceneInstance> LoadSceneAsync(AssetReference reference, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default);
```

* `mode` 参数为 `LoadSceneMode.Single` 时，当你生成场景实例，将会先卸载现有的资源。效果就像通常的切换场景；
* `mode` 参数若为 `LoadSceneMode.Additive` ，则不会卸载现有资源，而是用增量的方式生成；
* `activeOnLoad` 为 `true` ，则加载后就生成场景实例，否则仅加载，需要生动生成；
* 使用 AssetReference 读取时，无法读取已经读取过的场景，但是使用 path 是可以重复读取的。虽然不明白底层为何，但是我也不推荐重复去尝试生成 handle 来读取。若要多次生成到场景树上，请将返回的 SceneInstance 保存下来使用。

卸载并释放从 Addressables 加载的场景的函数为：

```cs
async UniTaskVoid ReleaseScene(this SceneInstance sceneInstance, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update);
```

该函数是异步的，但不提供取消信号。开弓没有回头箭，卸载了就别想整回来。

Debug.Log 提示与设置
---
本项目的 Debug.Log 提示仅在 Unity 编辑器中，或是 Development_Build 模式打包的项目中进行处理。
你可以在项目的 `Assets/Resources` 目录下点击右键 `Create/Addressables/HomoAddressableConfig` 来创建设置。该配置文件在 `Resources` 目录下存在多个时，仅会以其中一个为准（具体是哪个我也说不准）。你可以在该配置中更改各项 Debug 的警报级别，或者是干脆关闭。
当你在没有创建该设置的情况下尝试读取资源，将会自动创建该配置至 `Resources` 目录。

许可证
---

本项目使用 MIT 协议

