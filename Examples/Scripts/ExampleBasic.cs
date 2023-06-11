using TMPPlayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExampleBasic : MonoBehaviour {
    public TMProPlayer tmpp;
    public Button nextButton, skipButton, softSkipButton;
    public Slider slider;
    TextMeshProUGUI softSkipText;
    public RectTransform progressUI;

    void Start() {
        nextButton.onClick.AddListener(Continue);
        skipButton.onClick.AddListener(Skip);
        softSkipButton.onClick.AddListener(SoftSkip);
        softSkipText = softSkipButton.GetComponentInChildren<TextMeshProUGUI>();
        slider.onValueChanged.AddListener(ChangeSpeed);

        progressUI.localScale = new Vector3(0, 1, 1);
    }

    int progress;

    void Continue() {
        // if (softSkipOn) SoftSkip();
        switch (progress) {
            case 0:
                tmpp.SetText("<align=\"center\">(二) 组件面板设置<p=500>\n\n</align><line-indent=20>在 Inspector 中 TMProPlayer 的组件面板设置中，<p=250>有一些简单的设置。<p=1000>\n");
                tmpp.SetText("<b>isTypeWriter</b><indent=150>bool 类型，<p=250>设置为 true 后，<p=250>播放的文字将会以打字机效果逐字播放。为 false 时则一口气全部显示出来。<p=500>改变它的值并不会影响当前正在进行的打字机效果。<p=1000></indent>\n", true, true);
                tmpp.SetText("<b>openStyle\ncloseStyle</b><indent=150><p=250>string 类型，<p=250>类似于 TextMeshPro 中的 TextStyle，<p=250>用于在文字收尾添加一些文字，<p=250>方便处理一些整段文字都需要的效果。<p=500>由于个人技术水平原因，<p=250>无法成功解析并改写 TextMeshPro 的 TextStyle 中的标签，<p=250>所以另外实现了类似功能的实现。<p=500>openStyle 与 closeStyle 分别为添加在每段文本最前与最后的文字。<p=500>对于增量更新的文本，<p=250>则会在增量的文本前后也添加上。<p=1000></indent>\n", true, true);
                tmpp.SetText("<b>defaultDelay</b><indent=150><p=250>int 类型,<p=250>表示打字机效果中，<p=250>每个文字之间的默认间隔时间（毫秒）</indent><p=1000>。\n", true, true);
                tmpp.SetText("<b>timeScale</b><indent=150><p=250>float 类型,<p=250>表示打字机效果的时间缩放。<p=500>自定义标签时也可以根据你的喜好引用其来调整事件的速度。<p=1000></indent>\n", true, true);
                tmpp.SetText("\n<align=right>点击下一节查看标签的介绍", true, true);
                break;
            case 1:
                tmpp.SetText("<align=\"center\">(三) 使用标签<p=500>\n\n</align><line-indent=20>TMProPlayer 提供了一个自定义功能强大的不同于 TextMeshPro 内置富文本的标签系统，<p=250>我们可以用常见的 <tagName> 格式的标签来在文本播放时执行一些操作。<p=500>比如说暂停、<p=250>插入音效、<p=250>执行事件等等。<p=1000>\n");
                tmpp.SetText("<b>单一标签</b><indent=100><p=250>由单个标签构成的标签，<p=250>标签将会在其右侧的文字被打字机效果输出前（同一帧）执行。<p=500>没有启用打字机效果时不会执行单一标签。<p=500>跳过（包括软跳）则会一口气按先后顺序触发范围内的所有标签，<p=250>此时如果要用标签播放音效应当注意爆音问题，<p=250>使用一些音频技术或者管理器来规避。<p=1000></indent>\n", true, true);
                tmpp.SetText("<b>成对标签</b><indent=100><p=250>由两个标签构成的标签组，<p=250>格式如 <tagName>text</tagName> ，<p=200>左为开标签，<p=250>右为闭标签。<p=500>由于成对标签通常是用来表示某个范围的文字获得的效果，<p=250>它们将会统一在显示文字之前按照开标签的先后顺序执行，<p=250>并在内部判断文字范围再实现效果。<p=500>所以无论是否启用打字机效果，<p=250>都将执行成对标签。<p=1000></indent>\n", true, true);
                tmpp.SetText("<b>标签缩写</b><indent=100><p=250>标签可以有缩写/别名，<p=250>这取决于你的自定义。<p=500>若是要对成对标签使用缩写，<p=250>应当将开闭标签都写成同样的缩写，<p=250>否则不会被解析成为一对。<p=1000></indent>\n", true, true);
                tmpp.SetText("\n<align=right>点击下一节查看剩余标签的介绍", true, true);

                break;
            case 2:
                tmpp.SetText("<b>标签参数</b><indent=100><p=250>标签可以有参数，<p=250>参数的类型可以是 C# 内置的可以从字符串解析而来的任何类型。<p=500>填写参数的格式如：<tagName=arg0,arg1,arg2> 。<p=250>如果标签支持，你也可以省略参数使用默认值。<p=500>省略部分靠后参数时，直接不填写即可： <tagName=arg0> 。<p=500>省略考前或者中间的参数时，<p=250>请预留逗号表示参数位置防止解析错误： <tagName=,,arg2>。<p=500>若要全部省略，直接填写标签不要写等号 = 即可。<p=500>逗号与等号的两侧可以添加空格增加可读性。<p=500>成对标签的闭标签请不要写参数<p=1000></indent>\n");
                tmpp.SetText("<b>标签嵌套</b><indent=100><p=250>如大部分富文本一样，<p=250>我们的标签也支持成对标签的互相嵌套。<p=500>如： <tagA><tagB>text</tagB></tagA> ，<p=250>也支持同名标签的嵌套： <tagA><tagA></tagA></tagA> ，<p=250>将会以中间为一对，<p=250>外面为一对的方式解析<p=1000></indent>\n", true, true);
                tmpp.SetText("<b>标签交错</b><indent=100><p=250>与其他富文本不同，<p=250>我们还支持不同的标签交错。<p=500>如： <tagA><tagB>text</tagA></tagB> 。<p=1000></indent>\n", true, true);
                tmpp.SetText("\n<align=right>内置标签请参考 ReadMe 文档\n更多自定义标签的范例请参考\nTMPro-Player\\Examples\\2.CustomRichTagExample.unity", true, true);

                nextButton.interactable = false;
                break;
        }
        progress++;
    }

    void ChangeSpeed(float value) {
        tmpp.timeScale = value != 0 ? value : 0.1f;
    }

    void Skip() {
        tmpp.Skip();
    }

    bool softSkipOn = false;
    void SoftSkip() {
        softSkipOn = !softSkipOn;

        if (softSkipOn) {
            softSkipButton.colors = new ColorBlock {
                                                       normalColor = new Color(0.27f, 0.72f, 1, 1),
                                                       highlightedColor = new Color(0.27f, 0.72f, 1, 1),
                                                       pressedColor = new Color(0.27f, 0.72f, 1, 1),
                                                       selectedColor = new Color(0.27f, 0.72f, 1, 1),
                                                       colorMultiplier = 1,
                                                       fadeDuration = 0.1f
                                                   };
            tmpp.SetSoftSkip(true);
            softSkipText.text = "软跳 ON";
        } else {
            softSkipButton.colors = ColorBlock.defaultColorBlock;
            tmpp.SetSoftSkip(false);
            softSkipText.text = "软跳 OFF";

        }
    }

    void Update() {
        float progressOfSection = (float)tmpp.VisibleCount / tmpp.TextMeshPro.textInfo.characterCount;
        // float sectionOfAll = (progress + 1f) / 3f;
        progressUI.localScale = new Vector3((progressOfSection + progress) / 4f, 1, 1);
    }

}
