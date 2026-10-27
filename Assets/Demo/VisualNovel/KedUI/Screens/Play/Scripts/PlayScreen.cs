using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 02 대화 · 03 선택지. UIPF Root — 재생 중에 Root 층을 차지하는 투명한 화면이다. 무대(캐릭터·배경)는 아래 캔버스의
    /// 연출 무대가 그리고, 이 화면은 그 위에 퀵메뉴·대사창·선택지를 얹는다(시안: "실제 캐릭터·배경은 기존 Stage가 표시").
    ///
    /// 층(아래 → 위):
    ///   AdvanceArea   — 화면 아무 데나 누르면 다음 대사(진행 버튼). 대사창은 입력을 받지 않아 클릭이 여기로 온다.
    ///   HudRoot       — 숨기기로 통째로 감추는 층. 꼬리표·퀵메뉴·대사창·선택지.
    ///   RestoreArea   — 숨긴 동안만 켜진다. 누르면 다시 보인다(진행하지 않는다).
    ///
    /// 대사창과 선택지 상자는 UI 쪽 부품(PlayDialogueBox · PlayOptionsBox)으로 내준다. 이 화면은 재생 런타임을 모르고,
    /// 재생 쪽 계약은 앱이 그 부품 위에 구현한다(ked: App/Runtime/Play). 재생 쪽도 화면을 모른다.
    /// </summary>
    public sealed class PlayScreen : UIRoot<PlayScreen.Refs>, ILocalizedView
    {
        public enum Refs
        {
            SafeAreaRoot,
            AdvanceArea,

            HudRoot,
            TopGradient,
            ChapterTag,
            ChapterTagText,

            QuickMenu,
            Quick_auto,
            Quick_autoBase,
            Quick_autoIcon,
            QuickLabel_auto,
            Quick_fast_forward,
            Quick_fast_forwardBase,
            Quick_fast_forwardIcon,
            QuickLabel_fast_forward,
            Quick_rollback,
            Quick_rollbackBase,
            Quick_rollbackIcon,
            QuickLabel_rollback,
            Quick_backlog,
            Quick_backlogBase,
            Quick_backlogIcon,
            QuickLabel_backlog,
            Quick_hide_ui,
            Quick_hide_uiBase,
            Quick_hide_uiIcon,
            QuickLabel_hide_ui,
            Quick_menu,
            Quick_menuBase,
            Quick_menuIcon,
            QuickLabel_menu,

            DialogueFrame,
            DialogueBox,
            SpeakerPlate,
            Speaker,
            DialogueText,
            NextMarker,
            AdvanceHint,

            OptionsRoot,
            ChoiceDim,
            OptionsContainer,

            RestoreArea,
            RestoreHint,
        }

        public event Action AdvanceClicked;
        public event Action AutoClicked;
        public event Action FastForwardClicked;
        public event Action RollbackClicked;
        public event Action BacklogClicked;
        public event Action HideClicked;
        public event Action MenuClicked;
        public event Action RestoreClicked;

        private PlayDialogueBox _dialogueBox;
        private PlayOptionsBox _optionsBox;
        private KedButton _autoButton;
        private KedButton _fastForwardButton;
        private bool _hudHidden;
        private GameObject _focusBeforeHide;
        private string _chapterLabel;
        private Localizer _texts;

        public PlayDialogueBox Dialogue
        {
            get
            {
                EnsureInitialized();
                return _dialogueBox;
            }
        }

        public PlayOptionsBox Options
        {
            get
            {
                EnsureInitialized();
                return _optionsBox;
            }
        }

        public bool IsHudHidden => _hudHidden;

        protected override void OnInitialize()
        {
            _dialogueBox = new PlayDialogueBox(
                View.Rect(Refs.DialogueFrame),
                View.CanvasGroup(Refs.DialogueFrame),
                View.Image(Refs.DialogueBox),
                View.Rect(Refs.SpeakerPlate),
                View.Text(Refs.DialogueText),
                View.Text(Refs.Speaker),
                View.Rect(Refs.NextMarker) != null ? View.Rect(Refs.NextMarker).gameObject : null);

            _optionsBox = new PlayOptionsBox(
                View.Rect(Refs.OptionsRoot),
                View.CanvasGroup(Refs.OptionsRoot),
                View.Rect(Refs.OptionsContainer));

            _autoButton = Look(Refs.Quick_auto);
            _fastForwardButton = Look(Refs.Quick_fast_forward);

            KedUIUtil.OnClick(View.Button(Refs.AdvanceArea), () => AdvanceClicked?.Invoke());
            BindQuick(Refs.Quick_auto, () => AutoClicked?.Invoke());
            BindQuick(Refs.Quick_fast_forward, () => FastForwardClicked?.Invoke());
            BindQuick(Refs.Quick_rollback, () => RollbackClicked?.Invoke());
            BindQuick(Refs.Quick_backlog, () => BacklogClicked?.Invoke());
            BindQuick(Refs.Quick_hide_ui, () => HideClicked?.Invoke());
            BindQuick(Refs.Quick_menu, () => MenuClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.RestoreArea), () => RestoreClicked?.Invoke());

            // 선택지 항목은 앱(재생 쪽)이 만든다. 붙는 대로 글자를 테마(사용자 글자 크기)에 등록한다.
            KedChildWatcher watcher = View.Component<KedChildWatcher>(Refs.OptionsContainer);
            if (watcher != null)
                watcher.ChildrenChanged += RegisterOptionTexts;

            // 처음에는 대사창·선택지 모두 감춘 상태 — 앱(재생 쪽)이 보인다.
            _dialogueBox.SetVisibleImmediate(false);
            _dialogueBox.SetNextMarkerVisible(false);
            _optionsBox.SetVisibleImmediate(false);
            _optionsBox.SetInputEnabled(false);

            SetHudHidden(false);
        }

        protected override void OnPresented(in UIAppliedTheme theme)
        {
            // 대사·이름 크기는 레이아웃(연출 프리셋) × 사용자 글자 크기. 레이아웃을 따르는 글자라 Styled에 넣지 않고 여기서 곱한다.
            _dialogueBox?.SetTextScale(theme.TextScale);
        }

        public void Relocalize(Localizer localizer)
        {
            _texts = localizer;
            RedrawChapterTag();
        }

        /// <summary>자동 진행·빨리 넘기기의 켜짐 표시.</summary>
        public void SetModes(bool auto, bool fastForward)
        {
            _autoButton?.SetSelectedLook(auto);
            _fastForwardButton?.SetSelectedLook(fastForward);
        }

        public void SetHudHidden(bool hidden)
        {
            bool changed = hidden != _hudHidden;
            _hudHidden = hidden;

            // 숨길 때 포커스(재생 쪽이 고른 선택지일 수 있다)를 기억했다가 다시 보일 때 돌려준다.
            if (changed && hidden)
                _focusBeforeHide = KedUIUtil.CurrentFocus();

            KedUIUtil.SetCanvasGroup(View.CanvasGroup(Refs.HudRoot), visible: !hidden, interactive: !hidden);

            Button restore = View.Button(Refs.RestoreArea);
            if (restore != null)
                KedUIUtil.SetActive(restore, hidden);

            if (hidden)
            {
                KedUIUtil.ClearFocus();
            }
            else if (changed)
            {
                if (!KedUIUtil.TryFocus(_focusBeforeHide))
                    RestoreFocus();

                _focusBeforeHide = null;
            }
        }

        /// <summary>떠 있는 선택지가 있으면 첫 항목에 포커스(Panel이 닫히거나 UI가 다시 보일 때). 없으면 포커스를 비운다.</summary>
        public void RestoreFocus()
        {
            CanvasGroup options = View.CanvasGroup(Refs.OptionsRoot);
            RectTransform container = View.Rect(Refs.OptionsContainer);

            if (options != null && options.alpha > 0f && options.interactable && container != null)
            {
                foreach (Selectable item in container.GetComponentsInChildren<Selectable>())
                {
                    if (KedUIUtil.TryFocus(item.gameObject))
                        return;
                }
            }

            KedUIUtil.ClearFocus();
        }

        public void SetChapterLabel(string label)
        {
            _chapterLabel = label;
            RedrawChapterTag();
        }

        public void SetNextMarkerVisible(bool visible) => _dialogueBox?.SetNextMarkerVisible(visible);

        public void SetDialogueOpacity(float opacity) => _dialogueBox?.SetOpacity(opacity);

        private void RedrawChapterTag()
        {
            bool has = !string.IsNullOrEmpty(_chapterLabel);
            Image tag = View.Image(Refs.ChapterTag);

            if (tag != null)
                KedUIUtil.SetActive(tag, has);

            KedUIUtil.SetText(View.Text(Refs.ChapterTagText), _chapterLabel);
        }

        private void BindQuick(Refs key, Action action)
        {
            KedUIUtil.OnClick(View.Button(key), () =>
            {
                // 누른 퀵메뉴 버튼에 포커스를 남기지 않는다 — 남으면 Space(다음 대사)가 그 버튼을 다시 누른다.
                KedUIUtil.ClearFocus();
                action();
            });
        }

        private KedButton Look(Refs key)
        {
            Button button = View.Button(key);
            return button != null && button.TryGetComponent(out KedButton look) ? look : null;
        }

        private void RegisterOptionTexts()
        {
            RectTransform container = View.Rect(Refs.OptionsContainer);
            if (container == null)
                return;

            Styled.Prune();

            foreach (TMP_Text text in container.GetComponentsInChildren<TMP_Text>(true))
                Styled.AddText(text, UITextRole.Body, UITextSizePolicy.Prefab);
        }
    }
}
