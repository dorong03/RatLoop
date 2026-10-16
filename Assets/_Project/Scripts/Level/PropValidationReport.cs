using System.Collections.Generic;
using UnityEngine;

// 레벨 저장 / 테스트 플레이 전에 실행하는 세이프티 체크 결과.
// Error 는 저장을 막고, Warning 은 알려주기만 한다.
public class PropValidationReport
{
    public enum Severity { Warning, Error }

    public readonly struct Entry
    {
        public readonly Severity severity;
        public readonly string message;
        public readonly Object context;

        public Entry(Severity severity, string message, Object context)
        {
            this.severity = severity;
            this.message = message;
            this.context = context;
        }
    }

    private readonly List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;
    public bool HasErrors => entries.Exists(e => e.severity == Severity.Error);

    public void Error(string message, Object context = null)
    {
        entries.Add(new Entry(Severity.Error, message, context));
    }

    public void Warning(string message, Object context = null)
    {
        entries.Add(new Entry(Severity.Warning, message, context));
    }
}
