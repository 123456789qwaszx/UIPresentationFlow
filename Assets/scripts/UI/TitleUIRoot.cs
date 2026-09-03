using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TitleUIRoot : UIBase<TitleUIRoot.Refs>
{
    public enum Refs
    {
        TitleBG_Image,
        LobbyBtn_Button,
        LobbyBtn_Text,
        
        StartBtn_Button,
        StartBtn_Text
    }

    private Image _titleBgImage;
    
    private Button _lobbyButton;
    
    [UIRefTextRole(UITextRole.Title)]
    private TMP_Text _lobbyBtnText;
    
    private Button _startButton;
    
    [UIRefTextRole(UITextRole.Body)]
    private TMP_Text _startBtnText;

    protected override void OnInitialize()
    {
        _titleBgImage = View.Image(Refs.TitleBG_Image);
        
        _lobbyButton = View.Button(Refs.LobbyBtn_Button);
        _lobbyBtnText =View.Text(Refs.LobbyBtn_Text);
        
        _startButton = View.Button(Refs.StartBtn_Button);
        _startBtnText =View.Text(Refs.StartBtn_Text);
        

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif
    }

    private void ValidateRefs()
    {
        if (_titleBgImage == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.TitleBG_Image}", this);
        
        if(_lobbyButton == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.LobbyBtn_Button}", this);
        if(_lobbyBtnText == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.LobbyBtn_Text}", this);
        
        if(_startButton == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.StartBtn_Button}", this);
        if(_startBtnText == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.StartBtn_Text}", this);
    }
}