using UnityEngine;

/// <summary>
/// Egyszerű adatbázis a UnitCard ScriptableObject-ekhez.
/// A jelenetben legyen pontosan 1 példány (singleton).
/// </summary>
public class UnitDatabase : MonoBehaviour
{
    public static UnitDatabase Instance { get; private set; }

    [Header("Kártya definíciók (ScriptableObject-ek)")]
    public UnitCard[] defs;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UnitDatabase] Már létezik egy példány – ezt megsemmisítjük.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary> Visszaad egy UnitCard-ot név alapján (pont egyezés). </summary>
    public UnitCard GetByName(string unitName)
    {
        if (defs == null) return null;
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            if (d != null && d.unitName == unitName)
                return d;
        }
        return null;
    }

    /// <summary> ID → UnitCard (ID a tömb indexe). </summary>
    public UnitCard GetById(int id)
    {
        if (defs == null || id < 0 || id >= defs.Length) return null;
        return defs[id];
    }

    /// <summary> Név → ID (tömb index), -1 ha nincs. </summary>
    public int GetIdByName(string unitName)
    {
        if (defs == null) return -1;
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            if (d != null && d.unitName == unitName)
                return i;
        }
        return -1;
    }
}
