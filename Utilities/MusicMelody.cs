using Melodies25.Migrations;
using Melodies25.Utilities;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq; // added for Zip()
using System.Numerics;
using static Music.Algorythm;
using static Music.Engine;
using static Music.Globals;
using static Music.Messages;

namespace Music
{
    public class MusicMelody : Scale
    {
        // private List<Note> notes = new List<Note>();
        Random rnd = new Random();
        public int Tempo { get; set; }

        public Tonalities? Tonality;
        public MusicMelody() { }
        public MusicMelody(List<Note> nt) { this.notes = nt; }

        // КОНСТРУКТОРИ
        public MusicMelody(List<string> notes) : base(notes)
        {
            Tempo = 120;
        }

        public MusicMelody(string input) : base(input)
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
                if (LoggingManager.ReadMidi)
                    Message(COLORS.gray, "creating NoteList");
                    for (int i = 0; i < Notes.Count; i++)
                {
                    try
                    {
                        var name = Notes[i].Name;
                        list.Add(name);
                        if (LoggingManager.ReadMidi)
                            Message(COLORS.gray, name + " ");
                    }
                    catch
                    {
                        list.Add("?");
                        if (LoggingManager.ReadMidi)
                            ErrorMessage($"unable to read {Notes[i]} ");
                    }
                }                
                //MessageL(COLORS.olive, "+");
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

        public List<float> RelDurationsList
        {
            get
            {
                var list = new List<float>();
                foreach (var note in Notes)
                {
                    list.Add(note.RelDur);
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
                if (LoggingManager.ReadMidi)
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

        // АЛГОРИТМИ ПОШУКУ СПІВПАДІНЬ МЕЛОДІЙ
        static int LongestCommonSubstring(int[] A, int[] B)
        {
            return Algorythm.LongestCommonSubstring(A, B).length;
        }

        static int LongestCommonSubstring(float[] A, float[] B)
        {
            return Algorythm.LongestCommonSubstring(A, B).length;
        }

        public static MelodyMatchResult FindLongestSubstringMatch(int[] patternShape, int[] melodyShape)
        {
            var match = Algorythm.FindLongestSubstringMatch(patternShape, melodyShape);
            MelodyMatchResult result = new MelodyMatchResult(match.length, match.position, match.pairs, 0);
            return result;
        }

        public static MelodyMatchResult FindLongestSubstringMatch(float[] patternShape, float[] melodyShape)
        {
            var match = Algorythm.FindLongestSubstringMatch(patternShape, melodyShape);
            int pos = match.pairs.Count > 0 ? match.pairs[0].i1 : -1;
            var pairs = match.pairs;
            return new MelodyMatchResult(match.len, pos, pairs, 0);
        }

        public static MelodyMatchResult LongestCommonSubsequence(int[] arr1, int[] arr2, int gap)
        {
            var match = Algorythm.LongestCommonSubsequence(arr1, arr2, gap);
            int pos = match.indicesInFirst.Count > 0 ? match.indicesInFirst[0] : -1;
            var pairs = match.indicesInFirst.Zip(match.indicesInSecond, (i1, i2) => (i1, i2)).ToList();
            return new MelodyMatchResult(match.length, pos, pairs, 0);
        }

        public static MelodyMatchResult LongestCommonSubsequence(float[] arr1, float[] arr2, int gap)
        {
            var match = Algorythm.LongestCommonSubsequence(arr1, arr2, gap);
            int pos = match.indicesInFirst.Count > 0 ? match.indicesInFirst[0] : -1;
            var pairs = match.indicesInFirst.Zip(match.indicesInSecond, (i1, i2) => (i1, i2)).ToList();
            return new MelodyMatchResult(match.length, pos, pairs, 0);
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
            if (LoggingManager.ReadMidi)
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
            if (LoggingManager.ReadMidi)
            {
                if (LoggingManager.ReadMidi)
                    GrayMessageL($"generally enharmonized {count} notes");
            }
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
                if (LoggingManager.ReadMidi)
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
                    if (LoggingManager.ReadMidi)
                        GrayMessageL($"sharp jump at bestpos. {i}");
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
                if (LoggingManager.ReadMidi)                
                    GrayMessageL($"Desharp (flat) notes from {startposition} to {endposition}");
                for (int i = startposition; i <= endposition; i++)
                {
                    Notes[i].EnharmonizeFlat();
                }
                DesharpFlatTonalities();
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
                if (LoggingManager.ReadMidi)
                    GrayMessageL("unhromed end to flat");
            }
            else if (Notes[lastindex].Sharpness - Notes[lastindex - 1].Sharpness == -7)
            {
                Notes[lastindex].EnharmonizeSharp();
                if (LoggingManager.ReadMidi)
                    GrayMessageL("unhromed end to sharp");
            }
            else if (Notes[lastindex].Sharpness - Notes[lastindex - 1].Sharpness == 12)
            {
                Notes[lastindex].EnharmonizeFlat();
                if (LoggingManager.ReadMidi)
                    GrayMessageL("unhromed end to flat");
            }
            else if (Notes[lastindex].Sharpness - Notes[lastindex - 1].Sharpness == -12)
            {
                Notes[lastindex].EnharmonizeSharp();
                if (LoggingManager.ReadMidi)
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
                    if (LoggingManager.ReadMidi)
                        GrayMessageL($"correct upgoing chromatics, position {i - 2}");
                }
                else if (Notes[i - 2].Sharpness - Notes[i - 3].Sharpness == -5 &&
                    Notes[i - 1].Sharpness - Notes[i - 2].Sharpness == -5 &&
                    Notes[i].Sharpness - Notes[i - 1].Sharpness == 7)
                {
                    Notes[i - 1].EnharmonizeSharp();
                    if (LoggingManager.ReadMidi)
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
                    if (LoggingManager.ReadMidi)
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

        public void Join(MusicMelody other)
        {
            foreach (Note note in other.notes)
            {
                Notes.Add(note);
            }
        }

        public static MusicMelody Join(IList<MusicMelody> melodies)
        {
            MusicMelody newmelody = new();
            foreach (var melody in melodies)
                newmelody.Join(melody);
            return newmelody;
        }

        public new int Octaves()
        { return Range() / 12; }

        /*
                public List<MusicMelody> Permute() // генерування усіх можливих розташувань
                {
                    PermutationsGenerator<Note> generator = new();

                    var permutations = generator.GeneratePermutations(notes);

                    List<MusicMelody> list = new();
                    foreach (List<Note> chord in permutations)
                    {
                        MusicMelody newchord = new(chord);
                        //newchord.Adjust(0);
                        list.Add(newchord);
                    }
                    return list;
                }

                public new MusicMelody[] PermuteList() // генерування усіх можливих розташувань
                {
                    PermutationsGenerator<Note> generator = new();

                    List<List<Note>> permutations = generator.GeneratePermutations(notes);

                    MusicMelody[] list = new MusicMelody[permutations.Count];
                    for (int i = 0; i < permutations.Count; i++)
                    {
                        MusicMelody newchord = new(permutations[i]);
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

        // Збіг по інтервалах, а не по звуках,
        // таким чином однакові мелодії в різних тональностях розпінаються як однакові)
        public int SimilarByIntervals(MusicMelody other)
        {
            var notesThis = IntervalList.ToArray();
            var notesOther = other.IntervalList.ToArray();
            return LongestCommonSubstring(notesThis, notesOther);
        }

        public static MelodyMatchResult SimilarByIntervals(MusicMelody A, MusicMelody B)
        {
            var notesA = A.IntervalList.ToArray();
            var notesB = B.IntervalList.ToArray();
            return FindLongestSubstringMatch(notesA, notesB);
        }

        // Збіг по нотах (однакові мелодії в різних тональностях виглядатимуть як різні)
        public int SimilarByPitches(MusicMelody other)
        {
            var notesThis = Pitches.ToArray();
            var notesOther = other.Pitches.ToArray();
            return LongestCommonSubstring(notesThis, notesOther);
        }

        public static MelodyMatchResult SimilarByPitches(MusicMelody A, MusicMelody B)
        {
            var notesA = A.PitchesList.ToArray();
            var notesB = B.PitchesList.ToArray();
            return FindLongestSubstringMatch(notesA, notesB);
        }

        // Збіг за ритмічним малюнком
        public int SimilarByRythm(MusicMelody other)
        {
            var notesThis = RelDurationsList.ToArray();
            var notesOther = other.RelDurationsList.ToArray();
            return LongestCommonSubstring(notesThis, notesOther);
        }

        public static MelodyMatchResult SimilarByRhythm(MusicMelody A, MusicMelody B)
        {
            var notesA = A.RelDurationsList.ToArray();
            var notesB = B.RelDurationsList.ToArray();
            return FindLongestSubstringMatch(notesA, notesB);
        }

        public static MelodyMatchResult SimilarByBoth(MusicMelody melodyPattern, MusicMelody midiMelody)
        {
            if (melodyPattern is null || midiMelody is null)
                return new MelodyMatchResult(0, -1, new List<(int i1, int i2)>(), 0);

            var intPattern = melodyPattern.IntervalList.ToArray();
            var intMelody = midiMelody.IntervalList.ToArray();
            var durPattern = melodyPattern.RelDurationsList.ToArray();
            var durMelody = midiMelody.RelDurationsList.ToArray();

            const double EPS = 1e-3;
            var (notesCount, startPos, pairs) = FindCommonSubstringByIntervalAndDuration(intPattern, intMelody, durPattern, durMelody, EPS);

            if (notesCount == 0)
                return new MelodyMatchResult(0, -1, new List<(int i1, int i2)>(), 0);

            return new MelodyMatchResult(notesCount, startPos, pairs, 0);
        }

        // Найдовший збіг мелодій в заданій тональності з пропусками,
        // повертає кількість нот у послідовності

        public static MelodyMatchResult SimilarByIntervalsWithGap(MusicMelody pattern, MusicMelody melody, int gap)
        {
            if (pattern is null || melody is null)
                return new MelodyMatchResult(0, -1, new List<(int i1, int i2)>(), 0);

            int[] melodyNotes = melody.GetPitches().ToArray();
            int[] patternNotes = pattern.GetPitches().ToArray();

            var (bestlen, bestpos, bestpairs, bestshift) = FindBestShift(gap, melodyNotes, patternNotes);

            return new MelodyMatchResult(bestlen, bestpos, bestpairs, bestshift);
        }

        private static (int bestlen, int bestpos, List<(int i1, int i2)> bestpairs, int bestshift) FindBestShift(int gap, int[] melodyNotes, int[] patternNotes)
        {
            if (LoggingManager.PatternSearch || LoggingManager.AlgorithmDiagnostics)
                    MessageL(COLORS.olive, "\nFinding best shift for interval match with gap");
            int shift = 0;
            int bestshift = 0;
            int bestlen = 0;
            int bestpos = 0;
            var bestpairs = new List<(int i1, int i2)>();

            for (int i = 0; i < NotesInOctave; i++)
            {
                var result = LongestCommonSubsequence(melodyNotes, patternNotes, gap);
                if (LoggingManager.PatternSearch || LoggingManager.AlgorithmDiagnostics)
                    Logresults(shift, result);

                if (result.Length > bestlen)
                {
                    bestlen = result.Length;
                    bestshift = shift;
                    bestpairs = result.Pairs;
                    bestpos = result.Position;
                }
                shift++;
            }

            return (bestlen, bestpos, bestpairs, bestshift);
        }

        private static void Logresults(int shift, MelodyMatchResult result)
        {
            if (!LoggingManager.PatternSearch && !LoggingManager.AlgorithmDiagnostics)
                return;
            
                Message(COLORS.white, $"\nshift {shift}, len {result.Length}");
            foreach (var pair in result.Pairs)
            {                
                Message(COLORS.white, pair.ToString() + " ");
            }
        }

        public static MelodyMatchResult SimilarByRhythmWithGap(MusicMelody pattern, MusicMelody melody, int gap)
        {
            if (pattern is null || melody is null)
                return new MelodyMatchResult(0, -1, new List<(int i1, int i2)>(), 0);

            // Use absolute pitches for subsequence matching (keeps original behavior)
            float[] melodyNotes = melody.RelDurationsList.ToArray();
            float[] patternNotes = pattern.RelDurationsList.ToArray();

            var match = Algorythm.LongestCommonSubsequence(melodyNotes, patternNotes, gap);
            var pairs = match.indicesInFirst.Zip(match.indicesInSecond, (i1, i2) => (i1, i2)).ToList();
            var pos = match.indicesInFirst.Count > 0 ? match.indicesInFirst[0] : -1;
            return new MelodyMatchResult(match.length, pos, pairs, 0);
        }

        internal static MelodyMatchResult SimilarByBothWithGap(MusicMelody melodyPattern, MusicMelody midiMelody, int maxGap)
        {
            if (melodyPattern is null || midiMelody is null)
                return new MelodyMatchResult(0, -1, new List<(int i1, int i2)>(), 0);

            // absolute pitches and durations
            int[] melodyNotes = midiMelody.GetPitches().ToArray();
            int[] patternNotes = melodyPattern.GetPitches().ToArray();
            float[] durMelody = midiMelody.RelDurationsList.ToArray();
            float[] durPattern = melodyPattern.RelDurationsList.ToArray();

            const double EPS = 1e-3;

            var (foundLen, foundPos, foundPairs, foundShift) = FindBestShift2D(melodyNotes, patternNotes, durMelody, durPattern, maxGap, EPS);

            if (foundLen == 0)
                return new MelodyMatchResult(0, -1, new List<(int i1, int i2)>(), 0);

            return new MelodyMatchResult(foundLen, foundPos, foundPairs, foundShift);
        }

        // Найкраще транспонування за інтервалами і тривалістю з пропусками
        // повертає кількість нот у послідовності
        // eps - максимальна різниця тривалостей для вважання їх однаковими
        //  maxGap - максимальна кількість пропущених нот у обох мелодіях        
        // melodyNotes - мелодія, patternNotes - зразок        

        private static (int bestLen, int bestPos, List<(int i1, int i2)> bestPairs, int bestShift)
        FindBestShift2D(int[] melodyNotes, int[] patternNotes, float[] durMelody, float[] durPattern, int maxGap, double eps)
        {
            if (LoggingManager.PatternSearch || LoggingManager.AlgorithmDiagnostics)
                MessageL(COLORS.olive, "\nFinding best 2D shift (interval + duration) with gap");
            int clamped = Math.Clamp(maxGap, 1, 3);

            int curBestLen = 0;
            int curBestPos = -1;
            int curBestShift = 0;
            var curBestPairs = new List<(int i1, int i2)>();

            for (int shift = 0; shift < NotesInOctave; shift++)
            {
                // transpose pattern pitches (wrap within octave)
                var transposedPattern = patternNotes.Select(p => (p + shift) % NotesInOctave).ToArray();

                var (totalLen, idxFirstAll, idxSecondAll) = Algorythm.LongestCommonSubsequence(melodyNotes, transposedPattern, maxGap);
                if (idxSecondAll == null || idxSecondAll.Count == 0)
                    continue;

                // Keep only pairs where durations also match within eps
                var matchedPairs = new List<(int i1, int i2)>();
                for (int k = 0; k < idxFirstAll.Count; k++)
                {
                    int i1 = idxFirstAll[k];
                    int i2 = idxSecondAll[k];
                    if (i1 >= 0 && i1 < durMelody.Length && i2 >= 0 && i2 < durPattern.Length)
                    {
                        if (Math.Abs(durMelody[i1] - durPattern[i2]) <= eps)
                            matchedPairs.Add((i1, i2));
                    }
                }

                if (matchedPairs.Count == 0)
                    continue;

                // Cluster matched pairs into contiguous clusters where gaps in both sequences <= clamped
                var curFirst = new List<int>();
                var curSecond = new List<int>();

                void FinalizeCluster()
                {
                    if (curFirst.Count == 0) return;
                    int curLen = curFirst.Count;
                    if (curLen > curBestLen)
                    {
                        curBestLen = curLen;
                        curBestPos = curFirst[0];
                        curBestShift = shift;
                        curBestPairs = new List<(int i1, int i2)>();
                        for (int t = 0; t < curFirst.Count; t++)
                            curBestPairs.Add((curFirst[t], curSecond[t]));
                    }
                }

                for (int k = 0; k < matchedPairs.Count; k++)
                {
                    var (i1, i2) = matchedPairs[k];
                    if (curFirst.Count == 0)
                    {
                        curFirst.Add(i1);
                        curSecond.Add(i2);
                        continue;
                    }

                    int gapSecond = i2 - curSecond[^1] - 1;
                    int gapFirst = i1 - curFirst[^1] - 1;

                    if (gapSecond <= clamped && gapFirst <= clamped)
                    {
                        curFirst.Add(i1);
                        curSecond.Add(i2);
                    }
                    else
                    {
                        FinalizeCluster();
                        curFirst.Clear(); curSecond.Clear();
                        curFirst.Add(i1);
                        curSecond.Add(i2);
                    }
                }

                FinalizeCluster();

                // early exit if we matched whole pattern
                if (curBestLen >= patternNotes.Length) break;
            }

            return (curBestLen, curBestPos, curBestPairs, curBestShift);
        }

        /// <summary>
        /// ТРАНСПОНУВАННЯ МЕЛОДІЇ
        /// </summary>
        /// <param name="interval"></param> - інтервал транспонування
        /// <param name="quality"></param> - якість інтервалу
        /// <param name="dir"></param> - напрямок транспонування
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

        public static List<MusicMelody> Transpose(List<MusicMelody> original, INTERVALS interval, QUALITY quality, DIR dir)
        {
            List<MusicMelody> transposed = Clone(original);
            foreach (MusicMelody ch in transposed)
                ch.Transpose(interval, quality, dir);
            return transposed;
        }

        public bool EqualPitch(MusicMelody other)
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

        public MusicMelody Inverse()
        {
            MusicMelody newmelody = new();
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

        /// <summary>
        /// ГЕНЕРУВАННЯ ВИПАДКОВИХ МЕЛОДІЙ
        /// </summary>
        /// <param name="oct"></param>
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
        /// СТАТИСТИЧНІ
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, float>? GetStats()
        {
            var stats = new Dictionary<string, float>();

            if (Size() == 0) return null;

            float increment = 100f / Size();

            foreach (Note note in Notes)
            {
                if (!stats.ContainsKey(note.Name))  // Avoid duplicate key exception
                    stats[note.Name] = 0;
            }
            foreach (Note note in Notes)
            {
                stats[note.Name] += increment;
            }
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
            }
            foreach (Note note in Notes)
            {
                stats[note.Name] += (float)note.AbsDuration() * 100 / AbsLength;
            }

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
        public static void DisplayTable(List<MusicMelody> list)
        {
            //foreach (MusicMelody ch in list)
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

        public static void DisplayInline(List<MusicMelody> list)
        {
            foreach (MusicMelody ch in list)
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

        public static void Test(List<MusicMelody> list)
        {
            foreach (MusicMelody ch in list)
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
            MusicMelody clone = new();
            // Здійснюємо глибоке клонування для елементів MusicMelody
            clone.notes = new List<Note>(this.notes.Count);
            foreach (Note note in this.notes)
            {
                clone.notes.Add((Note)note.Clone());
            }
            return clone;
        }

        public static List<MusicMelody> Clone(List<MusicMelody> original)
        {
            List<MusicMelody> clonedlist = new();
            foreach (MusicMelody originalMelody in original)
            {
                MusicMelody clonedMelody = (MusicMelody)originalMelody.Clone();
                clonedlist.Add(clonedMelody);
            }
            return clonedlist;
        }

        public static MusicMelody[] Clone(MusicMelody[] original)
        {
            MusicMelody[] cloned = new MusicMelody[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                MusicMelody clonedMelody = (MusicMelody)original[i].Clone();
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

        public static MusicMelody CreateRandom(int length, int octaves)
        {
            MusicMelody melody = new MusicMelody();

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
        public void SaveMidi(string filepath = "output.mid")
        {
            MidiConverter.SaveMidi(this, filepath);
        }
    }

    public struct MelodyMatchResult
    {
        public int Length;
        public int Position;
        public List<(int i1, int i2)> Pairs;
        public int BestShift;
        public MelodyMatchResult(int length, int position, List<(int i1, int i2)> pairs, int bestShift)
        {
            Length = length;
            Position = position;
            Pairs = pairs;
            BestShift = bestShift;
        }
    }
}
