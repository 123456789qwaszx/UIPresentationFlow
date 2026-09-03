using UnityEngine;
using UnityEngine.UI;

public sealed class TitleUIRoot : UIBase<TitleUIRoot.Refs>
{
    public enum Refs
    {
        TitleBG_Image,
        LobbyBtn_Button,
    }

    private Image _titleBackground;
    private Button _lobbyButton;

    protected override void OnInitialize()
    {
        _titleBackground = View.Image(Refs.TitleBG_Image);
        _lobbyButton = View.Button(Refs.LobbyBtn_Button);
        

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif
    }

    private void ValidateRefs()
    {
        if (_titleBackground == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.TitleBG_Image}", this);
        
        if(_lobbyButton == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.LobbyBtn_Button}", this);

    }
}