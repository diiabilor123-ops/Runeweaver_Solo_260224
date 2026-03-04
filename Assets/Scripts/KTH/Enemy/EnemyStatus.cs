using Runeweaver;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
    private Dictionary<ElementType, int> elementStacks = new Dictionary<ElementType, int>();

    public void AddElementStack(ElementType type, int amount = 1)
    {
        if (!elementStacks.ContainsKey(type)) elementStacks[type] = 0;
        elementStacks[type] += amount;

        if (elementStacks[type] >= 10)
        {
            ExecuteExplosion(type);
            elementStacks[type] = 0; // ÃÊ±âÈ­
        }
    }

    private void ExecuteExplosion(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: // È­¿° Æø¹ß
                Debug.Log("Fire Explosion!"); break;
            case ElementType.Ice: // ¾óÀ½ Æø¹ß + È®·ü Áõ°¡
                Debug.Log("Ice Explosion!"); break;
            case ElementType.Volt: // 8¹æÇâ ¶óÀÌÆ®´×
                Debug.Log("Volt 8-way Spark!"); break;
        }
    }
}