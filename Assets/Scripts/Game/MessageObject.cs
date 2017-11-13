using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game
{
    public class MessageObject : MonoBehaviour
    {
        public float baseY;

        [SerializeField]
        Text messageText;

        public void Initialize(string message, Color color, float positionX)
        {
            gameObject.SetActive(true);
            messageText.text = message;
            messageText.color = color;

            transform.localPosition = new Vector3(positionX, baseY);
            transform.DOKill();
            DOTween.Sequence()
                   .OnStart(() =>
                   {
                       transform.localScale = Vector3.zero;
                   })
                   .Append(transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce))
                   .Join(transform.DOLocalMoveY(baseY + 300f, 1f).SetEase(Ease.OutCirc))
                   .OnComplete(() =>
                   {
                       gameObject.SetActive(false);
                   }).Play();
        }
    }
}
