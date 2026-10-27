using System;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 10 안내 팝업 — 확인 버튼 하나. 확인 팝업과 같은 Panel 규칙(모달·한 번만 처리).
    /// 성공 표시(체크)는 실제로 끝난 일에만 쓴다(저장 완료 등). 실패는 체크 대신 경고 표시와 실제 이유.
    /// </summary>
    public sealed class NoticePanel : UIPanel<NoticePanel.Refs>
    {
        public enum Refs
        {
            SafeAreaRoot,
            NoticeDim,
            NoticePopup,
            NoticeTitle,
            NoticeCheck,
            NoticeWarning,
            NoticeBody,
            NoticeAccept,
            NoticeAcceptLabel,
        }

        public enum Kind
        {
            Success,
            Failure,
        }

        public sealed class Request
        {
            public string Title;
            public string Body;
            public string AcceptLabel;
            public Kind Kind;
            public Action OnClosed;
        }

        public event Action<Request> Resolved;

        private Request _request;

        public bool IsPending => _request != null;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.NoticeAccept), Accept);

            Styled.AddText(View.Text(Refs.NoticeBody), UITextRole.Body, UITextSizePolicy.Prefab);
        }

        public void Present(Request request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));

            KedUIUtil.SetText(View.Text(Refs.NoticeTitle), request.Title);
            KedUIUtil.SetText(View.Text(Refs.NoticeBody), request.Body);
            KedUIUtil.SetText(View.Text(Refs.NoticeAcceptLabel), request.AcceptLabel);

            KedUIUtil.SetActive(View.Image(Refs.NoticeCheck), request.Kind == Kind.Success);
            KedUIUtil.SetActive(View.Image(Refs.NoticeWarning), request.Kind == Kind.Failure);

            KedUIUtil.SetButton(View.Button(Refs.NoticeAccept), true, true);
            KedUIUtil.Focus(View.Button(Refs.NoticeAccept));
        }

        /// <summary>확인 버튼 · Esc · Enter.</summary>
        public void Accept()
        {
            Request request = _request;
            if (request == null)
                return;

            _request = null;
            KedUIUtil.SetButton(View.Button(Refs.NoticeAccept), true, false);

            Resolved?.Invoke(request);
        }

        private void OnDisable()
        {
            _request = null;
        }
    }
}
