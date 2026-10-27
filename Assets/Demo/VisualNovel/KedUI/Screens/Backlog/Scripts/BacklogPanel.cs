using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 08 백로그. UIPF Panel. 기록은 재생 쪽 백로그(VNFeatureController.Backlogs)를 흐름이 읽어 넘긴다.
    /// 줄은 ScrollRect 안의 VerticalLayoutGroup에 견본(LogRowTemplate)을 복제해 쌓는다. 열면 맨 아래(지금 줄)로 간다.
    /// 지금 줄은 옅은 청록 바탕으로 표시한다.
    /// </summary>
    [UIViewKey("Ked.BacklogPanel")]
    public sealed class BacklogPanel : UIPanel<BacklogPanel.Refs>
    {
        public enum Refs
        {
            SafeAreaRoot,
            LogDim,
            LogPanel,
            BacklogTitle,
            BacklogClose,
            BacklogCloseBase,
            BacklogCloseIcon,

            LogScroll,
            LogViewport,
            LogContent,
            LogRowTemplate,
            LogEmpty,

            BacklogHint,
        }

        public event Action CloseClicked;
        public event Action<int> JumpClicked;

        private readonly List<BacklogRow> _rows = new();
        private BacklogRow _template;
        private bool _scrollToEndPending;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.BacklogClose), () => CloseClicked?.Invoke());

            _template = View.Widget<BacklogRow>(Refs.LogRowTemplate);
            if (_template != null)
                _template.gameObject.SetActive(false);
        }

        public void Present(IReadOnlyList<BacklogRow.Model> rows)
        {
            RectTransform content = View.Rect(Refs.LogContent);

            int count = rows?.Count ?? 0;
            EnsureRows(count, content);

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < count;
                KedUIUtil.SetActive(_rows[i], used);

                if (used)
                    _rows[i].Present(rows[i]);
            }

            KedUIUtil.SetActive(View.Rect(Refs.LogEmpty), count == 0);

            // 레이아웃이 줄 높이를 계산한 뒤에 맨 아래로 — 같은 프레임에 바로 내리면 높이가 0이라 안 내려간다.
            _scrollToEndPending = true;
            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            ScrollToEnd();

            KedUIUtil.Focus(View.Button(Refs.BacklogClose));
        }

        private void LateUpdate()
        {
            if (_scrollToEndPending)
                ScrollToEnd();
        }

        private void ScrollToEnd()
        {
            ScrollRect scroll = View.Component<ScrollRect>(Refs.LogScroll);
            if (scroll == null)
                return;

            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
            _scrollToEndPending = false;
        }

        private void EnsureRows(int count, RectTransform content)
        {
            if (_template == null || content == null)
                return;

            while (_rows.Count < count)
            {
                BacklogRow row = Instantiate(_template, content);
                row.name = "LogRow_" + _rows.Count;
                row.gameObject.SetActive(true);
                row.EnsureInitialized();
                row.Index = _rows.Count;
                row.JumpClicked += r => JumpClicked?.Invoke(r.Index);

                // 읽는 글자는 사용자 글자 크기를 따른다.
                Styled.AddText(row.SpeakerText, UITextRole.Body, UITextSizePolicy.Prefab);
                Styled.AddText(row.BodyText, UITextRole.Body, UITextSizePolicy.Prefab);

                _rows.Add(row);
            }
        }
    }
}
