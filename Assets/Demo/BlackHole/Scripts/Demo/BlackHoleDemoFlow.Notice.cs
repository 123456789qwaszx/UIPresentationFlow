namespace BlackHole.Unity
{
    internal sealed partial class BlackHoleDemoFlow
    {
        private NoticeOverlay _notice;

        // 알림 한 줄(Overlay). 처음 알릴 때 열고 그 뒤로는 계속 둔다(알림이 없으면 아무것도 그리지 않는다).
        // 창(Panel)과 달리 쌓이지 않고 입력을 막지 않는다. 테마·글자 배율이 바뀌면 UIManager가 다시 적용한다.
        private void ShowNotice(string text, float holdSeconds)
        {
            _notice ??= _ui.ShowOverlay<NoticeOverlay>(_presentations.Notice);
            _notice.Show(text, holdSeconds);
        }

        private void DisposeNotice()
        {
            if (_notice != null)
            {
                _notice.Clear();
                _ui.HideOverlay<NoticeOverlay>();
            }

            _notice = null;
        }
    }
}
