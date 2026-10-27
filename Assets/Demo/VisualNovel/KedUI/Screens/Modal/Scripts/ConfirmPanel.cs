using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 09 확인 팝업. UIPF Panel로 쌓는다 — Panel 스택의 맨 위만 입력을 받고(아래 Panel은 UIPF가 막는다),
    /// 딤이 화면 전체의 포인터를 막는다. 그래서 "모달"의 입력 차단은 UIPF의 Panel 규칙으로 얻는다.
    /// (UIPF의 Overlay는 아래 화면을 막지 않는 층이라 모달에 쓰지 않는다.)
    ///
    /// 한 번 열 때 받은 요청(제목·본문·버튼 글자·콜백)으로 동작한다. 수락·취소·닫기·Esc 중 무엇이든
    /// 처음 하나만 처리한다 — 두 번 누르거나 Esc와 겹쳐도 콜백은 한 번이다.
    /// 버튼은 OnInitialize에서 한 번만 묶는다. 다시 열어도 리스너가 늘지 않는다.
    /// </summary>
    [UIViewKey("Ked.ConfirmPanel")]
    public sealed class ConfirmPanel : UIPanel<ConfirmPanel.Refs>
    {
        public enum Refs
        {
            SafeAreaRoot,
            ConfirmDim,
            ConfirmPopup,
            ConfirmTitle,
            ConfirmClose,
            ConfirmCloseBase,
            ConfirmCloseIcon,
            ConfirmBody,
            ConfirmCancel,
            ConfirmCancelLabel,
            ConfirmAccept,
            ConfirmAcceptLabel,
        }

        public sealed class Request
        {
            public string Title;
            public string Body;
            public string AcceptLabel;
            public string CancelLabel;
            public Action OnAccept;
            public Action OnCancel;
        }

        /// <summary>요청이 끝났다(수락이면 true). 흐름이 이 Panel을 닫고 요청의 콜백을 부른다.</summary>
        public event Action<Request, bool> Resolved;

        private Request _request;

        public bool IsPending => _request != null;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.ConfirmAccept), () => Resolve(true));
            KedUIUtil.OnClick(View.Button(Refs.ConfirmCancel), () => Resolve(false));
            KedUIUtil.OnClick(View.Button(Refs.ConfirmClose), () => Resolve(false));

            Styled.AddText(View.Text(Refs.ConfirmBody), UITextRole.Body, UITextSizePolicy.Prefab);
        }

        public void Present(Request request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));

            KedUIUtil.SetText(View.Text(Refs.ConfirmTitle), request.Title);
            KedUIUtil.SetText(View.Text(Refs.ConfirmBody), request.Body);
            KedUIUtil.SetText(View.Text(Refs.ConfirmAcceptLabel), request.AcceptLabel);
            KedUIUtil.SetText(View.Text(Refs.ConfirmCancelLabel), request.CancelLabel);

            SetButtonsInteractable(true);

            // 되돌릴 수 없는 쪽을 기본으로 두지 않는다 — Enter 한 번에 수락되지 않게 취소에 포커스.
            KedUIUtil.Focus(View.Button(Refs.ConfirmCancel));
        }

        /// <summary>Esc. 취소와 같다.</summary>
        public void Cancel() => Resolve(false);

        private void Resolve(bool accepted)
        {
            Request request = _request;
            if (request == null)
                return;

            _request = null;
            SetButtonsInteractable(false);

            Resolved?.Invoke(request, accepted);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            KedUIUtil.SetButton(View.Button(Refs.ConfirmAccept), true, interactable);
            KedUIUtil.SetButton(View.Button(Refs.ConfirmCancel), true, interactable);
            KedUIUtil.SetButton(View.Button(Refs.ConfirmClose), true, interactable);
        }

        private void OnDisable()
        {
            // 다른 길(전체 닫기)로 닫혀도 남은 요청을 콜백 없이 버린다 — 나중에 늦게 불리지 않게.
            _request = null;
        }
    }
}
