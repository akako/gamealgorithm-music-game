using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Common.Recorder
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField]
        AudioManager audioManager;
        [SerializeField]
        Button recordButton;
        [SerializeField]
        Button playButton;
        [SerializeField]
        Button[] noteButtons;
        [SerializeField]
        Text time;

        List<Note> notes = new List<Note>();
        bool isRecording = false;
        float previousTime = 0f;

        void Start()
        {
            // フレームレート設定
            Application.targetFrameRate = 60;

            // ボタンのリスナー設定
            recordButton.onClick.AddListener(OnRecordButtonClick);
            playButton.onClick.AddListener(OnPlayButtonClick);
            for (var i = 0; i < noteButtons.Length; i++)
            {
                noteButtons[i].onClick.AddListener(GetOnNoteButtonClickAction(i));
            }
        }

        void Update()
        {
            // 時間表示の更新
            var bgmTime = audioManager.bgm.time;
            time.text = string.Format("{0:00}:{1:00}:{2:000}",
                Mathf.FloorToInt(bgmTime / 60f), 
                Mathf.FloorToInt(bgmTime % 60f),
                Mathf.FloorToInt(bgmTime % 1f * 1000));

            // キーボード入力も可能に
            var keys = new KeyCode[]
            {
                KeyCode.Z, KeyCode.S, KeyCode.X, KeyCode.D, KeyCode.C
            };
            for (var i = 0; i < keys.Length; i++)
            {
                if (Input.GetKeyDown(keys[i]))
                {
                    noteButtons[i].onClick.Invoke();
                }
            }

            if (isRecording)
            {
                // 録音中
                if (!audioManager.bgm.isPlaying)
                {
                    isRecording = false;
                    playButton.interactable = notes.Count > 0;
                }
            }
            else if (audioManager.bgm.isPlaying)
            {
                // 録音したノート音を再生
                foreach (var note in notes.Where(x => previousTime < x.time && x.time <= bgmTime))
                {
                    audioManager.notes[note.noteNumber].Play();
                    noteButtons[note.noteNumber].Select();
                    StartCoroutine(DeselectCoroutine(noteButtons[note.noteNumber]));
                }
                previousTime = bgmTime;
            }
        }

        /// <summary>
        /// ボタンのフォーカスを外します
        /// </summary>
        /// <returns>The coroutine.</returns>
        /// <param name="button">Button.</param>
        IEnumerator DeselectCoroutine(Button button)
        {
            yield return new WaitForSeconds(0.1f);
            if (EventSystem.current.currentSelectedGameObject == button.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// RECボタンが押された際の処理です
        /// </summary>
        void OnRecordButtonClick()
        {
            if (audioManager.bgm.isPlaying)
            {
                audioManager.bgm.Stop();
            }
            else
            {
                // 録音開始時は前の録音内容をクリアする
                notes.Clear();
                audioManager.bgm.Play();
                isRecording = true;
            }
        }

        /// <summary>
        /// PLAYボタンが押された際の処理です
        /// </summary>
        void OnPlayButtonClick()
        {
            if (isRecording)
            {
                return;
            }

            if (audioManager.bgm.isPlaying)
            {
                audioManager.bgm.Stop();
            }
            else
            {
                previousTime = audioManager.bgm.time;
                audioManager.bgm.Play();
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
                if (!audioManager.bgm.isPlaying)
                {
                    return;
                }

                notes.Add(new Note(audioManager.bgm.time, noteNo));
                audioManager.notes[noteNo].Play();
                noteButtons[noteNo].Select();
                StartCoroutine(DeselectCoroutine(noteButtons[noteNo]));
            };
        }
    }
}
