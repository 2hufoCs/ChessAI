using NaughtyAttributes;
using TMPro;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AIDebugWindow : MonoBehaviour
{
    [SerializeField] private Minmax minmax;
    [SerializeField] private int moveCalculationDepth;

    [Header("UI References")] 
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Transform textListParent;
    
    [Button]
    IEnumerator DebugMoveCount()
    {
        for (int i = textListParent.childCount - 1; i >= 0 ; i--)
            Destroy(textListParent.GetChild(i).gameObject);
        
        for (int i = 1; i <= moveCalculationDepth; i++)
        {
            DateTime initialTime = DateTime.Now;
            yield return StartCoroutine(minmax.GetOptimizedMoveCount(i)); // wait until moves have been calculated
            
            // Use timer to calculate minmax duration taken
            int numPositions = minmax.totalPositions;
            TimeSpan diff = DateTime.Now - initialTime;
            float timeDiff = diff.Seconds * 1000 + diff.Milliseconds;
            
            GameObject newText = Instantiate(textPrefab, textListParent);
            string txt = $"depth: {i}; {numPositions} positions, Time: {timeDiff}  milliseconds";
            
            newText.GetComponentInChildren<TextMeshProUGUI>().text = txt;
            newText.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
        }
    }

    public void CalculateNumPositions()
    {
        StartCoroutine(DebugMoveCount());
    }
}
