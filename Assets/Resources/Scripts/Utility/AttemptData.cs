using System;

[Serializable]
public class AttemptData
{
    // 스테이지 id
    public int levelID;
    // 스테이지 클리어 여부
    public bool isClear;
    // 클리어까지 걸린 시간 (클리어 못하면 0)
    public float clearTime; 
    // 죽음까지 걸린 시간 (클리어하면 0)
    public float deathTime; 

    // 점프 횟수
    public int jumpCount;   
    // 사망 횟수
    public int deathCount;   
    // 리플레이 사용 횟수
    public int replayCount; 
    // 재시작 횟수
    public int retryCount;    
    // 기믹 상호작용 횟수
    public int gimmickInteractCount;   

    public AttemptData(int levelID)
    {
        this.levelID = levelID;
        isClear = false;
        clearTime = 0f;
        deathTime = 0f;
        jumpCount = 0;
        deathCount = 0;
        replayCount = 0;
        retryCount = 0;
        gimmickInteractCount = 0;
    }
}
