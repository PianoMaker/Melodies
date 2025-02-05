using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using static Music.Engine;
using static Music.Globals;

namespace Music
{
    public class Melody : Scale
    {
        // private List<Note> notes = new List<Note>();
        Random rnd = new Random();
        public int Tempo { get; set; }
        public Melody() { }
        public Melody(List<Note> nt) { this.notes = nt; }

        public Melody(List<string> notes) : base(notes)
        {
            Tempo = 120;
        }

        public Melody(string input) : base(input)
        {
            Tempo = 120;
        }

        public new Note this[int index]
        { get { return notes[index]; } set { this[index] = value; } }

        public new List<Note> GetNotes() { return notes; }

        /*
                public Tonalities DetectTonality()
                {
                    float maxsharpness = notes[^1].Sharpness();
                    float minsharpness = notes[^1].Sharpness();

                    MODE mode;

                    foreach (Note note in notes)
                    {
                        if (note.Sharpness() > maxsharpness) maxsharpness = note.Sharpness();
                        else if (note.Sharpness() < minsharpness) minsharpness = note.Sharpness();
                    }

                    if (maxsharpness - minsharpness > 9) return null;
                    if (notes[^1].Sharpness() - minsharpness <= 1 && maxsharpness - notes[^1].Sharpness() <= 5) mode = MODE.dur;
                    else if (notes[^1].Sharpness() - minsharpness <= 4 && maxsharpness - notes[^1].Sharpness() <= 5) mode = MODE.moll;
                    else return null;


                    Tonalities tonality = new Tonalities(Notes[^1], mode);
                    return tonality;
                }
        */
        public bool IfStartsFromNote(Note note)
        {
            Console.WriteLine($"first pitch is {pitch_to_notename(note.Step, note.Pitch)}"); 
            return Notes[0].Pitch == note.Pitch;
        }




        public bool IfStartsFromNote(string input)
        {
            var note = new Note(input);
            return IfStartsFromNote(note);
        }


        public int LongestCommonSubstring(Melody other)
        {
            var notesThis = GetPitches().ToArray();
            var notesOther = other.GetPitches().ToArray();
            return LongestCommonSubstring(notesThis, notesOther).Count;
        }

        


        static List<int> LongestCommonSubstring(int[] A, int[] B)
        {
            int n = A.Length;
            int m = B.Length;
            int[,] dp = new int[n + 1, m + 1];

            int maxLength = 0;
            int endIndex = -1;

            // Заповнення таблиці для знаходження максимальної спільної підстрічки
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    if (A[i - 1] == B[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;

                        if (dp[i, j] > maxLength)
                        {
                            maxLength = dp[i, j];
                            endIndex = i - 1;  // Зберігаємо кінцевий індекс підстрічки в A
                        }
                    }
                    else
                    {
                        dp[i, j] = 0; // Якщо елементи не рівні, підстрічка завершується
                    }
                }
            }

            // Відновлення найдовшої спільної підстрічки
            List<int> lcs = new List<int>();
            if (endIndex != -1)
            {
                for (int i = endIndex - maxLength + 1; i <= endIndex; i++)
                {
                    lcs.Add(A[i]);
                }
            }

            return lcs;
        }


        public new void Inversion()
        {
            if (notes.Count > 1)
            {
                Note firstNote = notes[0]; // Зберігаємо посилання на перший елемент

                // Зсуваємо всі елементи вперед, від другого до останнього
                for (int i = 0; i < notes.Count - 1; i++)
                {
                    notes[i] = notes[i + 1];
                }

                // Останній елемент отримує значення першого елемента
                notes[^1] = firstNote;
                Adjust();
            }
        }

        public void Join(Melody other)
        {
            
            foreach (Note note in other.notes)
            {
                Notes.Add(note);
            }
        }

        public static Melody Join(IList<Melody> melodies)
        {
            Melody newmelody = new();
            foreach (var melody in melodies)
                newmelody.Join(melody);
            return newmelody;
        }

        public new int Octaves()
        { return Range() / 12; }

/*
        public List<Melody> Permute() // генерування усіх можливих розташувань
        {
            PermutationsGenerator<Note> generator = new();

            var permutations = generator.GeneratePermutations(notes);

            List<Melody> list = new();
            foreach (List<Note> chord in permutations)
            {
                Melody newchord = new(chord);
                //newchord.Adjust(0);
                list.Add(newchord);
            }
            return list;
        }

        public new Melody[] PermuteList() // генерування усіх можливих розташувань
        {
            PermutationsGenerator<Note> generator = new();

            List<List<Note>> permutations = generator.GeneratePermutations(notes);

            Melody[] list = new Melody[permutations.Count];
            for (int i = 0; i < permutations.Count; i++)
            {
                Melody newchord = new(permutations[i]);
                //newchord.Adjust(0);
                list[i] = newchord;
            }
            return list;
        }


        public new void Play()
        {
            if (player == PLAYER.beeper)
                Beeper.Play(this);
            if (player == PLAYER.naudio)
                NAPlayer.Play(this);
            if (player == PLAYER.midiplayer)
                MidiFile0.Play(this);
        }
*/
        public new void RemoveNote(Note note) { notes.Remove(note); }

        public new void Reverse()
        { notes.Reverse(); }
                

        public new int Range()
        { return pitchdiff(notes[0].AbsPitch(), notes[^1].AbsPitch()); }


        public new int Size() { return notes.Count(); }


        public void Transpose(INTERVALS interval, QUALITY quality, DIR dir)
        {
            foreach (Note note in notes)
                note.Transpose(interval, quality, dir);
        }

        public void TransposeToLowNote(Note note) // chord
        {

            if (note == Notes[0]) return;
            DIR dir = new();
            if (note.CompareTo(Notes[0]) == 1) dir = DIR.UP;
            else dir = DIR.DOWN;

            Interval move = new(notes[0], note);

            foreach (Note nt in notes)
                nt.Transpose(move, dir);
        }

        public void TransposeToHighNote(Note note)
        {

            if (note  == Notes[^1]) return;
            DIR dir = new();
            if (note > Notes[^1]) dir = DIR.UP;
            else dir = DIR.DOWN;

            Interval move = new(notes[^1], note);

            foreach (Note nt in notes)
                nt.Transpose(move, dir);
        }

        public static List<Melody> Transpose(List<Melody> original, INTERVALS interval, QUALITY quality, DIR dir)
        {
            List<Melody> transposed = Clone(original);
            foreach (Melody ch in transposed)
                ch.Transpose(interval, quality, dir);
            return transposed;
        }

        public bool EqualPitch(Melody other)
        {
            if (other.Notes.Count != Notes.Count) return false;
            for (int i = 0; i < Notes.Count; i++)
            {
                if (!Notes[i].EqualPitch(other.Notes[i])) return false;
            }
            return true;
        }

        //summary
        // Enharmonize to avoid double accidentals
        public void EnharmonizeSmart()
        {
            foreach (Note note in Notes)
                note.EnharmonizeSmart();
        }

        public Melody Inverse()
        {
            Melody newmelody = new();
            DIR dir = new DIR();
            foreach (Note note in Notes)
            {
                Note temp = (Note)Notes[0].Clone();
                Interval intreval = new Interval(temp, note);
                if (note > temp) dir = DIR.DOWN; else dir = DIR.UP;
                temp.Transpose(intreval, dir);
                newmelody.Notes.Add(temp);
            }
            return newmelody;
        }



        public void RandomizeOct(int oct)
        {            
            foreach (Note note in Notes)
                note.Oct = rnd.Next(oct);
        }

        public void RandomizeDur()
        {
            foreach (Note note in Notes)
                note.SetRandomDuration();
        }


        /// <summary>
        /// ////////////////TEST SECTION///////////////////
        /// </summary>


        /*
        public static void DisplayTable(List<Melody> list)
        {
            //foreach (Melody ch in list)
            //    ch.Display();
            StringOutput.Display(list);
        }

        public new void DisplayInline()
        {
            foreach (Note note in notes)
            {
                note.DisplayInline();
            }
        }

        public static void DisplayInline(List<Melody> list)
        {
            foreach (Melody ch in list)
            {
                ch.DisplayInline();
                Console.WriteLine();
            }

        }

        public new void Test()
        {
            DisplayInline();
            Play();
        }

        public static void Test(List<Melody> list)
        {
            foreach (Melody ch in list)
            {
                ch.Test();
            }
        }
        */
        



        /// <summary>
        /// Клонування об'єктів
        /// </summary>
        /// <returns></returns>
        /// 
        

        public override object Clone()
        {
            Melody clone = new();
            // Здійснюємо глибоке клонування для елементів Melody
            clone.notes = new List<Note>(this.notes.Count);
            foreach (Note note in this.notes)
            {
                clone.notes.Add((Note)note.Clone());
            }
            return clone;
        }

        public static List<Melody> Clone(List<Melody> original)
        {
            List<Melody> clonedlist = new();
            foreach (Melody originalMelody in original)
            {
                Melody clonedMelody = (Melody)originalMelody.Clone();
                clonedlist.Add(clonedMelody);
            }
            return clonedlist;
        }

        public static Melody[] Clone(Melody[] original)
        {
            Melody[] cloned = new Melody[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                Melody clonedMelody = (Melody)original[i].Clone();
                cloned[i] = clonedMelody;
            }
            return cloned;
        }
/*
        internal void PlayMelody()
        {
            Player.Play(this);
        }

        public bool SaveMidi(string filepath="output.mid")
        {
            return MidiFile0.SaveMidi(this, filepath);
        }
        */




    }
}
