using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    // 버튼 상수는 bit index
    public const int MOUSEBUTTON0 = 0;  // byte → int 권장
    public const int MOUSEBUTTON1 = 1;
    public const int JUMP         = 2;
    public const int RELOAD       = 3;
    public const int AIM          = 4;

    public float     rotationY;   // 플레이어 몸 좌우 회전 (Yaw)
    public Vector3   direction;   // 이동 방향 (WASD) - moveDirection 제거
    public NetworkButtons buttons;
    // Jump, Fire 등 모든 버튼은 buttons 안으로
}