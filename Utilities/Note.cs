﻿using static Music.Engine;
using static System.Console;
using static System.Convert;
using static Music.Globals;
using static Music.Messages;
using System.Diagnostics.Metrics;
using NAudio.Midi;

namespace Music
{

    public class Note : ICloneable, IComparable
    {

        private int pitch; // висота у півтонах від "до" = 0
        private int step;// висота у ступенях від "до" = 0
        private int oct; // октава (1 - перша октава)
        private Duration duration;
        private bool rest;

        public int Pitch
        {
            get { return pitch; }
            set { pitch = value; }
        }

        public int Step
        {
            get { return step; }
            set { step = value; }
        }

        public int Oct
        {
            get { return oct; }
            set { oct = value; }
        }

        public int Alter { get { return pitch_to_alter(step, pitch); } }

        
        public int MidiNote { get { return AbsPitch() + GMCorrection; } }
        // 1-а октава відповідає 4-й MIDI-октаві, нумерація MIDI-октав з нуля

        // розташування на квінтовому колі. D = 0.
        public float Sharpness { get { return sharpness_counter(step, Alter); } }
        
        public int MidiDur { get { return duration.MidiDuration(PPQN); } }

        public string DurSymbol
        {
            get { return duration.Symbol(rest); }
        }

        public (string, string) DurName
        {
            get {
                return (duration.Symbol(rest), GetName());                  
            }
        }

        public string Name
        {
            get
            {
                if (!rest)
                    return pitch_to_notename(step, pitch).Replace("b", "♭");
                else
                    return "-";
            }
        }



        public Note(Note note)
        {
            pitch = note.pitch;
            step = note.step;
            oct = note.oct;
            duration = note.duration;
            rest = note.rest;
        }

        public Note(int pitch, int step)
        {
            this.pitch = pitch; this.step = step; oct = 1; duration = new Duration(); rest = false;

        }
        public Note(int pitch, int step, int oct)
        {
            this.pitch = pitch; this.step = step; this.oct = oct; duration = new Duration(); rest = false;

        }
        public Note(int pitch, int step, int oct, int duration)
        {
            this.pitch = pitch; this.step = step; this.oct = oct; this.duration = new Duration(duration); rest = false;
        }

        public Note(int pitch, int step, int oct, Duration duration)
        {
            this.pitch = pitch;
            this.step = step;
            this.oct = oct;
            this.duration = duration;
            rest = false;
        }

        public Note(int pitch, int step, int oct, Duration duration, bool rest)
        {
            this.pitch = pitch;
            this.step = step;
            this.oct = oct;
            this.duration = duration;
            this.rest = rest;
            rest = false;
        }

        public Note(NOTES note, ALTER alter)
        {
            step = (int)note;
            pitch = standartpitch_from_step(step) + (int)alter;
            oct = 1; duration = new Duration();
            rest = false;
        }

        public Note(NOTES note, ALTER alter, int oct)
        {
            step = (int)note;
            pitch = standartpitch_from_step(step) + (int)alter;
            this.oct = oct; duration = new Duration();
            rest = false;
        }

        public Note(NoteOnEvent noteEvent)
        {
            pitch = noteEvent.NoteNumber % NotesInOctave;
            step = pitch_to_step_alter(pitch).Item1;
            oct = noteEvent.NoteNumber / NotesInOctave - GMOctaveCorrection;
            rest = false;
        }

        public void EnterNote(string input)
        {//створення ноти за буквою
            input = CutSlash(input);
            int octave; string key;
            octdivide(input, out octave, out key);
            int temp = key_to_pitch(key);
            if (temp == -100) { WriteLine("ERROR"); ReadKey(); throw new IncorrectNote(); }
            else pitch = temp;
            step = key_to_step(key);
            oct = octave;
            duration = new Duration();
        }

        private static string CutSlash(string input)
        {
            var slash = input.IndexOf('/');
            if (slash != -1)
            {
                input = input.Substring(slash);
            }

            return input;
        }

        public Note GenerateRandomNote()
        {
            var rnd = new Random();
            int step = rnd.Next(7);
            int alter = rnd.Next(2) - 1;
            return new Note(step, alter);
        }


        public static Note GenerateRandomDistinctNote(Note note)
        {
            var rnd = new Random();
            while (true)
            {
                int step = rnd.Next(6);
                int alter = rnd.Next(2) - 1;
                var newnote = new Note((NOTES)step, (ALTER)alter);
                if (!newnote.Equals(note))
                    return newnote;
            }
        }

        
        public static Note GenerateRandomNote(int oct)
        {
            var rnd = new Random();
            int step = rnd.Next(6);
            int alter = rnd.Next(3);

            return new Note((NOTES)step, (ALTER)alter)
            {
                oct = rnd.Next(oct)
            };
        }


        public static Note GenerateRandomDistinctNote(Melody melody)
        {
            var rnd = new Random();
            int counter = 0;
            while (counter < 10000)
            {
                counter++;
                bool distinct = true;
                int step = rnd.Next(6);
                int alter = rnd.Next(3) - 1;
                var newnote = new Note((NOTES)step, (ALTER)alter);
                foreach (var note in melody)
                    if (newnote.EqualPitch(note))
                    {
                        distinct = false;
                        break;
                    }
                if (distinct) return newnote;
                
            }
            throw new Exception("no distinct note found");
        }

        public void SetRandomDuration()
        {
            Random rnd = new Random();
            int dur = (int)Math.Pow(2, rnd.Next(5));
            duration = new(dur);
        }


        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            Note note = (Note)obj;
            return Pitch == note.Pitch && Step == note.Step && Oct == note.Oct;
        }

        //співпадає ступінь і звуковисотність
        public bool EqualDegree(Note obj)
        {
            Note note = (Note)obj;
            //GrayMessageL($"{Step} vs {obj.Step} and {Pitch} vs {obj.Pitch}");
            return Pitch == note.Pitch && Step == note.Step;
        }

        public bool EqualPitch(Note obj)
        {
            Note note = (Note)obj;
            return Pitch == note.Pitch;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Pitch.GetHashCode();
                hash = hash * 23 + Step.GetHashCode();
                hash = hash * 23 + Oct.GetHashCode();
                return hash;
            }
        }


        public Note(string input)        {            

            if (input is null) throw new IncorrectNote("Impossible to initialize note");
            input = CutSlash(input); // при пошуку типу cis/des
            stringdivider(input, out string key, out int octave, out int duration, out string? durmodifier);
            if (key == "r") MakeRest();
            else
            {

                pitch = key_to_pitch(key, true);
                step = key_to_step(key);
                oct = octave;
            }
            try
            {
                this.duration = new Duration(duration, durmodifier);
            }
            catch (Exception ex)
            {
                this.duration = new Duration(DURATION.quater);
                ErrorMessage("Possible incorrect duration");
            }
        }
        

        private void MakeRest()
        {
            rest = true;
            pitch = 0;
            step = 0;
            GrayMessageL("a rest found");
        }

        public int GetAlter() { return pitch_to_alter(step, pitch); }

        public int AbsDuration() { return duration.AbsDuration(); }
        public int AbsPitch()
        {
            if (rest == true) return -1; 
            if (pitch - step > 10) return pitch + (oct - 2) * NotesInOctave; // для до-бемоля і іншої дубль-бемольної екзотики
            if (step - pitch > 5) return pitch + oct * NotesInOctave;
            else return pitch + (oct - 1) * NotesInOctave;
        }


        public bool CheckIfFlatable()
        {
            return Sharpness >= 2;
        }
        public bool CheckIfSharpable()
        {
            return Sharpness <= -2;
        }

        public double PrintDuration
        {
            get { return duration.RelDuration(); }
            set { duration.Dur = (DURATION)value; }
        }

        public Duration Duration { get => duration; set => duration = value; }
        public bool Rest { get => rest; set => rest = value; }

        public void EnharmonizeSharp()
        {
            step = addstep(step, ref oct, -1);

        }

        public void EnharmonizeFlat()
        { step = addstep(step, ref oct, 1); }


        public void EnharmonizeSmart()
        {
            if (Sharpness > 6) step = addstep(step, ref oct, 1);
            else if (Sharpness < -6) step = addstep(step, ref oct, -1);
            else;
        }

        public void EnharmonizeDoubles()
        {
            if (Sharpness > 10) step = addstep(step, ref oct, 1);
            else if (Sharpness < -10) step = addstep(step, ref oct, -1);
        }

        public bool IfSoprano()
        {
            return AbsPitch() >= SopranoL.AbsPitch() && AbsPitch() <= SopranoH.AbsPitch();
        }

        public bool IfAlto()
        {
            return AbsPitch() >= AltoL.AbsPitch() && AbsPitch() <= AltoH.AbsPitch();
        }

        public bool IfTenor()
        {
            return AbsPitch() >= TenorL.AbsPitch() && AbsPitch() <= TenorH.AbsPitch();
        }

        public bool IfBase()
        {
            return AbsPitch() >= BaseL.AbsPitch() && AbsPitch() <= BaseH.AbsPitch();
        }


        public string GetName() {
            if (!rest)
                return pitch_to_notename(step, pitch).Replace("b", "♭");
            else
                return rest();
        }

        public int Octave() { return Oct; }
        public void OctUp(int num = 1) { oct += num; }

        public void OctDown(int num = 1) { oct -= num; }

        public static int PitchSort(Note a, Note b)
        {
            if (a.AbsPitch() > b.AbsPitch())
                return 1;
            else if (a.AbsPitch() == b.AbsPitch())
                return 0;
            else return -1;
        }

        

        // Транспозиція //
        public void Transpose(INTERVALS interval, QUALITY quality, int octave = 0)
        {
            step = addstep(step, ref oct, (int)interval);
            pitch = addpitch(pitch, /*ref oct,*/ int_to_pitch(interval, quality));
            oct += octave;
        }

        public Note TransposeToNote(INTERVALS interval, QUALITY quality, int octave = 0)
        {
            var note = (Note)Clone();
            note.step = addstep(step, ref oct, (int)interval);
            note.pitch = addpitch(pitch, /*ref oct,*/ int_to_pitch(interval, quality));
            note.oct += octave;
            return note;
        }

        public void Transpose(int interval, int quality, int octave = 0)
        {
            step = addstep(step, ref oct, interval);
            pitch = addpitch(pitch, int_to_pitch((INTERVALS)interval, (QUALITY)quality));//вдруге октаву не зсуваємо!
            oct += octave;
        }

        public void Transpose(INTERVALS interval, QUALITY quality, DIR dir, int octave = 0)
        {
            if (dir == DIR.UP)
            {
                step = addstep(step, ref oct, (int)interval);//якщо відбувається перехід в іншу октаву
                pitch = addpitch(pitch, int_to_pitch(interval, quality));//вдруге октаву не зсуваємо!
                oct += octave; // якщо інтервал більший за октаву
            }
            else
            {
                step = addstep(step, ref oct, -1*(int)interval);//якщо відбувається перехід в іншу октаву
                pitch = addpitch(pitch, -1*int_to_pitch(interval, quality));//вдруге октаву не зсуваємо!
                oct-= octave;   // якщо інтервал більший за октаву
            }
        }

        public void Transpose(Interval i)
        { Transpose(i.Interval_, i.Quality, i.Octaves); }


        public int DisplayWidth(int minWidth, int displayRange)
        {            
            return minWidth + (int)(duration.RelDuration() * displayRange); 
        }

        public void Transpose(Interval i, DIR dir)
        { Transpose(i.Interval_, i.Quality, dir, i.Octaves); }


        /// <summary>
        /// /DISPLAYING
        /// </summary>
        /*
        public void Display()
        { StringOutput.Display(this); }
        public void DisplayTable()
        { WriteLine(GetName() + "\npitch = " + ToInt32(Pitch) + "\noctave = " + ToInt32(Oct) + 
            "\nduration = " + duration + "\nMidiTicks(PPQN-480) = " + duration.MidiDuration(480) +
            "\nabspitch= " + AbsPitch() + "\nMidiNote= " + MidiNote  + " \nfreq = " + Pitch_to_hz(AbsPitch())); }

        public void DisplayInline()
        { StringOutput.DisplayInline(this); }


        public void Play()
        {
            //if (Pitch_to_hz(AbsPitch()) < 37) throw new IncorrectNote("impossible to playm pitch: " + Pitch + " octave: " + Oct + "\n");
            if (player == PLAYER.beeper)
                Beeper.Play(this);
            else if (player == PLAYER.naudio)
                NAPlayer.Play(this);
            else if (player == PLAYER.midiplayer)
                MidiFile0.Play(this);
        }*/

        public int SortByPitch(Note a, Note b)
        {
            return a.CompareTo(b);
        }

        public override string ToString()
        {
            return pitch_to_notename(step, pitch) + " (" + Oct + ") "; 

        }
        public object Clone()
        {
            Note clone = new(pitch, step, oct, (Duration)duration.Clone(), rest);
            return clone;
        }

        public int CompareTo(object? obj)
        {
            if (obj is Note other)
            {
                if (AbsPitch() > other.AbsPitch()) return 1;
                else if (AbsPitch() < other.AbsPitch()) return -1;
                else if (Step == 6 && other.Step == 0) return -1;
                else if (Step == 0 && other.Step == 6) return 1;
                else if (Step < other.Step) return -1;
                else if (Step > other.Step) return 1;
                else return 0;
            }
            else throw new ArgumentException("Object is not of type Note");
        }

        internal void SetDuration(int mididur, int tickperquater)
        {
            try
            {
                var duration = new Duration(mididur, tickperquater);
                Duration = duration;
            }
            catch (Exception e)
            {
                ErrorMessage(e.Message);
            }
        }


        internal void SetDuration(int dur)
        {
            try
            {
                var duration = new Duration(dur);
                Duration = duration;
            }
            catch (Exception e)
            {
                ErrorMessage(e.Message);
            }

        }

        public static bool operator ==(Note a, Note b)
        {
           // if (b is null || a is null) return false;
            return a.Step == b.Step && a.Pitch == b.Pitch && a.Oct == b.Oct;

        }

        public static bool operator !=(Note a, Note b)
        {
            //if (b is null || a is null) return true; 
            return a.Step != b.Step || a.Pitch != b.Pitch || a.Oct != b.Oct;

        }

        public static bool operator > (Note a, Note b)
        {
            if (a.CompareTo(b) == 1)
                return true;
            else return false;

        }

        public static bool operator < (Note a, Note b)
        {
            if (a.CompareTo(b) == -1)
                return true;
            else return false;

        }

        //public string GetJSON()
        //{
        //    return JsonConvert.SerializeObject(this);
        //}





    };


}

