using System.Collections;
using UnityEngine;
using DG.Tweening; // DOTween �𓱓����Ă���O��

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class NumberTile : MonoBehaviour {
    [HideInInspector] public int value;
    [HideInInspector] public int x, y;

    private BoardManager board;
    private SpriteRenderer sr;

    public void Initialize(BoardManager board, int x, int y, int value)
    {
        this.board = board;
        this.x = x;
        this.y = y;
        this.value = value;

        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one;
        name = $"Tile_{x}_{y}_{value}";
        UpdateVisual();
    }

    public void SetCoordinates(int nx, int ny)
    {
        x = nx; y = ny;
        name = $"Tile_{x}_{y}_{value}";
    }

    public void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        // �����ڂ�Prefab���Ƃɕ����Ă���z��Ȃ̂ŐF�͐G��Ȃ����A�K�v�Ȃ炱����sr.color ��ύX
    }

    // BoardManager ����ĂԔėp�ړ��iDOTween�j
    public void MoveTo(Vector3 targetPos, float duration)
    {
        transform.DOKill(); // �ȑO��Tween ��؂�
        transform.DOMove(targetPos, duration).SetEase(Ease.OutCubic);
    }

    // ���ŃA�j���iDOTween�j�� Coroutine �Ƃ��đ҂Ă�悤�ɂ���
    public IEnumerator PlayDestroyAnimationCoroutine(float duration = 0.18f)
    {
        bool finished = false;
        transform.DOKill();
        transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack)
            .OnComplete(() => finished = true);

        while (!finished) yield return null;

        // Destroy �͂����ōs���iBoardManager ���ŎQ�Ƃ͊��� null �ɂ��Ă��܂��j
        Destroy(gameObject);
    }

    void OnMouseDown(){
        if (board != null) board.OnTileClicked(x, y);
    }

    // �ړ��A�j���[�V�����iTransform.position �Łj
    public IEnumerator MoveToPosition(Vector3 targetPos, float duration){
        Vector3 start = transform.position;
        float t = 0f;

        while (t < duration){
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, targetPos, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        transform.position = targetPos;
    }

    // ���ŃA�j���i�k�����Ă���j��j
    public IEnumerator PopAndDestroy(){
        float dur = 0.12f;
        float t = 0f;
        Vector3 startScale = transform.localScale;

        while (t < dur){
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / dur);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ���ŃA�j���i�k���{�t�F�[�h�j
    public void PlayDestroyAnimation(System.Action onComplete)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => {
            onComplete?.Invoke();
            Destroy(gameObject);
        });
    }
    
    public void Highlight(bool highlight)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (highlight)
        {
            sr.color = Color.yellow;
        }
        else
        {
            sr.color = Color.white;
        }
    }
    
    void UpdateSpriteForValue()
    {
        // 数字に応じてスプライトを変更
        // この部分は実際のスプライトリソースに応じて実装する必要があります
        // 例：Resources.Load<Sprite>($"Block_{value}")
        // 現在はコメントアウトして、後で実装
        /*
        Sprite newSprite = Resources.Load<Sprite>($"Block_{value}");
        if (newSprite != null)
        {
            sr.sprite = newSprite;
        }
        */
    }

}
