using UnityEngine;
using System.Collections; // 이거 꼭 있어야 함!

[UnityEngine.CreateAssetMenu(fileName = "IntroPattern", menuName = "Boss/Patterns/Intro")]
public class IntroPattern : BossPattern
{
    public float walkDuration = 2.0f;

    public override IEnumerator Execute(BossBrain brain)
    {
        // 1. 등을 보이고 천천히 걷기
        brain.anim.SetFloat("MoveSpeed", 0.3f);
        yield return new WaitForSeconds(walkDuration);

        // 2. 멈춰서 뒤돌아보기
        brain.anim.SetFloat("MoveSpeed", 0f);
        brain.transform.Rotate(0, 180, 0); // 슥 돌기

        // 3. 비웃거나 조롱하는 제스처 (애니메이터에 있다면 추가)
        brain.anim.SetTrigger("Gesture_HeadShake");
        yield return new WaitForSeconds(1.5f);

        Debug.Log("인트로 종료: 전투 시작");
    }
}