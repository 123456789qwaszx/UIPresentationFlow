using System;
using System.Collections.Generic;
using Ked.Samples.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VisualNovelDemo
{
    /// <summary>
    /// 데모의 가짜 재생 — 대본(VNDemoScript)을 재생 화면의 대사창 · 선택지 상자(화면 묶음의 UI 부품)에 그린다.
    /// ked에서는 이 자리가 본체(Yarn · 연출 · 타자기 · 자동 진행 · 되감기)다. 화면 묶음은 재생 런타임을 모르므로
    /// 데모는 화면에 무엇을 보일지만 정하면 된다.
    ///
    ///   타자기   글자를 TypeSpeed(글자/초)로 연다. 빨리 넘기기(켜 둠 · Ctrl을 누르는 동안)는 SpeedUp배.
    ///   진행     글자가 다 열리기 전이면 끝까지 열고, 열렸으면 다음 줄.
    ///   자동     다 열린 뒤 AutoDelay초 쉬고 다음 줄. 빨리 넘기기는 곧바로. 둘 다 선택지에서 멈춘다.
    ///   되감기   본 줄의 기록(History)을 거슬러 한 줄 앞으로. 백로그의 이동도 같은 기록을 자른다.
    /// Paused(Panel이 열림 · UI 숨김)면 아무것도 나아가지 않는다.
    /// </summary>
    internal sealed class VNDemoPlayer
    {
        internal sealed class Snapshot
        {
            public int Step;
            public int[] History;
        }

        private const float FastForwardGap = 0.05f;
        private const int AllCharacters = 99999;

        private readonly VNDemoScript _script;
        private readonly PlayScreen _play;
        private readonly Func<string, string> _text;
        private readonly VNDemoOptionItem _optionPrefab;
        private readonly List<int> _history = new();
        private readonly List<VNDemoOptionItem> _items = new();

        private int _step = -1;
        private float _revealed;
        private int _total;
        private bool _revealing;
        private bool _choosing;
        private float _wait;

        // 선택지를 고른 프레임. Enter로 고르면 같은 프레임에 흐름도 Enter를 진행으로 읽는다 — 새 줄을 곧바로 끝까지 열지 않게.
        private int _choseFrame = -1;

        public VNDemoPlayer(VNDemoScript script, PlayScreen play, Func<string, string> text, VNDemoOptionItem optionPrefab)
        {
            _script = script;
            _play = play;
            _text = text;
            _optionPrefab = optionPrefab;
        }

        public float TypeSpeed { get; set; } = 30f;
        public float AutoDelay { get; set; } = 1.5f;
        public float SpeedUp { get; set; } = 4f;

        public bool Auto { get; private set; }
        public bool FastForward { get; private set; }
        public bool Paused { get; set; }

        /// <summary>누르는 동안 빨리 넘기기(Ctrl). 켜 둔 빨리 넘기기와 같이 달리지만 퀵메뉴의 켜짐 표시는 바꾸지 않는다.</summary>
        public bool Holding { get; set; }

        private bool Rushing => FastForward || Holding;

        public bool IsRunning => _step >= 0;
        public bool IsChoosing => _choosing;

        /// <summary>본 줄의 차례(맨 끝이 지금 줄). 백로그가 읽는다.</summary>
        public IReadOnlyList<int> History => _history;

        /// <summary>자동 · 빨리 넘기기가 바뀌었다(퀵메뉴의 켜짐 표시).</summary>
        public event Action ModesChanged;

        /// <summary>줄이 다 열렸다(꼬리표 · 다음 표시).</summary>
        public event Action LineShown;

        public string SceneOfCurrent => IsRunning ? _script.SceneOf(_step) : string.Empty;

        public string SpeakerOf(int step) => Speaker(_script[step]);

        public string TextOf(int step) => _text(_script[step].TextKey);

        #region 시작 · 끝 · 기록

        public void Begin()
        {
            _history.Clear();
            SetModes(auto: false, fastForward: false);
            Show(0, instant: false);
        }

        public void Stop()
        {
            HideChoices();
            SetModes(auto: false, fastForward: false);

            _step = -1;
            _revealing = false;
            _history.Clear();

            _play.Dialogue.SetVisibleImmediate(false);
            _play.SetNextMarkerVisible(false);
        }

        public Snapshot Capture() => new() { Step = _step, History = _history.ToArray() };

        /// <summary>저장한 자리로 — 그 줄을 다 열린 채로 보인다.</summary>
        public void Restore(Snapshot snapshot)
        {
            _history.Clear();

            if (snapshot.History != null && snapshot.History.Length > 0)
                _history.AddRange(snapshot.History);

            // Show가 그 줄을 다시 기록하므로 맨 끝(그 줄)은 뺀다.
            if (_history.Count > 0)
                _history.RemoveAt(_history.Count - 1);

            SetModes(auto: false, fastForward: false);
            Show(Mathf.Clamp(snapshot.Step, 0, _script.Count - 1), instant: true);
        }

        #endregion

        #region 진행

        public void Tick(float deltaTime)
        {
            if (!IsRunning || Paused)
                return;

            if (_revealing)
            {
                float speed = TypeSpeed * (Rushing ? SpeedUp : 1f);
                _revealed = Mathf.Min(_total, _revealed + speed * deltaTime);
                _play.Dialogue.Line.maxVisibleCharacters = Mathf.FloorToInt(_revealed);

                if (_revealed >= _total)
                    Complete();

                return;
            }

            if (_choosing || (!Auto && !Rushing))
                return;

            _wait += deltaTime;
            if (_wait >= (Rushing ? FastForwardGap : AutoDelay))
                Next();
        }

        public void Advance()
        {
            if (!IsRunning || Paused || _choosing || Time.frameCount == _choseFrame)
                return;

            if (_revealing)
            {
                Complete();
                return;
            }

            Next();
        }

        public bool Rollback()
        {
            if (!IsRunning || Paused || _history.Count < 2)
                return false;

            _history.RemoveAt(_history.Count - 1);
            int previous = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);

            Show(previous, instant: true);
            return true;
        }

        /// <summary>백로그의 줄로 — 그 줄부터 다시. 지금 줄(맨 끝)로는 가지 않는다.</summary>
        public bool JumpTo(int historyIndex)
        {
            if (!IsRunning || historyIndex < 0 || historyIndex >= _history.Count - 1)
                return false;

            int step = _history[historyIndex];
            _history.RemoveRange(historyIndex, _history.Count - historyIndex);

            Show(step, instant: true);
            return true;
        }

        public void ToggleAuto() => SetModes(!Auto, fastForward: false);

        public void ToggleFastForward() => SetModes(auto: false, fastForward: !FastForward);

        public void StopFastForward()
        {
            if (FastForward)
                SetModes(Auto, fastForward: false);
        }

        #endregion

        private void Next()
        {
            VNDemoScript.Step step = _script[_step];

            int next = string.IsNullOrEmpty(step.Next) ? _step + 1 : _script.IndexOf(step.Next);
            if (next >= _script.Count)
                next = 0;

            Show(next, instant: false);
        }

        private void Show(int index, bool instant)
        {
            HideChoices();

            _step = index;
            _history.Add(index);
            _wait = 0f;

            VNDemoScript.Step step = _script[index];
            string speaker = Speaker(step);

            PlayDialogueBox box = _play.Dialogue;
            box.SetVisibleImmediate(true);
            box.PrimeText(_text(step.TextKey), speaker, !string.IsNullOrEmpty(speaker), hideCharacters: !instant);

            _total = box.Line.textInfo.characterCount;
            _revealed = instant ? _total : 0f;
            _revealing = !instant && _total > 0;

            if (!_revealing)
                Complete();
        }

        private void Complete()
        {
            _revealing = false;
            _revealed = _total;
            _wait = 0f;
            _play.Dialogue.Line.maxVisibleCharacters = AllCharacters;

            VNDemoScript.Step step = _script[_step];

            if (step.Choices != null && step.Choices.Length > 0)
                ShowChoices(step.Choices);
            else
                _play.SetNextMarkerVisible(true);

            LineShown?.Invoke();
        }

        private void ShowChoices(VNDemoScript.Choice[] choices)
        {
            _choosing = true;

            // 빨리 넘기기는 선택지에서 멈춘다(ked 본체와 같다). 자동은 켜 둔 채 고르기를 기다린다.
            StopFastForward();

            PlayOptionsBox options = _play.Options;

            for (int i = 0; i < choices.Length; i++)
            {
                VNDemoScript.Choice choice = choices[i];
                VNDemoOptionItem item = Object.Instantiate(_optionPrefab, options.ItemContainer, false);
                item.name = "Option_" + i;
                item.Bind(_text(choice.TextKey), () => Choose(choice.Target));
                _items.Add(item);
            }

            options.SetVisibleImmediate(true);
            options.SetInputEnabled(true);

            // 키보드로도 고를 수 있게 첫 항목에 포커스(재생 화면이 선택지 상자 안에서 고른다).
            _play.RestoreFocus();
        }

        private void Choose(string target)
        {
            if (!_choosing || Paused)
                return;

            _choseFrame = Time.frameCount;
            Show(_script.IndexOf(target), instant: false);
        }

        private void HideChoices()
        {
            _choosing = false;

            foreach (VNDemoOptionItem item in _items)
            {
                if (item == null)
                    continue;

                // 지우기는 프레임 끝이다 — 먼저 꺼서 새 항목의 레이아웃 · 포커스 찾기에 끼지 않게.
                item.gameObject.SetActive(false);
                Object.Destroy(item.gameObject);
            }

            _items.Clear();

            PlayOptionsBox options = _play.Options;
            options.SetInputEnabled(false);
            options.SetShown(false);
        }

        private void SetModes(bool auto, bool fastForward)
        {
            if (auto == Auto && fastForward == FastForward)
                return;

            Auto = auto;
            FastForward = fastForward;
            _wait = 0f;
            ModesChanged?.Invoke();
        }

        private string Speaker(VNDemoScript.Step step) =>
            string.IsNullOrEmpty(step.SpeakerKey) ? string.Empty : _text(step.SpeakerKey);
    }
}
