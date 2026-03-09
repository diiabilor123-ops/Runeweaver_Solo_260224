using UnityEngine;
using System.Collections;

public abstract class BossPattern : ScriptableObject
{
    public string patternName;

    // 모든 패턴이 공통적으로 실행할 추상 함수
    public abstract IEnumerator Execute(BossBrain brain);
}