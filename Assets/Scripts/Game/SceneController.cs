using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Common;
using Common.Data;

namespace Game
{
    public class SceneController : MonoBehaviour
    {
        public const float PRE_NOTE_SPAWN_TIME = 3f;
        public const float PERFECT_BORDER = 0.05f;
        public const float GREAT_BORDER = 0.1f;
        public const float GOOD_BORDER = 0.2f;
        public const float BAD_BORDER = 0.5f;

        [SerializeField]
        AudioManager audioManager;
        [SerializeField]
        Button[] noteButtons;
        [SerializeField]
        Color defaultButtonColor;
        [SerializeField]
        Color highlightButtonColor;
        [SerializeField]
        TextAsset songDataAsset;
        [SerializeField]
        Transform noteObjectContainer;
        [SerializeField]
        NoteObject noteObjectPrefab;
        [SerializeField]
        Transform messageObjectContainer;
        [SerializeField]
        MessageObject messageObjectPrefab;
        [SerializeField]
        Transform baseLine;
        [SerializeField]
        GameObject gameOverPanel;
        [SerializeField]
        Button retryButton;
        [SerializeField]
        Text scoreText;
        [SerializeField]
        Text lifeText;

        float previousTime = 0f;
        SongData song;
        Dictionary<Button, int> lastTappedMilliseconds = new Dictionary<Button, int>();
        List<NoteObject> noteObjectPool = new List<NoteObject>();
        List<MessageObject> messageObjectPool = new List<MessageObject>();
        int life;
        int score;

        KeyCode[] keys = new KeyCode[]
        {
            KeyCode.Z, KeyCode.S, KeyCode.X, KeyCode.D, KeyCode.C
        };

        int Life
        {
            set
            {
                life = value;
                if (life <= 0)
                {
                    life = 0;
                    gameOverPanel.SetActive(true);
                }
                lifeText.text = string.Format("Life: {0}", life);
            }
            get { return life; }
        }

        int Score
        {
            set
            {
                score = value;
                scoreText.text = string.Format("Score: {0}", score);
            }
            get { return score; }
        }

        void Start()
        {
            // フレームレート設定
            Application.targetFrameRate = 60;

            Score = 0;
            Life = 10;
            retryButton.onClick.AddListener(OnRetryButtonClick);

            // ボタンのリスナー設定と最終タップ時間の初期化
            for (var i = 0; i < noteButtons.Length; i++)
            {
                noteButtons[i].onClick.AddListener(GetOnNoteButtonClickAction(i));
                lastTappedMilliseconds.Add(noteButtons[i], 0);
            }

            // ノートオブジェクトのプール
            for (var i = 0; i < 100; i++)
            {
                var obj = Instantiate(noteObjectPrefab, noteObjectContainer);
                obj.baseY = baseLine.localPosition.y;
                obj.gameObject.SetActive(false);
                noteObjectPool.Add(obj);
            }
            noteObjectPrefab.gameObject.SetActive(false);

            // メッセージオブジェクトのプール
            for (var i = 0; i < 50; i++)
            {
                var obj = Instantiate(messageObjectPrefab, messageObjectContainer);
                obj.baseY = baseLine.localPosition.y;
                obj.gameObject.SetActive(false);
                messageObjectPool.Add(obj);
            }
            messageObjectPrefab.gameObject.SetActive(false);

            // 楽曲データのロード
            song = SongData.LoadFromJson(songDataAsset.text);

            audioManager.bgm.PlayDelayed(1f);
        }

        void Update()
        {
            // キーボード入力も可能に
            for (var i = 0; i < keys.Length; i++)
            {
                if (Input.GetKeyDown(keys[i]))
                {
                    noteButtons[i].onClick.Invoke();
                }
            }

            // ノートを生成
            var bgmTime = audioManager.bgm.time;
            foreach (var note in song.GetNotesBetweenTime(previousTime + PRE_NOTE_SPAWN_TIME, bgmTime + PRE_NOTE_SPAWN_TIME))
            {
                var obj = noteObjectPool.FirstOrDefault(x => !x.gameObject.activeSelf);
                var positionX = noteButtons[note.NoteNumber].transform.localPosition.x;
                obj.Initialize(this, audioManager.bgm, note, positionX);
            }
            previousTime = bgmTime;
        }

        void OnNotePerfect(int noteNumber)
        {
            ShowMessage("Perfect", Color.yellow, noteNumber);
            Score += 1000;
        }

        void OnNoteGreat(int noteNumber)
        {
            ShowMessage("Great", Color.magenta, noteNumber);
            Score += 500;
        }

        void OnNoteGood(int noteNumber)
        {
            ShowMessage("Perfect", Color.green, noteNumber);
            Score += 300;
        }

        void OnNoteBad(int noteNumber)
        {
            ShowMessage("Bad", Color.gray, noteNumber);
            Life--;
        }

        public void OnNoteMiss(int noteNumber)
        {
            ShowMessage("Miss", Color.black, noteNumber);
            Life--;
        }

        void ShowMessage(string message, Color color, int noteNumber)
        {
            if (gameOverPanel.activeSelf)
            {
                return;
            }

            var positionX = noteButtons[noteNumber].transform.localPosition.x;
            var obj = messageObjectPool.FirstOrDefault(x => !x.gameObject.activeSelf);
            obj.Initialize(message, color, positionX);
        }

        /// <summary>
        /// ボタンのフォーカスを外します
        /// </summary>
        /// <returns>The coroutine.</returns>
        /// <param name="button">Button.</param>
        IEnumerator DeselectCoroutine(Button button)
        {
            yield return new WaitForSeconds(0.1f);
            if (lastTappedMilliseconds[button] <= DateTime.Now.Millisecond - 100)
            {
                button.image.color = defaultButtonColor;
            }
        }

        /// <summary>
        /// ノート（音符）に対応したボタン押下時のアクションを返します
        /// </summary>
        /// <returns>The on note button click action.</returns>
        /// <param name="noteNo">Note no.</param>
        UnityAction GetOnNoteButtonClickAction(int noteNo)
        {
            return () =>
            {
                if (gameOverPanel.activeSelf)
                {
                    return;
                }

                audioManager.notes[noteNo].Play();
                noteButtons[noteNo].image.color = highlightButtonColor;
                StartCoroutine(DeselectCoroutine(noteButtons[noteNo]));
                lastTappedMilliseconds[noteButtons[noteNo]] = DateTime.Now.Millisecond;

                var targetNoteObject = noteObjectPool.Where(x => x.NoteNumber == noteNo)
                                                     .OrderBy(x => x.AbsoluteTimeDiff)
                                                     .FirstOrDefault(x => x.AbsoluteTimeDiff <= BAD_BORDER);
                if (null == targetNoteObject)
                {
                    return;
                }

                var timeDiff = targetNoteObject.AbsoluteTimeDiff;
                if (timeDiff <= PERFECT_BORDER)
                {
                    OnNotePerfect(targetNoteObject.NoteNumber);
                }
                else if (timeDiff <= GREAT_BORDER)
                {
                    OnNoteGreat(targetNoteObject.NoteNumber);
                }
                else if (timeDiff <= GOOD_BORDER)
                {
                    OnNoteGood(targetNoteObject.NoteNumber);
                }
                else
                {
                    OnNoteBad(targetNoteObject.NoteNumber);
                }
                targetNoteObject.gameObject.SetActive(false);
            };
        }

        void OnRetryButtonClick()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
