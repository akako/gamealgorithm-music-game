using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public struct Note
    {
        public readonly float time;
        public readonly int noteNumber;

        public Note(float time, int noteNumber)
        {
            this.time = time;
            this.noteNumber = noteNumber;
        }
    }
}
