using UnityEngine;
using UnityEngine.InputSystem;

public class PromotionSelector : MonoBehaviour
{
    private PieceType _type;

    void OnClick(InputAction.CallbackContext context)
    {
        Vector3 mouseScreenPos =  Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Collider2D hit = Physics2D.OverlapCircle(mouseWorldPos, .01f);
        // Vector2 snappedPos = WorldToBoard(mouseWorldPos);
        // Vector2Int snappedWholePos = Vector2Int.RoundToInt(snappedPos);

        if (hit == null) return;
        ActionsBus.OnPawnPromoted?.Invoke(_type);
    }
    
    Vector2 WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return snappedPos + (Vector2)transform.position + Vector2.one * 3;
    }
}
