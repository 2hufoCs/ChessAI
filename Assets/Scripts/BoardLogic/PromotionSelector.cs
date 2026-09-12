using UnityEngine;
using UnityEngine.InputSystem;

public class PromotionSelector : MonoBehaviour
{
    [SerializeField] private PieceType _type;
    [SerializeField] private PieceColor _color;
    [SerializeField] private LayerMask _promotionLayer;

    private bool _triggered = false;

    void OnEnable()
    {
        transform.localPosition *= _color == PieceColor.White ? 1 : -1;
        transform.localPosition *= BoardSettings.Instance.boardFlipped ? -1 : 1;
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (_triggered) return;
        
        Vector2 mouseScreenPos =  Input.mousePosition;
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Collider2D hit = Physics2D.OverlapCircle(mouseWorldPos, .01f,  _promotionLayer);

        if (hit == null) return;
        if (hit != GetComponent<Collider2D>()) return;

        _triggered = true;
        
        ActionsBus.OnPawnPromoted?.Invoke(_type);
    }
    
    Vector2 WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return snappedPos + (Vector2)transform.position + Vector2.one * 3;
    }
}
