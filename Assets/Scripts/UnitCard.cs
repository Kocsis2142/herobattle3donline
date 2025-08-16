using UnityEngine;

public enum CardRole { Defender, Attacker }

[CreateAssetMenu(menuName = "Cards/Unit Card")]
public class UnitCard : ScriptableObject
{
    [Header("Alap adatok")]
    public string unitName;           // ezzel hivatkozunk a szerveren
    public Sprite icon;

    [Header("Játékmenet")]
    public CardRole role = CardRole.Defender; // ⬅️ EZ DÖNT AZ ATTACKER/DEFENDER KÖZÖTT
    public int maxHP = 10;
    public int damage = 1;
    public float attackRate = 1f;
    public float attackRange = 1.5f;
    public float moveSpeed = 2f;

    [Header("Prefab override (opcionális)")]
    public GameObject prefabOverride;
}
