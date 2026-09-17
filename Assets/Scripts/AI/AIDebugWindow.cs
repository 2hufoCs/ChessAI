using NaughtyAttributes;
using TMPro;
using UnityEngine;
using System;

public class AIDebugWindow : MonoBehaviour
{
    [SerializeField] private Minmax minmax;
    [SerializeField] private int moveCalculationDepth;

    [Header("UI References")] 
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Transform textListParent;
    
    [Button]
    void DebugMoveCount()
    {
        for (int i = textListParent.childCount - 1; i >= 0 ; i--)
            Destroy(textListParent.GetChild(i).gameObject);
        
        for (int i = 1; i <= moveCalculationDepth; i++)
        {
            // Use timer to calculate minmax duration taken
            float initialTime = DateTime.Now.Millisecond;
            int numPositions = minmax.GetMoveCount(i);
            float timeDiff = DateTime.Now.Millisecond - initialTime;
            
            GameObject newText = Instantiate(textPrefab, textListParent);
            string txt = $"depth: {i}; {numPositions} positions, Time: {timeDiff}  milliseconds";
            
            newText.GetComponentInChildren<TextMeshProUGUI>().text = txt;
            newText.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
        }
    }
}
