using System.ComponentModel;
using System.Text.RegularExpressions;
using static Music.Engine;
using static Music.Globals;
using static Music.Messages;


namespace Music
{
    public class Interval
    {

        private INTERVALS interval;
        private QUALITY quality;
        private int octaves;

        public Interval() { }

        public Interval(string input)
        {
            octaves = 0;
            Match match = Regex.Match(input, @"(\d+)([a-zA-Z|+|-]+)?");
            try
            {
                if (input is null) interval = INTERVALS.PRIMA; quality = QUALITY.PERFECT;
                if (match.Success)
                {
                    if (match.Groups[1].Value == "1") interval = INTERVALS.PRIMA;
                    else if (match.Groups[1].Value == "2") interval = INTERVALS.SECUNDA;
                    else if (match.Groups[1].Value == "3") interval = INTERVALS.TERZIA;
                    else if (match.Groups[1].Value == "4") interval = INTERVALS.QUARTA;
                    else if (match.Groups[1].Value == "5") interval = INTERVALS.QUINTA;
                    else if (match.Groups[1].Value == "6") interval = INTERVALS.SEKSTA;
                    else if (match.Groups[1].Value == "7") interval = INTERVALS.SEPTYMA;
                    else if (match.Groups[1].Value == "8") interval = INTERVALS.OCTAVA;
                    else if (Convert.ToInt32(match.Groups[1].Value) > 7 || Convert.ToInt32(match.Groups[2].Value) > 7)
                    {
                        int temp = Convert.ToInt32(match.Groups[1].Value);
                        while (temp > 7) { temp -= 8; octaves++; }
                        interval = (INTERVALS)temp;
                    }
                    if (match.Groups[2].Value == "maj" || match.Groups[2].Value == "+") quality = QUALITY.MAJ;
                    else if (match.Groups[2].Value == "min" || match.Groups[2].Value == "-") quality = QUALITY.MIN;
                    else if (match.Groups[2].Value == "aug" || match.Groups[2].Value == "++") quality = QUALITY.AUG;
                    else if (match.Groups[2].Value == "dim" || match.Groups[2].Value == "--") quality = QUALITY.DIM;
                    else if (match.Groups[2].Value == "n" || match.Groups[2].Value == "=") quality = QUALITY.PERFECT;
                    if ((interval == INTERVALS.PRIMA || interval == INTERVALS.QUARTA || interval == INTERVALS.QUINTA) && quality == QUALITY.MAJ) quality = QUALITY.AUG;
                    if ((interval == INTERVALS.OCTAVA || interval == INTERVALS.QUARTA || interval == INTERVALS.QUINTA) && quality == QUALITY.MIN) quality = QUALITY.DIM;
                }
            }
            catch (IncorrectNote e) { ;
            ErrorMessage(e.Message);
            }
        }

        public Interval(INTERVALS interval, QUALITY quality)
        { this.interval = interval; this.quality = quality; }

        public Interval(int interval, int quality, int octaves = 0)
        { this.interval = (INTERVALS)interval; this.quality = (QUALITY)quality; this.octaves = octaves; }


        public Interval(Note notelow, Note notehigh)
        {
            if (notelow > notehigh) swap(ref notelow, ref notehigh);
            int steps = stepdiff(notelow.Step, notehigh.Step);
            int pitchs = pitchdiff(notelow.Pitch, notehigh.Pitch);
            quality = int_quality(steps, pitchs);
            interval = (INTERVALS)steps;
            octaves = (notehigh.AbsPitch() - notelow.AbsPitch()) / 12;
            if (Interval_ == INTERVALS.PRIMA && Quality == QUALITY.DIM) Interval_ = INTERVALS.OCTAVA;

        }

        public INTERVALS Interval_
        {
            get { return interval; }
            set { this.interval = value; }
        }

        public QUALITY Quality
        {
            get { return quality; }
            set { this.Quality = value; }
        }

        public int Steps {
            get {
                switch (interval)
                {
                    default: return 0;
                    case INTERVALS.PRIMA: return 0;
                    case INTERVALS.SECUNDA: return 1;
                    case INTERVALS.TERZIA: return 2;
                    case INTERVALS.QUARTA: return 3;
                    case INTERVALS.QUINTA: return 4;
                    case INTERVALS.SEKSTA: return 5;
                    case INTERVALS.SEPTYMA: return 6;
                }
            }
        }

        public int Octaves
        { get { return octaves; }
          set { this.octaves = value; }
        }

        public static void OctDown<T>(T chord) where T : Scale
        {
            foreach (Note nt in chord.Notes)
                nt.OctDown(1);
        }
        public static void OctUp<T>(T chord) where T : Scale
        {
            foreach (Note nt in chord.Notes)
                nt.OctUp(1);
        }

        public static void OctUp<T>(List<T> chord) where T : Scale
        {
            foreach (T ch in chord)
                OctUp(ch);
        }

        public static void OctDown<T>(List<T> chord) where T : Scale
        {
            foreach (T ch in chord)
                OctDown(ch);
        }



        public void TransposeNote(Note note, DIR dir = DIR.UP)
        {
            note.Transpose(interval, quality, dir);
        }
        public void TransposePitch(ref int pitch, ref int oct, DIR dir)
        {
            if (dir == DIR.UP) pitch = addpitch(pitch, ref oct, int_to_pitch(interval, quality));
            else pitch = addpitch(pitch, ref oct, -1 * int_to_pitch(interval, quality));
        }

        public void TransposeStep(ref int step, ref int oct, DIR dir)
        {
            if (dir == DIR.UP) step = addstep(step, ref oct, (int)interval);
            else step = addstep(step, ref oct, -1 * (int)interval);
        }
        public void TransposeStep(ref int step, DIR dir = DIR.UP)
        {
            if (dir == DIR.UP) step = addstep(step, (int)interval);
            else step = addstep(step, -1 * (int)interval);
        }

        public void TransposeStep(ref NOTES note, DIR dir = DIR.UP)
        {
            if (dir == DIR.UP) addstep(ref note, (int)interval);
            else addstep(note, -1 * (int)interval);
        }

        //public void Transpose<T>(T scale, DIR dir = DIR.UP) where T : Scale
        //{
        //    scale.Transpose(interval, quality, dir);
        //}

        public T Transpose<T>(T scale, DIR dir = DIR.UP) where T : Scale
        {
            T transposed = (T)scale.Clone();
            transposed.Transpose(interval, quality, dir, octaves);
            return transposed;
        }

        public static T Transpose<T>(T scale, int interval, int quality, DIR dir = DIR.UP, int octave = 0) where T : Scale
        {
            T transposed = (T)scale.Clone();
            transposed.Transpose((INTERVALS)interval, (QUALITY)quality, dir, octave);
            return transposed;
        }

        public static T Transpose<T>(T scale, Interval interval, DIR dir = DIR.UP) where T : Scale
        {
            T transposed = (T)scale.Clone();
            transposed.Transpose(interval, dir);
            return transposed;
        }


        public List<T> Transpose<T>(T[] scale, DIR dir = DIR.UP) where T : Scale
        {
            List<T> transposed = new();
            for (int i = 0; i < scale.Length; i++)
            {
                scale[i].Transpose(interval, quality, dir, octaves);
                transposed.Add(scale[i]);
            }
            return transposed;
        }

        public static List<T> Transpose<T>(T[] scale, int interval, int quality, DIR dir = DIR.UP) where T : Scale
        {
            List<T> transposed = new();
            for (int i = 0; i < scale.Length; i++)
            {
                scale[i].Transpose((INTERVALS)interval, (QUALITY)quality, dir);
                transposed.Add(scale[i]);
            }
            return transposed;
        }

        public static List<T> Transpose<T>(T[] scale, Interval interval, DIR dir = DIR.UP) where T : Scale
        {
            List<T> transposed = new();
            for (int i = 0; i < scale.Length; i++)
            {
                scale[i].Transpose(interval, dir);
                transposed.Add(scale[i]);
            }
            return transposed;
        }

        public List<T> Transpose<T>(List<T> scale, DIR dir = DIR.UP) where T : Scale
        {// транспонує послідовності акордів
            List<T> transposed = new();

            for (int i = 0; i < scale.Count; i++)
            {
                T temp = (T)scale[i].Clone();
                temp.Transpose(interval, quality, dir, octaves);
                transposed.Add(temp);
            }
            // StringOutput.Display(transposed);

            return transposed;
        }
        public static List<T> Transpose<T>(List<T> scale, int interval, int quality, DIR dir = DIR.UP, int octave = 0) where T : Scale
        {// транспонує послідовності акордів
            List<T> transposed = new();

            for (int i = 0; i < scale.Count; i++)
            {
                T temp = (T)scale[i].Clone();
                temp.Transpose((INTERVALS)interval, (QUALITY)quality, dir, octave);
                transposed.Add(temp);
            }
            // StringOutput.Display(transposed);

            return transposed;
        }

        public static List<T> Transpose<T>(List<T> scale, Interval interval, DIR dir = DIR.UP) where T : Scale
        {// транспонує послідовності акордів
            List<T> transposed = new();

            for (int i = 0; i < scale.Count; i++)
            {
                T temp = (T)scale[i].Clone();
                temp.Transpose(interval, dir);
                transposed.Add(temp);
            }
            // StringOutput.Display(transposed);

            return transposed;
        }


        public override string ToString()
        {
            return interval.ToString() + " " + quality.ToString() + " (" + octaves.ToString() + ")" ;

        }

        public static List<T> AllTonalities<T>(T item) where T : Scale/*, ICloneable*/
        {

            var result = new List<T>();
            foreach (var interval in allintervals)
            {
                var newItem = item.Clone() as T;
                if (newItem is not null)
                {
                    newItem.Transpose(interval.interval, interval.quality, DIR.UP);
                    result.Add(newItem);
                }
            }
            return result;
        }

        public static List<T> AllTonalities<T>(List<T> scales) where T : Scale, ICloneable
        {//помножує список акордів на 12 тональностей
            List<T> transposed = new();
            foreach (var interval in allintervals)
            {
                List<T> temp = interval.Transpose(scales);
                transposed.AddRange(temp);
            }
            return transposed;
        }
      


        //public static List<Scale> Permute(Scale chord) // генерування усіх можливих розташувань
        //{
        //    PermutationsGenerator<Note> generator = new PermutationsGenerator<Note>();

        //    List<List<Note>> permutations = generator.GeneratePermutations(chord.GetNotes());

        //    List<Scale> list = new List<Scale>();
        //    foreach (List<Note> ch in permutations)
        //    {
        //        Scale newchord = new Scale(ch);
        //        //newchord.Adjust(0);
        //        list.Add(newchord);
        //    }
        //    return list;
        //}

    }
}
