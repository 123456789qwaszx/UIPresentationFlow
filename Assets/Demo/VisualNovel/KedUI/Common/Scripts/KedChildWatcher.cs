using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 남이 자식을 붙이는 칸(선택지 항목 칸 — 재생 쪽 VNOptionsPresenter가 항목을 만든다)에서
    /// 자식이 바뀐 것을 화면에 알린다. 화면은 새 항목의 글자를 테마(글자 크기)에 등록한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KedChildWatcher : MonoBehaviour
    {
        public event Action ChildrenChanged;

        private void OnTransformChildrenChanged()
        {
            ChildrenChanged?.Invoke();
        }
    }
}
