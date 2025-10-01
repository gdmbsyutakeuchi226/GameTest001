using UnityEngine;

public class Panel : MonoBehaviour {
    public int number;  // 1〜9
    public Vector2Int boardPos; // グリッド座標

    public void OnClick(){
        Debug.Log($"Clicked Panel at {boardPos} with number {number}");
        // ゲームマネージャに処理を委譲
        GameManager.Instance.OnPanelClicked(this);
    }

    public void SetNumber(int num, Sprite sprite){
        number = num;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }
}
