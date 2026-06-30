using UnityEngine;

/// <summary>
/// PowerUp 효과를 적용하는 인터페이스
/// 각 PowerUp 타입별로 구현 필요
/// </summary>
public interface IPowerUpEffect
{
    /// <summary>
    /// 플레이어에게 증강 효과 적용
    /// 중복 호출 시 효과는 누적됨
    /// </summary>
    void Apply(PlayerController target);
}
