using System;
using System.Collections.Generic;

namespace VisualNovelDemo
{
    /// <summary>
    /// 데모 대본 — 줄과 선택지. 글자는 글자 표(VNDemoTexts.csv)의 키라, 설정에서 언어를 바꾸면 대본도 바뀐다.
    /// 재생 런타임이 없는 데모라 대본도 코드 한 곳에 적는다(ked에서는 Yarn 대본이 이 자리다).
    /// 줄이 데모에서 무엇을 해 볼지 안내한다 — 화면 하나하나가 UIPF의 어느 층인지.
    /// </summary>
    internal sealed class VNDemoScript
    {
        internal sealed class Step
        {
            public string Label;        // 장면 이름(꼬리표). 비우면 앞 줄의 장면을 잇는다
            public string SpeakerKey;   // 비우면 이름 없는 줄(해설)
            public string TextKey;
            public Choice[] Choices;    // 있으면 이 줄 뒤에 고른다
            public string Next;         // 다음 줄 대신 갈 곳(장면 이름). 비우면 바로 다음 줄
        }

        internal readonly struct Choice
        {
            public readonly string TextKey;
            public readonly string Target;

            public Choice(string textKey, string target)
            {
                TextKey = textKey;
                Target = target;
            }
        }

        private readonly List<Step> _steps = new();
        private readonly Dictionary<string, int> _labels = new(StringComparer.Ordinal);

        public int Count => _steps.Count;

        public Step this[int index] => _steps[index];

        public static VNDemoScript Create()
        {
            var script = new VNDemoScript();

            script.Line("start", null, "script.intro");
            script.Line(null, "speaker.harin", "script.root");
            script.Line(null, "speaker.shigure", "script.panel");
            script.Line(null, "speaker.harin", "script.backlog");
            script.Line(null, "speaker.shigure", "script.page");
            script.Line(null, "speaker.harin", "script.textScale");
            script.Line(null, "speaker.shigure", "script.variant");
            script.Choose(null, "speaker.harin", "script.choose",
                new Choice("script.choice.save", "save"),
                new Choice("script.choice.again", "start"));

            script.Line("save", "speaker.shigure", "script.save1");
            script.Line(null, "speaker.harin", "script.save2");
            script.Line("end", null, "script.end", next: "start");

            return script;
        }

        public int IndexOf(string label) =>
            label != null && _labels.TryGetValue(label, out int index) ? index : 0;

        // 그 줄이 속한 장면 이름 — 위로 올라가며 처음 만나는 Label.
        public string SceneOf(int index)
        {
            for (int i = Math.Min(index, _steps.Count - 1); i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(_steps[i].Label))
                    return _steps[i].Label;
            }

            return string.Empty;
        }

        private void Line(string label, string speakerKey, string textKey, string next = null)
        {
            Add(new Step { Label = label, SpeakerKey = speakerKey, TextKey = textKey, Next = next });
        }

        private void Choose(string label, string speakerKey, string textKey, params Choice[] choices)
        {
            Add(new Step { Label = label, SpeakerKey = speakerKey, TextKey = textKey, Choices = choices });
        }

        private void Add(Step step)
        {
            if (!string.IsNullOrEmpty(step.Label))
                _labels[step.Label] = _steps.Count;

            _steps.Add(step);
        }
    }
}
