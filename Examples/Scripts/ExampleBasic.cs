using System;
using TMPPlayer;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ExampleBasic : MonoBehaviour {
    public TMProPlayer tmpp;
    [FormerlySerializedAs("next")]
    public Button nextButton;
    public RectTransform progressUI;

    void Start() {
        nextButton.onClick.AddListener(Continue);
        progressUI.localScale = new Vector3(0, 1, 1);
    }

    int progress;

    void Continue() {
        switch (progress) {
            case 0:
                tmpp.SetText("<align=\"center\">(二) 组件面板设置<p=500>\n\n</align><line-indent=20>在 Inspector 中 TMProPlayer 的组件面板设置中，<p=250>有一些简单的设置。<p=1000>\n");
                tmpp.SetText("isTypeWriter ：<p=250>bool 类型，<p=250>设置为 true 后，<p=250>播放的文字将会以打字机效果逐字播放。为 false 时则一口气全部显示出来。<p=500>改变它的值并不会影响当前正在进行的打字机效果。<p=1000>\n", true, true);
                tmpp.SetText("openStyle closeStyle ：<p=250>string 类型，<p=250>类似于 TextMeshPro 中的 TextStyle，<p=250>用于在文字收尾添加一些文字，<p=250>方便处理一些整段文字都需要的效果。<p=500>由于个人技术水平原因，<p=250>无法成功解析并改写 TextMeshPro 的 TextStyle 中的标签，<p=250>所以做出了类似功能的实现。<p=500>openStyle 与 closeStyle 分别为添加在每段文本最前与最后的文字。<p=500>对于增量更新的文本，<p=250>则会在增量的文本前后也添加上，<p=250>效果类似于 <open>文字<close><open>增量文字<close><open>增量文字2<close> 。<p=1000>\n", true, true);
                tmpp.SetText("defaultDelay ：<p=250>int 类型。<p=500>表示打字机效果中，<p=250>每个文字之间的默认间隔时间（毫秒）。值得一提的是，<p=250>默认的 < pause > 暂停效果与其是叠加的。\n", true, true);
                break;
            case 1:
                tmpp.SetText("<align=\"center\">(三) 使用标签<p=500>\n\n</align><line-indent=20>TMProPlayer 提供了一个自定义功能强大的不同于 TextMeshPro 内置富文本的标签系统，<p=250>我们可以用格式如 <tagName> 的标签来在文本播放时执行一些操作。<p=500>比如说暂停、<p=250>插入音效、<p=250>执行事件等等。<p=1000>\n");
                tmpp.SetText("标签分为两种格式，<p=250>第一种是单个标签。<p=500>单个标签所包含的操作将会在文字播放到的时候执行。<p=500>除了无参数，<p=250>或者表示默认参数的 <tagName> 的写法，<p=250>我们也可以根据标签所支持的参数来填写成例如 <tagName=value1,value2,value3> 的格式，<p=250>来更精细地执行操作。<p=500>参数的类型可以是任何 C# 中能够从字符串类型转换而来的所有基本类型，<p=250>如 int,<p=250>bool,<p=250>float,<p=250>string 等。<p=500>如果你希望后面的参数为默认，<p=250>而前面的参数手动填写，<p=250>也可以省略后面的部分，<p=250>写成 <tagName=value1> 。<p=500>我们也支持省略前面的部分参数，<p=250>但是你需要留下逗号来让程序知道你所填写的参数到底是哪一个参数： <tagName=,value2>。<p=500>另外逗号与等号周围也可以添加空格，<p=250>便于提高可读性，<p=250>这不会影响代码的执行。<p=500>TMProPlayer 内置的单个标签有 < pause >，<p=250>也可缩写为 < p > （去掉标签名前后空格，<p=250>后文不再赘述），<p=250>支持一个 float 类型参数，<p=250>它的作用是在播放文字的时候，<p=250>对播放动作执行时长等同于参数(毫秒)的暂停。<p=1000>\n", true, true);
                tmpp.SetText("另一种则是成对标签，<p=250>格式如 <tagName> </tagName> 这样分为（左）开合标签，<p=250>（右）闭合标签，<p=250>分别写在一段文字的之前与之后的标签。<p=500>成对标签主要用于实现一些针对范围内文字的效果。<p=500>在开合标签里同样可以填写参数，<p=250>具体与单个标签相同，<p=250>不再赘述。<p=500>成对标签的解析功能支持嵌套功能，<p=250>如 <tagA><tagB></tagB></tagA> ，<p=250>甚至是同名标签的嵌套 <A><A></tagA></tagA>。<p=500>我们还支持交叉标签，<p=250>如 <tagA><tagB></tagA></tagB> 。<p=500>内置的成对标签有 < Delay = value > （缩写 < d = value >），<p=250>参数类型为 float ，<p=250>它的作用是将范围内的文字播放后的延时设置为参数（毫秒），<p=250>以调整文字播放的速度。", true, true);
                nextButton.interactable = false;
                break;
        }
        progress++;
    }

    void Update() {
        float progressOfSection = (float)tmpp.VisibleCount / tmpp.TextMeshPro.textInfo.characterCount;
        // float sectionOfAll = (progress + 1f) / 3f;
        progressUI.localScale = new Vector3((progressOfSection + progress) / 3f, 1, 1);
    }

}
