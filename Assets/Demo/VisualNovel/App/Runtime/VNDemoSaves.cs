using System;
using System.Collections.Generic;

namespace VisualNovelDemo
{
    /// <summary>
    /// 데모의 수동 저장 — 메모리에만 둔다(플레이를 멈추면 사라진다). ked에서는 진행 패키지의 저장 슬롯이 이 자리다.
    /// 최근 저장이 앞(01)이다. 덮어쓰면 그 기록이 맨 앞으로 온다 — ked 예제와 같은 규칙.
    /// </summary>
    internal sealed class VNDemoSaves
    {
        internal sealed class Entry
        {
            public string Id;
            public string Title;
            public string Chapter;
            public DateTime SavedAt;
            public int PlaySeconds;
            public VNDemoPlayer.Snapshot Snapshot;
        }

        private readonly List<Entry> _entries = new();
        private int _nextId = 1;

        public IReadOnlyList<Entry> Entries => _entries;

        public Entry Find(string id) => _entries.Find(entry => entry.Id == id);

        public Entry Create(string title, string chapter, int playSeconds, VNDemoPlayer.Snapshot snapshot)
        {
            var entry = new Entry
            {
                Id = "slot-" + _nextId++,
                Title = title,
                Chapter = chapter,
                SavedAt = DateTime.Now,
                PlaySeconds = playSeconds,
                Snapshot = snapshot,
            };

            _entries.Insert(0, entry);
            return entry;
        }

        public bool Overwrite(string id, string title, string chapter, int playSeconds, VNDemoPlayer.Snapshot snapshot)
        {
            Entry entry = Find(id);
            if (entry == null)
                return false;

            _entries.Remove(entry);

            entry.Title = title;
            entry.Chapter = chapter;
            entry.SavedAt = DateTime.Now;
            entry.PlaySeconds = playSeconds;
            entry.Snapshot = snapshot;

            _entries.Insert(0, entry);
            return true;
        }
    }
}
