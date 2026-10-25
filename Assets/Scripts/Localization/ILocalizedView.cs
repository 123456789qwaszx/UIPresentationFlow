// 코드가 글자를 만드는 화면(점수 0 → "점수 0"처럼 값을 넣은 글자). 등록된 화면이면 ViewTexts가 시작할 때 한 번,
// 언어가 바뀔 때마다 부른다(닫힌 화면도). 화면은 Localizer를 기억해 두고 글자를 만들 때 쓰며,
// 여기서는 마지막으로 보여 준 값을 지금 언어로 다시 쓴다. 잠깐 떠 있는 툴팁은 닫아도 된다.
public interface ILocalizedView
{
    void Relocalize(Localizer localizer);
}
