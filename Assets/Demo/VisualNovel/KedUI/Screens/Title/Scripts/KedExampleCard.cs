using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 타이틀 오른쪽 카드에 보일 예제 소개. 글자는 UI 글자 표의 키다.
    /// 그림은 앱이 고른다(ked: 실제 예제 대본이 쓰는 배경 — 시안 샘플 그림이 아니다). 없으면 중립 바탕.
    /// </summary>
    public sealed class KedExampleCard
    {
        public string NameKey;
        public string TitleKey;
        public string SummaryKey;
        public Sprite Preview;
    }
}
