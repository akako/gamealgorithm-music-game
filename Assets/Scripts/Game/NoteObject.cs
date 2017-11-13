using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Common.Data;

namespace Game
{
    public class NoteObject : MonoBehaviour
    {
        public float baseY;

        [SerializeField]
        Image image;

        SceneController sceneController;
        AudioSource bgm;
        SongData.Note note;
        float positionX;

        public int NoteNumber
        {
            get { return gameObject.activeSelf ? note.NoteNumber : int.MinValue; }
        }

        public float AbsoluteTimeDiff
        {
            get { return Mathf.Abs(note.Time - bgm.time); }
        }

        void Update()
        {
            var timeDiff = note.Time - bgm.time;
            if (timeDiff < -SceneController.BAD_BORDER)
            {
                sceneController.OnNoteMiss(NoteNumber);
                gameObject.SetActive(false);
            }

            GetComponent<RectTransform>().localPosition = new Vector3(positionX,
                                                  baseY + timeDiff * 800f,
                                                  transform.localPosition.z);
        }

        public void Initialize(SceneController sceneController, AudioSource bgm, SongData.Note note, float positionX)
        {
            gameObject.SetActive(true);

            this.sceneController = sceneController;
            this.bgm = bgm;
            this.note = note;
            this.positionX = positionX;
            switch (note.NoteNumber)
            {
                case 1:
                case 3:
                    image.color = Color.green;
                    break;
                default:
                    image.color = Color.white;
                    break;
            }

            Update();
        }
    }
}