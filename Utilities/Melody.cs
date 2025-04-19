﻿using Melodies25.Migrations;
using NAudio.Midi;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using static Music.Engine;
using static Music.Globals;
using static Music.Messages;

namespace Music
{
    public class Melody : Scale
    {
        // private List<Note> notes = new List<Note>();
        Random rnd = new Random();
        public int Tempo { get; set; }

        public Tonalities? Tonality;
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


        //повертає мелодичний малюнок по інтервалах
        public List<int> IntervalList
        {
            get
            {
                List<int> list = new List<int>();
                for (int i = 1; i < Notes.Count; i++)
                {
                    try
                    {
                        var interval = Notes[i].AbsPitch() - Notes[i - 1].AbsPitch();
                        list.Add(interval);
                    }
                    catch
                    {
                        list.Add(0);
                        ErrorMessage($"unable to read {Notes[i]} or {Notes[i - 1]} ");
                    }
                }
                return list;
            }
        }
        //повертає список нот як індекс висоти звуку
        public List<int> PitchesList
        {
            get
            {
                List<int> list = new List<int>();
                for (int i = 0; i < Notes.Count; i++)
                {
                    try
                    {
                        var pitch = Notes[i].AbsPitch();
                        list.Add(pitch);
                    }
                    catch
                    {
                        list.Add(-1);
                        ErrorMessage($"unable to read {Notes[i]} ");
                    }
                }
                return list;
            }
        }

        //повертає список нот поіменно
        public List<string> NotesList
        {
            get
            {
                var list = new List<string>();
                Message(COLORS.gray, "creating NoteList");
                for (int i = 0; i < Notes.Count; i++)
                {
                    try
                    {
                        var name = Notes[i].Name;
                        list.Add(name);
                        //Message(COLORS.gray, name + " ");
                    }
                    catch
                    {
                        list.Add("?");
                        ErrorMessage($"unable to read {Notes[i]} ");
                    }
                }
                MessageL(COLORS.olive, "+");
                return list;
            }
        }

        public List<float> SharpnessList
        {
            get
            {
                var list = new List<float>();
                foreach (var note in Notes)
                {
                    list.Add(note.Sharpness);
                }
                return list;
            }
        }

        public List<(string, string)> DurName
        {
            get
            {
                var list = new List<(string, string)>();
                foreach (var note in Notes)
                    list.Add(note.DurName);
                return list;
            }
        }

        public string NotesString
        {
            get
            {
                MessageL(COLORS.olive, "\ngetting AllNotes list");
                string list = "";
                foreach (var note in Notes)
                {
                    try
                    {
                        list += note.Name;
                        list += "  ";
                    }
                    catch
                    {
                        list += " ? ";
                    }
                }
                return list;
            }
        }

        public string SharpnessString
        {
            get
            {
                string list = "";
                foreach (var note in Notes)
                {
                    list += note.Sharpness;
                    list += " ";

                }
                return list;
            }
        }

        public string IntervalString
        {
            get
            {
                string list = "";
                foreach (var interval in IntervalList)
                {
                    list += interval.ToString();
                    list += " ";

                }
                return list;
            }
        }

        public int AbsLength
        {
            get
            {
                int length = 0;
                foreach (var note in Notes)
                {
                    length += note.AbsDuration();

                }
                return length;
            }

        }


        //Чи починається з ноти
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

        // Найдовше співпадіння мелодій (перевіряє по інтервалах, а не по звуках,
        // таким чином однакові мелодії в різних тональностях розпінаються як однакові)
        public int LongestCommonSubstring(Melody other)
        {
            var notesThis = IntervalList.ToArray();
            var notesOther = other.IntervalList.ToArray();
            return LongestCommonSubstring(notesThis, notesOther).Count;
        }

        // Найдовше співпадіння мелодій в заданій тональності,
        // повертає кількість нот у послідовності
        public int LongestAbsoulteCommonSubstring(Melody other)
        {
            var notesThis = Pitches.ToArray();
            var notesOther = other.Pitches.ToArray();
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

        public void Enharmonize()
        {//бета-версія

            if (Tonality is not null)
                EnharmonizeToTonality();
            else
            {
                // виправлення MIDI-знаків альтерації відповідно
                // до тональної логіки

                EnharmonizeCommon(); // загальний для зменшених інтервалів
                Desharp(); // утворені зайві дієзи 
                Desharp();// утворені зайві дієзи 
                DesharpFlatTonalities();// дієзні послідовності посеред бемолів
                UnDoubleFlat(); // утворені зайві дубль-бемолі
                UnChromEnd(); // хроматизми наприкінці
                UpChromatics(); // висхідна хроматика            
                AfterEffectUnflat(); // артефакти попередніх
            }

        }

        private void EnharmonizeToTonality()
        {
            if (Tonality is null) return;
            MessageL(COLORS.olive, "Enharmonize to tonality");
            var scale = Tonality.NotesInTonalityExtended();
            for (int i = 0; i < Size(); i++)
            {
                if (Tonality is not null)
                    Notes[i] = TryMakeToScale(scale, Notes[i]);
            }
        }

        public void EnharmonizeCommon()
        {
            int count = 0;

            for (int i = 1; i < Size() - 1; i++)
            {
                if (Notes[i].Sharpness - Notes[i - 1].Sharpness > 6 &&
                    Notes[i + 1].Sharpness - Notes[i].Sharpness <= 0)
                { Notes[i].EnharmonizeFlat(); count++; }
                else if (Notes[i].Sharpness - Notes[i - 1].Sharpness < -6 &&
                    Notes[i + 1].Sharpness - Notes[i].Sharpness >= 0)
                { Notes[i].EnharmonizeSharp(); count++; }
            }
            GrayMessageL($"generally enharmonized {count} notes");
        }

        //якщо надто дієзна тональність фрагменту
        public void Desharp()
        {
            int startsharprow = 0;
            int doublesharpposition = 0;
            int endsharprow = 0;


            for (int i = 1; i < Size() - 1; i++)
            {

                if (Notes[i].Sharpness < 3 && doublesharpposition == 0)
                {
                    startsharprow = 0;
                    continue;
                }
                if (Notes[i].Sharpness > 3 && startsharprow == 0)
                    startsharprow = i;

                if (notes[i].Sharpness > 8)
                {
                    doublesharpposition = i;
                }
                if (Notes[i].Sharpness < 4 && doublesharpposition > 0)
                {
                    endsharprow = i;
                    break;
                }
            }

            if (startsharprow > 0 && doublesharpposition >= startsharprow && endsharprow >= doublesharpposition)
            {
                Console.WriteLine($"Desharp notes from {startsharprow} to {endsharprow}");
                for (int j = startsharprow; j < endsharprow; j++)
                    Notes[j].EnharmonizeFlat();
            }
        }

        //великі дієзні фрагменти посеред бемолів
        public void DesharpFlatTonalities()
        {
            int startposition = 0;
            int endposition = 0;

            for (int i = 1; i < Size(); i++)
            {

                if (Notes[i].Sharpness - Notes[i - 1].Sharpness >= 7 && Notes[i].Sharpness > 0)
                {
                    GrayMessageL($"sharp jump at pos. {i}");
                    startposition = i;
                }

                if (startposition > 0)
                    if (Notes[i].Sharpness < 4)
                    {
                        startposition = 0;
                        continue;
                    }


                if (Notes[i].Sharpness - Notes[i - 1].Sharpness <= -4 || i == (Size() - 1) /*&& Notes[i].Sharpness > 2*/)
                {
                    endposition = i;
                    break;
                }
            }
            if (startposition > 0 && endposition > startposition)
            {
                {
                    GrayMessageL($"Desharp (flat) notes from {startposition} to {endposition}");
                    for (int i = startposition; i <= endposition; i++)
                    {

                        Notes[i].EnharmonizeFlat();
                    }
                    DesharpFlatTonalities();
                }

            }
        }


        public void UnDoubleFlat()
        {
            int lastindex = Size() - 1;
            int startposition = 0;
            for (int i = 1; i < Size() - 1; i++)
            {
                if (Notes[i].Sharpness - Notes[i - 1].Sharpness == 7)
                    startposition = i;

            }
        }



        public void UnChromEnd()
        {
            int lastindex = Size() - 1;
            if (Notes[lastindex].Sharpness - Notes[lastindex - 1].Sharpness == 7)
            {
                Notes[lastindex].EnharmonizeFlat();
                GrayMessageL("unhromed end to flat");

            }
            else if (Notes[lastindex].Sharpness - Notes[lastindex - 1].Sharpness == -7)
            {
                Notes[lastindex].EnharmonizeSharp();
                GrayMessageL("unhromed end to sharp");
            }
            else if (Notes[lastindex].Sharpness - Notes[lastindex - 1].Sharpness == 12)
            {
                Notes[lastindex].EnharmonizeFlat();
                GrayMessageL("unhromed end to flat");
            }
            else if (Notes[lastindex].Sharpness - Notes[lastindex - 1].Sharpness == -12)
            {
                Notes[lastindex].EnharmonizeSharp();
                GrayMessageL("unhromed end to sharp");
            }
        }


        public void UpChromatics()
        {
            for (int i = 3; i < Size(); i++)
            {
                if (Notes[i - 3].Sharpness > 5) continue;
                if (Notes[i - 2].Sharpness - Notes[i - 3].Sharpness == -5 &&
                    Notes[i - 1].Sharpness - Notes[i - 2].Sharpness == 7 &&
                    Notes[i].Sharpness - Notes[i - 1].Sharpness == -5)
                {
                    Notes[i - 2].EnharmonizeSharp();
                    GrayMessageL($"correct upgoing chromatics, position {i - 2}");
                }
                else if (Notes[i - 2].Sharpness - Notes[i - 3].Sharpness == -5 &&
                    Notes[i - 1].Sharpness - Notes[i - 2].Sharpness == -5 &&
                    Notes[i].Sharpness - Notes[i - 1].Sharpness == 7)
                {
                    Notes[i - 1].EnharmonizeSharp();
                    GrayMessageL($"correct upgoing chromatics 2, position {i - 2}");
                }
            }

        }

        public void AfterEffectUnflat()
        {
            for (int i = 1; i < Size() - 1; i++)
            {
                if (Notes[i].Sharpness - Notes[i - 1].Sharpness == 10)
                {
                    Notes[i - 1].EnharmonizeSharp();
                    GrayMessageL($"unflat note {i}");
                }
            }
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

            if (note == Notes[^1]) return;
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
        // EnharmonizeCommon to avoid double accidentals
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


        public Dictionary<string, float>? GetStats()
        {
            var stats = new Dictionary<string, float>();

            if (Size() == 0) return null;

            float increment = 100f / Size();

            foreach (Note note in Notes)
            {
                if (!stats.ContainsKey(note.Name))  // Avoid duplicate key exception
                    stats[note.Name] = 0;
            };
            foreach (Note note in Notes)
            {
                stats[note.Name] += increment;    
            };
            // values * 100 і округлити до одного знака

            stats = stats.ToDictionary(pair => pair.Key, pair => (float)Math.Round(pair.Value, 1));

            return stats.OrderByDescending(x => x.Value)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

        }

        public Dictionary<string, float>? GetWeight()
        {
            var stats = new Dictionary<string, float>();

            if (AbsLength == 0) return null;

            foreach (Note note in Notes)
            {
                if (!stats.ContainsKey(note.Name))  // Avoid duplicate key exception
                    stats[note.Name] = 0;
            };
            foreach (Note note in Notes)
            {

                stats[note.Name] += (float)note.AbsDuration() * 100 / AbsLength;
            };

            stats = stats.ToDictionary(pair => pair.Key, pair => (float)Math.Round(pair.Value, 1));

            return stats.OrderByDescending(x => x.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

        }

        public Dictionary<string, float>? GetDegreesStats()
        {
            if (Tonality is not null && Notes.Count > 0)
            {
                return Tonalities.DegreeStats(Notes, Tonality);
            }
            else return null;
        }

        public Dictionary<string, float>? GetDegreesWeightStats()
        {
            if (Tonality is not null && Notes.Count > 0)
            {
                return Tonalities.DegreeWeightStats(Notes, Tonality);
            }
            else return null;
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

        internal MidiEventCollection ConvertToMIDI()
        {

            var collection = new MidiEventCollection(0, PPQN);
            int currenttime = 0;
            var tempoEvent = new TempoEvent(Tempo, 0);
            collection.AddEvent(tempoEvent, 0);

            foreach (var note in Notes)
            {
                var neOn = new NoteEvent(currenttime, 1, MidiCommandCode.NoteOn, note.MidiNote, 100);
                var neOff = new NoteEvent(currenttime + note.MidiDur, 1, MidiCommandCode.NoteOff, note.MidiNote, 100);                
                collection.AddEvent(neOn, 1);
                collection.AddEvent(neOff, 1);
                currenttime += note.MidiDur;
            }

            return collection;
        }

        public static Melody CreateRandom(int length, int octaves)
        {
            Melody melody = new Melody();            

            while (melody.Notes.Count < length)
            {
                try
                {
                    var newnote = Note.GenerateRandomNote(octaves);
                    melody.AddNote(newnote);
                }
                catch (Exception e) { ErrorMessage(e.Message); }
            }
            melody.EnharmonizeSmart();
            return melody;
        }
 public void SaveMidi(string filepath="output.mid")
        {
            MidiConverter.SaveMidi(this, filepath);
        }
    }
}
