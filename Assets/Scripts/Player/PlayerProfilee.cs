using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
    [Header("Investigation")]
    [SerializeField, Min(1)] private int interrogationLimit = 3;

    public int InterrogationLimit => Mathf.Max(1, interrogationLimit);
}
