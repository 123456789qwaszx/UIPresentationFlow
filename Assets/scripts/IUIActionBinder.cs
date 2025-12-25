using UnityEngine.UI;

public interface IUiActionBinder
{
    void Bind(ButtonWidget button, string route);
}