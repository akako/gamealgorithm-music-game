using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using Common;
using Common.Data;

namespace Recorder
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
        [SerializeField]
        TextAsset preloadSongDataAsset;

        bool isRecording = false;
        float previousTime = 0f;
        SongData song;

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

            // 楽曲データのロード
            if (null != preloadSongDataAsset)
            {
                song = SongData.LoadFromJson(preloadSongDataAsset.text);
                playButton.interactable = song.HasNote;
            }
            else
            {
                song = new SongData();
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
                    playButton.interactable = song.HasNote;

                    // 録音後の自動保存
                    var path = string.Format("Assets/Resources/{0}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    File.WriteAllText(path, JsonUtility.ToJson(song));
                    AssetDatabase.Refresh();
                }
            }
            else if (audioManager.bgm.isPlaying)
            {
                // 録音したノート音を再生
                foreach (var note in song.GetNotesBetweenTime(previousTime, bgmTime))
                {
                    audioManager.notes[note.NoteNumber].Play();
                    noteButtons[note.NoteNumber].Select();
                    StartCoroutine(DeselectCoroutine(noteButtons[note.NoteNumber]));
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
                // 録音終了時の処理
                audioManager.bgm.Stop();
            }
            else
            {
                // 録音開始時の処理
                song.ClearNotes();
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

                song.AddNote(audioManager.bgm.time, noteNo);
                audioManager.notes[noteNo].Play();
                noteButtons[noteNo].Select();
                StartCoroutine(DeselectCoroutine(noteButtons[noteNo]));
            };
        }
    }
}
