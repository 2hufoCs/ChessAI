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
    [SerializeField] private GameObject smallTextPrefab;
    [SerializeField] private GameObject bigTextPrefab;
    [SerializeField] private Transform textListParent;

    void OnEnable()
    {
        ActionsBus.OnCheckmate += ShowCheckmateText;
        ActionsBus.OnDraw += ShowDrawText;
    }

    void OnDisable()
    {
        ActionsBus.OnCheckmate -= ShowCheckmateText;
        ActionsBus.OnDraw -= ShowDrawText;
    }
    
    [Button]
    IEnumerator DebugMoveCount()
    {
        for (int i = textListParent.childCount - 1; i >= 0 ; i--)
            Destroy(textListParent.GetChild(i).gameObject);
        
        GameObject moveCountText = Instantiate(smallTextPrefab, textListParent);
        moveCountText.GetComponentInChildren<TextMeshProUGUI>().text = "";
        for (int i = 1; i <= moveCalculationDepth; i++)
        {
            DateTime initialTime = DateTime.Now;
            yield return StartCoroutine(minmax.GetOptimizedMoveCount(i)); // wait until moves have been calculated
            
            // Use timer to calculate minmax duration taken
            int numPositions = minmax.totalPositions;
            TimeSpan diff = DateTime.Now - initialTime;
            float timeDiff = diff.Seconds * 1000 + diff.Milliseconds;
            
            
            string txt = $"depth: {i}; {numPositions} positions, Time: {timeDiff}  milliseconds\n";
            
            moveCountText.GetComponentInChildren<TextMeshProUGUI>().text += txt;
            moveCountText.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
        }
    }

    void ShowCheckmateText(PieceColor checkmatedColor)
    {
        GameObject checkmateText = Instantiate(bigTextPrefab, textListParent);
        PieceColor winnerColor = checkmatedColor == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        string msg = $"Checkmate! ({winnerColor.ToString() } wins)";
        checkmateText.GetComponentInChildren<TextMeshProUGUI>().text = msg;
    }

    void ShowDrawText(DrawOutcomes drawOutcome)
    {
        GameObject checkmateText = Instantiate(bigTextPrefab, textListParent);
        
        // Different text depending on how draw was achieved
        string msg = $"Draw! (because of  {drawOutcome.ToString()})";
        checkmateText.GetComponentInChildren<TextMeshProUGUI>().text = msg;
    }

    public void CalculateNumPositions()
    {
        StartCoroutine(DebugMoveCount());
    }
}
