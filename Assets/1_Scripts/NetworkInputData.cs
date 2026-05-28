using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public const byte MOUSEBUTTON0 = 1;
    public const byte MOUSEBUTTON1 = 2;

    public float rotationY; // ✅ 추가
    public Vector3 direction;
    public Vector3 moveDirection;
    public NetworkButtons buttons;
    public bool Jump;
}