using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TitleUIRoot : UIRoot<TitleUIRoot.Refs>
{
    public event Action LobbyClicked;
    public event Action StartClicked;
    
    public enum Refs
    {
        TitleBG_Image,
        
        LobbyBtn_Button,
        
        [UIRefTextRole(UITextRole.Title)] 
        LobbyBtn_Text,
        
        StartBtn_Button,
        
        [UIRefTextRole(UITextRole.Body)]
        StartBtn_Text
    }

    private Image _titleBgImage;
    private Button _lobbyButton;
    private TMP_Text _lobbyBtnText;
    private Button _startButton;
    private TMP_Text _startBtnText;

    protected override void OnInitialize()
    {
        _titleBgImage = View.Image(Refs.TitleBG_Image);
        
        _lobbyButton = View.Button(Refs.LobbyBtn_Button);
        _lobbyBtnText = View.Text(Refs.LobbyBtn_Text);
        
        _startButton = View.Button(Refs.StartBtn_Button);
        _startBtnText = View.Text(Refs.StartBtn_Text);
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif
        
        _lobbyButton.onClick.RemoveAllListeners();
        _lobbyButton.onClick.AddListener(HandleLobbyClicked);
        
        _startButton.onClick.RemoveAllListeners();
        _startButton.onClick.RemoveListener(HandleStartClicked);
    }
    
    private void HandleLobbyClicked()
    {
        LobbyClicked?.Invoke();
    }

    private void HandleStartClicked()
    {
        StartClicked?.Invoke();
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