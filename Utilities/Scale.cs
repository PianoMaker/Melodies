using static Music.Engine;
using static Music.Messages;
using System.Linq;
using System.Collections;

namespace Music
{
    public class Scale : ICloneable, IEnumerable<Note>, IComparer<Scale>
    {
        protected List<Note> notes = new();

        public List<Note> Notes
        { get { return notes; } 
        set { notes = value; }
        }


        public List<int> Pitches
        {
            get
            {
                List<int> pitches = new List<int>();
                foreach (var note in notes)
                    pitches.Add(note.Pitch);

                return pitches;
            }
        }

        public List<int> Steps
        {
            get
            {
                List<int> steps = new List<int>();
                foreach (var note in notes)
                    steps.Add(note.Step);

                return steps;
            }
        }



        public Scale() { }
        public Scale(List<Note> notes) { this.notes = notes; }

        public Scale(params Note[] nt)
        {
            foreach (Note note in nt)
            {              
                if (note is not null) AddNote(note);
            }
        }

        public Scale(List<string> notes)
        {
            foreach (string input in notes)
            {
                Note note = new(input);
                if (note is not null) AddNote(note);
            }
           

        }

        public Scale(string input)
        {
            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (input is null || input == "0" || (input.All(char.IsWhiteSpace))) throw new IncorrectNote(note_error());
            else
                foreach (string i in parts)
                {
                    try
                    {
                        Note note = new(i);
                        AddNote(note);
                    }
                    catch (IncorrectNote) { Console.WriteLine("skip adding " + i); }
                };
        }

        public Note this[int index]
        { get { return notes[index]; } set { this[index] = value; } }

        public List<Note> GetNotes() { return notes; }

        public List<int> GetPitches()
        {
            List<int> pitches = new List<int>();
            foreach (var note in notes)
                pitches.Add(note.Pitch);

            return pitches;
        }

        public List<int> GetSteps()
        {
            List<int> steps = new List<int>();
            foreach (var note in notes)
                steps.Add(note.Step);

            return steps;
        }


        public void AddNote(Note note) {
            Note temp = (Note)note.Clone();              
            notes.Add(temp); 
        }

        public void AddNote(int interval, int quality)
        {
            if (notes[0] is null) throw new IncorrectNote("No initial note entered");
            Note newnote = (Note)notes[0].Clone();
            newnote.Transpose(interval, quality);
            AddNote(newnote);
        }
        public void AddNote(INTERVALS interval, QUALITY quality)
        {
            if (notes[0] is null) throw new IncorrectNote("No initial note entered");
            Note newnote = (Note)notes[0].Clone();
            newnote.Transpose(interval, quality);
            AddNote(newnote);
        }
        public void AddNote(Interval interval)
        {
            if (notes[0] is null) throw new IncorrectNote("No initial note entered");
            Note newnote = (Note)notes[0].Clone();
            newnote.Transpose(interval);
            AddNote(newnote);
        }


        public void Adjust()
        {
            for (int i = 0; i < notes.Count - 1; i++)
            {
                while (notes[i + 1].AbsPitch() < notes[i].AbsPitch())
                    notes[i + 1].OctUp(1);
                while (notes[i + 1].AbsPitch() - notes[i].AbsPitch() > 12)
                    notes[i + 1].OctDown(1);
            }

        }
        public void Adjust(int octave = 1)
        {
            while (notes[0].Oct > octave) OctDown();
            while (notes[0].Oct < octave) OctUp();
            Adjust();
        }


        public bool CheckForSharps()
        {
            foreach(Note note in notes)
                if (note.GetAlter() > 0) return true;   
            return false;
        }

        public bool CheckIfSharpable()
        {
            foreach (Note note in notes)
                if (note.Sharpness > -2) return false;
            return true;
        }

        public float CheckForMaxSharp()
        {
            if (notes.Count == 0) return 0;
            float max = notes[0].Sharpness;
            foreach (Note note in notes)
                if (note.Sharpness > max) max = note.Sharpness;
            return max;
        }

        public float CheckForMinSharp()
        {
            if (notes.Count == 0) return 0;
            float min = notes[0].Sharpness;
            foreach (Note note in notes)
                if (note.Sharpness < min) min = note.Sharpness;
            return min;
        }

        public bool CheckForFlats()
        {
            foreach (Note note in notes)
                if (note.GetAlter() < 0) return true;
            return false;
        }

        public bool CheckIfFlatable()
        {
            foreach (Note note in notes)
                if (note.Sharpness < 2) return false;
            return true;
        }

        public static bool IfNoteInScale(Scale scale, Note note)
        {
            foreach(Note _note in scale.Notes)
            {
                if (_note == note)
                    return true;
            }
            return false;
        }

        public static Note TryMakeToScale(Scale scale, Note note)
        {
            bool ifskiped = true;
            string confirmednotes = "Notes confirmed: ";
            string affectednotes = "Notes affected: ";
            string skipednotes = $"Notes out of tonality: {note}";
            MessageL(COLORS.cyan, $"analize {note.Name} {note.Pitch}");

            foreach (Note _note in scale.Notes)
            {
                //GrayMessageL($"compare to {_note.GetName()} {_note.Pitch}");
                if (_note.EqualDegree(note))
                {
                    confirmednotes += $"{note.Name} ";
                    ifskiped = false;
                    break;
                }
                else if (_note.Pitch == note.Pitch)
                {
                    ifskiped = false;
                    GrayMessageL($"processing {note.Name}");
                    while (note.Sharpness < _note.Sharpness)
                        note.EnharmonizeSharp();
                    while (note.Sharpness > _note.Sharpness)
                        note.EnharmonizeFlat();

                    affectednotes += $"{note.Name} ";
                    break;
                }                               
            }
            GrayMessageL(affectednotes);
            if (ifskiped)
                ErrorMessageL(skipednotes);
            MessageL(COLORS.standart, confirmednotes);
            return note;
        }


        public void Clear() { notes.Clear(); }
        /*
        public void Display(ChordT chordT) { if (notes.Count > 0)  Notes.ForEach(note => note.Display()); }

        */
        public void EnharmonizeToSharp(bool nodoubles = true)
        {
            if (nodoubles == true && CheckIfSharpable() == false) return;
            foreach (Note note in notes)
                note.EnharmonizeSharp();
        }

        public void EnharmonizeToFlat(bool nodoubles = true)
        {
            if (nodoubles == true && CheckIfFlatable() == false) return;
            foreach (Note note in notes)
                note.EnharmonizeFlat();
        }

        public void ExcludeDouble()
        {
            List<Note> distinctNotes = Notes.Distinct().ToList();
            Notes = distinctNotes;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            Scale other = (Scale)obj;

            return notes.SequenceEqual(other.notes);
        }

        public bool Contains(Note note) { return notes.Contains(note); }

        public int IndexOf(Note note) { return notes.IndexOf(note); }

        public void Inversion()
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

        public int Octaves()
        { return Range() / 12; }

        public void OctUp()
        {
            foreach (Note note in notes)
                note.OctUp(1);
        }

        public void OctDown()
        {
            foreach (Note note in notes)
                note.OctDown(1);
        }
        //public List<T> Permute<T>() where T : Scale, new()// генерування усіх можливих розташувань
        //{
        //    PermutationsGenerator<Note> generator = new PermutationsGenerator<Note>();

        //    List<List<Note>> permutations = generator.GeneratePermutations(notes);

        //    List<T> list = new List<T>();
        //    foreach (List<Note> chord in permutations)
        //    {
        //        T newchord = new T();
        //        //newchord.Adjust(0);
        //        list.Add(newchord);
        //    }
        //    return list;
        //}

        //public Scale[] PermuteList() // генерування усіх можливих розташувань
        //{
        //    PermutationsGenerator<Note> generator = new PermutationsGenerator<Note>();

        //    List<List<Note>> permutations = generator.GeneratePermutations(notes);

        //    Scale[] list = new Scale[permutations.Count];
        //    for (int i = 0; i < permutations.Count; i++)
        //    {
        //        Scale newchord = new Scale(permutations[i]);
        //        //newchord.Adjust(0);
        //        list[i] = newchord;
        //    }
        //    return list;
        //}


        //public static List<Scale> Permute(Scale chord) // генерування усіх можливих розташувань
        //{
        //    PermutationsGenerator<Note> generator = new PermutationsGenerator<Note>();

        //    List<List<Note>> permutations = generator.GeneratePermutations(chord.notes);

        //    List<Scale> list = new List<Scale>();
        //    foreach (List<Note> ch in permutations)
        //    {
        //        Scale newchord = new Scale(ch);
        //        //newchord.Adjust(0);
        //        list.Add(newchord);
        //    }
        //    return list;
        //}


        //public List<Scale> PermuteList() // генерування усіх можливих розташувань
        //{
        //    return PermuteToList(this);
        //}

        //public List<Scale> PermuteList(int octave = 0)
        //{
        //    return PermuteToList(this);
        //}

        //public Scale[] PermuteArray() // генерування усіх можливих розташувань
        //{

        //    return PermuteToArray(this);

        //}


        public void RemoveNote(Note note) { notes.Remove(note); }


        public void Reverse()
        { notes.Reverse(); }

        //transposed.Reverse(); return transposed;


        public int Range()
        { return notes[^1].AbsPitch() - notes[0].AbsPitch(); }


        public int Size() { return notes.Count(); }

        public float Sharpness()
        {
            float sharpness = 0;
            foreach (Note note in notes)
            {
                sharpness += sharpness_counter(note.Step, note.GetAlter());
            }
            return sharpness / Size();
        }

        public void Transpose(INTERVALS interval, QUALITY quality, DIR dir, int octave = 0)
        {
            foreach (Note note in notes)
                note.Transpose(interval, quality, dir, octave);
        }


        public void Transpose(Interval i, DIR dir)
        { Transpose(i.Interval_, i.Quality, dir); }

        public static List<Scale> Transpose(List<Scale> original, INTERVALS interval, QUALITY quality, DIR dir)
        {
            List<Scale> transposed = Clone(original);
            foreach (Scale ch in transposed)
                ch.Transpose(interval, quality, dir);
            return transposed;
        } // Clonable


        /// <summary>
        /// ////////////////TEST SECTION///////////////////
        /// </summary>

        public virtual void Display(bool octtriger = true, int color = 14)
        {
            StringOut.Display(this, octtriger, color);
        }

        public void DisplaySh(bool octtriger = true, int color = 14)
        {
            StringOut.DisplaySh(this, octtriger, color);
        }
        

        public void SortByPitch()
        {
            notes.Sort();
            
        }
        /*
        public void ShortTest()
        {
            Display();
            Play();
        }

        public void Test()
        {
            DisplayInline();
            Play();
        }
        */
        public override string ToString()
        {
            string display = " ";
            foreach (Note note in notes)
                display += (note.ToString());
            return display;
        }



        public static Scale operator +(Scale A, Scale B)
        {
            Scale C = (Scale)A.Clone();
            foreach (Note note in B.Notes)
            {
                Note temp = (Note)note.Clone();
                C.AddNote(note);
            }
            return C;
        }

        public static Scale operator +(Note A, Scale B)
        {
            Scale C = new();
            C.AddNote(A);
            foreach (Note note in B.Notes)
            {
                Note temp = (Note)note.Clone();
                C.AddNote(note);
            }

            return C;
        }


        public static bool operator ==(Scale A, Scale B)
        {
            if(A.Notes.Count!=B.Notes.Count) return false;
            for (int i = 0; i < A.Notes.Count; i++)
            {
                if (A.Notes[i] != B.Notes[i])
                    return false;
            }
            return true;
        }

        public static bool operator !=(Scale A, Scale B)
        {
            if (A.Notes.Count != B.Notes.Count) return true;
            for (int i = 0; i < A.Notes.Count; i++)
            {
                if (A.Notes[i] != B.Notes[i])
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Клонування об'єктів
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            Scale clone = new();
            // Здійснюємо глибоке клонування для елементів Scale
            clone.notes = new List<Note>(notes.Count);
            foreach (Note note in notes)
            {
                clone.notes.Add((Note)note.Clone());
            }
            return clone;
        }

        public static  List<Scale> Clone(List<Scale> original)
        {
            List<Scale> clonedlist = new();
            foreach (Scale originalScale in original)
            {
                Scale clonedScale = (Scale)originalScale.Clone();
                clonedlist.Add(clonedScale);
            }
            return clonedlist;
        }

        public static Scale[] Clone(Scale[] original)
        {
            Scale[] cloned = new Scale[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                Scale clonedScale = (Scale)original[i].Clone();
                cloned[i] = clonedScale;
            }
            return cloned;
        }

        public IEnumerator<Note> GetEnumerator()
        {
            // Return the enumerator for the notes list
            return notes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // Call the generic version
            return GetEnumerator();
        }

        public int Compare(Scale? x, Scale? y)
        {
            return x.Notes.Count().CompareTo(y.Notes.Count());
        }
    }
}
