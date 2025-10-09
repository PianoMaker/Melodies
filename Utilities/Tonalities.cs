using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using static Music.Engine;
using static Music.Messages;
using static Music.Globals;
using static System.Console;
using NAudio.Midi;

namespace Music
{

    //Тональність
    public class Tonalities : ICloneable
    {
        private NOTES step;
        private ALTER alter;// знак альтерації (у назві тональності)
        private MODE mode; // лад


        public Tonalities(NOTES key, ALTER alter, MODE mode)
        { this.step = key; this.mode = mode; this.alter = alter; }

        public Tonalities(string key, MODE mode)
        {
            int step = key_to_step(key);
            this.step = (NOTES)step;
            alter = (ALTER)alter_from_pitch(step, key_to_pitch(key));
            this.mode = mode;
        }

        public Tonalities(Note note, MODE mode)
        {
            this.step = (NOTES)note.Step;             
            alter = (ALTER)note.GetAlter();
            this.mode = mode;
        }

        public Tonalities(int sharpflat, int majorminor)
        {
            MODE mode = (MODE)majorminor;
            GetTonalityFromKeySignature(sharpflat, mode);
        }

        public Tonalities(KeySignatureEvent ks)
        {
            int sharpflat = (int)ks.SharpsFlats;
            MODE mode = (MODE)ks.MajorMinor;
            GetTonalityFromKeySignature(sharpflat, mode);
        }

        private void GetTonalityFromKeySignature(int sharpflat, MODE mode)
        {
            if (mode == MODE.dur)
            {
                this.mode = MODE.dur;
                this.alter = ALTER.NATURAL;
                switch (sharpflat)
                {

                    case 0: this.step = NOTES.DO; break;
                    case 1: this.step = NOTES.SOL; break;
                    case 2: this.step = NOTES.RE; break;
                    case 3: this.step = NOTES.LA; break;
                    case 4: this.step = NOTES.MI; break;
                    case 5: this.step = NOTES.SI; break;
                    case 6: this.step = NOTES.FA; this.Alter = ALTER.SHARP; break;
                    case 7: this.step = NOTES.DO; this.Alter = ALTER.SHARP; break;
                    case -1: this.step = NOTES.FA; break;
                    case -2: this.step = NOTES.SI; alter = ALTER.FLAT; break;
                    case -3: this.step = NOTES.MI; alter = ALTER.FLAT; break;
                    case -4: this.step = NOTES.LA; alter = ALTER.FLAT; break;
                    case -5: this.step = NOTES.RE; alter = ALTER.FLAT; break;
                    case -6: this.step = NOTES.SOL; alter = ALTER.FLAT; break;
                    case -7: this.step = NOTES.DO; alter = ALTER.FLAT; break;
                }
            }
            else if (mode == MODE.moll)
            {
                this.mode = MODE.moll;
                this.alter = ALTER.NATURAL;
                switch (sharpflat)
                {

                    case 0: this.step = NOTES.LA; break;
                    case 1: this.step = NOTES.MI; break;
                    case 2: this.step = NOTES.SI; break;
                    case 3: this.step = NOTES.FA; this.Alter = ALTER.SHARP; break;
                    case 4: this.step = NOTES.DO; this.Alter = ALTER.SHARP; break;
                    case 5: this.step = NOTES.SOL; this.Alter = ALTER.SHARP; break;
                    case 6: this.step = NOTES.RE; this.Alter = ALTER.SHARP; break;
                    case 7: this.step = NOTES.LA; this.Alter = ALTER.SHARP; break;
                    case -1: this.step = NOTES.RE; break;
                    case -2: this.step = NOTES.SOL; break;
                    case -3: this.step = NOTES.DO; break;
                    case -4: this.step = NOTES.FA; break;
                    case -5: this.step = NOTES.SI; alter = ALTER.FLAT; break;
                    case -6: this.step = NOTES.MI; alter = ALTER.FLAT; break;
                    case -7: this.step = NOTES.LA; alter = ALTER.FLAT; break;
                }
            }
        }

        public Tonalities(int step, int pitch, MODE mode)
        {
            this.step = (NOTES)step;
            alter = (ALTER)alter_from_pitch(step, pitch);
            this.mode = mode;

        }

        //input вводити в європейській нотації як "c-dur" "es-moll" і т.п.
        public Tonalities(string input)
        {

            string name = EnterTonalityName(input);
            string[] tokens = name.Split('-', ' '); 
            string key = tokens[0]; 
            string tempmode = tokens.Length > 1 ? tokens[1] : "dur"; 

            step = (NOTES)key_to_step(key); 
            int pitch = key_to_pitch(key); 
            alter = (ALTER)pitch_to_alter(step, pitch); 
            mode = (tempmode == "dur") ? MODE.dur : MODE.moll; 

        }

        public NOTES Key { get { return step; } set { step = value; } }

        public ALTER Alter
        {
            get { return alter; }
            set { alter = value; }
        }

        public Note GetNote()
        {
            Note note = new Note(step, alter);
            return note;
        }

        public int Step()
        {
            return (int)step;
        }
        public MODE Mode { get { return mode; } set { mode = value; } }

        public string EnterTonalityName(string tonality)
        {
            if (tonality.Length < 4)
                throw new IncorrectNote("impossible to determint input: " + tonality);

            // Завжди парсимо тональності у європейській нотації (es/is, H/B)
            notation = Notation.eu;

            // Нормалізація тире (en/em/minus) і пробілів перед зниженням регістру
            tonality = tonality.Trim()
                .Replace('\u2013', '-') // en-dash
                .Replace('\u2014', '-') // em-dash
                .Replace('\u2212', '-') // minus sign
                .Replace('–', '-')      // safety
                .Replace('—', '-')      // safety
                .Replace('−', '-')      // safety
                .Replace("  ", " ");

            // до нижнього регістру
            string temp = "";
            for (int i = 0; i < tonality.Length; i++)
                temp += char.ToLowerInvariant(tonality[i]);
            tonality = temp;

            if (!tonality.EndsWith("dur") && !tonality.EndsWith("moll"))
                throw new IncorrectNote("impossible to determint input: " + tonality);

            if (tonality.EndsWith("dur") && tonality.Substring(tonality.Length - 4, 1) != "-")
                tonality = tonality.Insert(tonality.Length - 3, "-");

            if (tonality.EndsWith("moll") && tonality.Substring(tonality.Length - 5, 1) != "-")
                tonality = tonality.Insert(tonality.Length - 4, "-");

            if (tonality[2] == '-' && tonality[3] == '-')
                tonality = tonality.Remove(3, 1);

            if (tonality[3] == '-' && tonality[4] == '-')
                tonality = tonality.Remove(3, 1);

            // Більше не перетворюємо 'as' -> 'aes': Engine.key_to_pitch підтримує 's' у європейській нотації

            if (key_to_step(tonality.Substring(0, 1)) == -100 &&
                key_to_step(tonality.Substring(0, 2)) == -100 &&
                key_to_step(tonality.Substring(0, 3)) == -100)
            {
                throw new IncorrectNote("impossible to determint input: " + tonality);
            }

            return tonality;
        }

        public void Enharmonize()
        {
            {
                int pitch = standartpitch_from_step((int)step) + (int)alter;

                if (Keysignatures() > 0)
                {
                    step = addstepN(step, 1);
                    alter = (ALTER)alter_from_pitch((int)step, pitch);
                }
                else
                {
                    step = addstepN(step, -1);
                    alter = (ALTER)alter_from_pitch((int)step, pitch);
                }
            }
        }
        public int Keysignatures()
        { 
            return keysign(step, alter, mode);
        }

        public float MaxSharpness()
        {//максимально дієзна нота
                return Note().Sharpness + 5;
        }

        public float MinSharpness()
        {//максимально бемольна нота
            if (mode == MODE.dur) return Note().Sharpness - 1;
            else return Note().Sharpness - 4;
        }

        public string Name()
        {
            
            return (step_to_notename((int)step, (int)alter));
        }
        public Note Note()
        {
            Note note = new(step, alter);
            return note;
        }

        public int Pitch()
        {
            return standartpitch_from_step((int)step + (int)alter);
        }

        public bool Relative(Tonalities destination)
        {
            if (Keysignatures() == destination.Keysignatures() && step != destination.step)
                return true;
            else if (Keysignatures() == destination.Keysignatures() + 1 || Keysignatures() == destination.Keysignatures() - 1)
                return true;
            else if (mode == MODE.moll && destination.mode == MODE.dur && destination.Keysignatures() == Keysignatures() + 4)
                return true;
            else if (mode == MODE.dur && destination.mode == MODE.moll && destination.Keysignatures() == Keysignatures() - 4)
                return true;
            else return false;
        }
        public void Show()
        {
            Write(ToString());
        }
        public void ShowInBrackets()
        {
            Message(8, "(" + Name() + ")");
        }
        public void ShowRelatives()
        {
            Tonalities current = this;
            for (int i = 1; i < 7; i++)
            {
                current.Transport(i);
                current.Show();
                current = this;
            }
        }

        public void Transport(int i)
        {
            int pitch = standartpitch_from_step(step) + (int)alter;

            switch (i)
            {                
                default: return;
                case 2:   /* II/VII ступінь */
                    if (mode == MODE.dur) { step = addstepN(step, 1); pitch = addpitch(pitch, 2); mode=MODE.moll; }
                    else { step = addstepN(step, -1); pitch = addpitch(pitch, -2); mode = MODE.dur; }
                    break;
                case 3:
                    step = addstepN(step, 2); /* III ступінь */
                    if (mode == MODE.dur) { pitch = addpitch(pitch, 4); mode = MODE.moll; }
                    else { pitch = addpitch(pitch, 3); mode = MODE.dur; };
                    break;
                case 4: step = addstepN(step, 3); pitch = addpitch(pitch, 5); break;// субдомінанта
                case 5: step = addstepN(step, 4); pitch = addpitch(pitch, 7); break; // домінанта
                case 6:
                    step = addstepN(step, 5); /* VI ступінь */
                    if (mode == MODE.dur) { pitch = addpitch(pitch, 9); mode = MODE.moll; }
                    else { pitch = addpitch(pitch, 8); mode = MODE.dur; }
                    break;
                case 1:
                    if (mode == MODE.moll) { step = addstepN(step, 4); pitch = addpitch(pitch, 7); mode = MODE.dur; } // мажорна домінанта
                    else { step = addstepN(step, 3); pitch = addpitch(pitch, 5); mode = MODE.moll; } // мінорна субдомінанта
                    break;
            };

            alter = (ALTER)alter_from_pitch(step, pitch);
        }

        public override string ToString()
        {
            if (this is null) return notonality();
            string tonmode = "";
            if (mode == MODE.dur) tonmode = major();
            else if (mode == MODE.moll) tonmode = minor();

            return (step_to_notename((int)step, (int)alter) + " " + tonmode);
        }

        public Scale NotesInTonality()
        {
            Scale scale = CommonScale();

            GrayMessageL("Scale:" + scale.ToString());
            return scale;
        }

        private Scale CommonScale()
        {
            var scale = new Scale();
            //I
            var noteI = new Note(step, alter);
            scale.AddNote(noteI);
            //II
            var noteII = noteI.TransposeToNote(INTERVALS.SECUNDA, QUALITY.MAJ);
            scale.AddNote(noteII);
            //III
            var noteIII = mode == MODE.dur ? noteI.TransposeToNote(INTERVALS.TERZIA, QUALITY.MAJ) : noteI.TransposeToNote(INTERVALS.TERZIA, QUALITY.MIN);
            scale.AddNote(noteIII);
            //IV
            var noteIV = noteI.TransposeToNote(INTERVALS.QUARTA, QUALITY.PERFECT);
            scale.AddNote(noteIV);
            //V
            var noteV = noteI.TransposeToNote(INTERVALS.QUINTA, QUALITY.PERFECT);
            scale.AddNote(noteV);
            //VI
            var noteVI = mode == MODE.dur ? noteI.TransposeToNote(INTERVALS.SEKSTA, QUALITY.MAJ) : noteI.TransposeToNote(INTERVALS.SEKSTA, QUALITY.MIN);
            scale.AddNote(noteVI);
            //VII
            var noteVII = mode == MODE.dur ? noteI.TransposeToNote(INTERVALS.SEPTYMA, QUALITY.MAJ) : noteI.TransposeToNote(INTERVALS.SEPTYMA, QUALITY.MIN);
            scale.AddNote(noteVII);
            return scale;
        }

        public Scale NotesInTonalityExtended()
        {
            
            Scale scale = CommonScale();

            var noteI = new Note(step, alter);
            scale.AddNote(noteI);
            //IIb
            var noteII_ = noteI.TransposeToNote(INTERVALS.SECUNDA, QUALITY.MIN);
            scale.AddNote(noteII_);


            //IV#
            var noteIV_ = noteI.TransposeToNote(INTERVALS.QUARTA, QUALITY.AUG);
            scale.AddNote(noteIV_);

            if (mode == MODE.moll) {
                
                //VI#
                var noteVI_ = noteI.TransposeToNote(INTERVALS.SEKSTA, QUALITY.MAJ);
                scale.AddNote(noteVI_);
                //VII#
                var noteVII_ = noteI.TransposeToNote(INTERVALS.SEPTYMA, QUALITY.MAJ);
                scale.AddNote(noteVII_);
            }

            GrayMessageL("Scale:" + scale.ToString());
            return scale;
        }


        public static string GetDegree(Note note, Tonalities tonality)
        {
            while (note.AbsPitch() < tonality.Pitch()) note.OctUp();
            

            Interval interval = new Interval(tonality.GetNote(), note);
            
            var degree = GetSteps(interval);
            if (degree == "unknown") return degree;
            switch(interval.Quality)
            {
                case QUALITY.AUG: degree += sharpsymbol; break;
                case QUALITY.DIM: degree += flatsymbol; break;
                case QUALITY.MIN:
                    {
                        if (degree == "II") degree += flatsymbol; 
                        else if (degree == "VI" && tonality.mode == MODE.dur)  degree += flatsymbol; 
                        else if (degree == "VII" && tonality.mode == MODE.dur) degree += flatsymbol;
                    }; break;
                case QUALITY.MAJ:
                    {
                        if (degree == "VI" && tonality.mode == MODE.moll) degree += sharpsymbol;
                        else if (degree == "VII" && tonality.mode == MODE.moll) degree += sharpsymbol;
                    }; break;
                default: return degree;
            }; 
            return degree;
        }

        private static string GetSteps(Interval interval)
        {
            //GrayMessageL($"interval.Steps = {interval.Steps}");
            switch (interval.Steps)
            {
                default: return "unknown";
                case 0: return "I"; 
                case 1: return "II"; 
                case 2: return "III";
                case 3: return "IV"; 
                case 4: return "V"; 
                case 5: return "VI"; 
                case 6: return "VII"; 
            }
        }

        public static Dictionary<string, float> DegreeStats(List<Note> notes, Tonalities tonality)
        {
            var dictionary = new Dictionary<string, float>();
            var increment = 100f / notes.Count;

            foreach (var note in notes)
            {
                if (tonality is not null)
                {
                    var degree = GetDegree(note, tonality);
                    
                    if (dictionary.ContainsKey(degree))
                    {
                        dictionary[degree] += increment;
                    }
                    else
                    {
                        dictionary.Add(degree, increment);
                    }
                }
            }
            dictionary = dictionary.OrderBy(d => d.Key).ToDictionary(pair => pair.Key, pair => (float)Math.Round(pair.Value, 1));  

            return dictionary;
        }

        public static Dictionary<string, float> DegreeWeightStats(List<Note> notes, Tonalities tonality)
        {
            var dictionary = new Dictionary<string, float>();
            var totallength = 0;
            foreach (var note in notes)
                totallength += note.AbsDuration();

            var increment = 100f / totallength;

            foreach (var note in notes)
            {
                if (tonality is not null)
                {
                    var degree = GetDegree(note, tonality);

                    if (dictionary.ContainsKey(degree))
                    {
                        dictionary[degree] += increment * note.AbsDuration();
                    }
                    else
                    {
                        dictionary.Add(degree, increment);
                    }
                }
            }
            dictionary = dictionary.OrderBy(d => d.Key).ToDictionary(pair => pair.Key, pair => (float)Math.Round(pair.Value, 1));

            return dictionary;
        }

        public static Tonalities? GetTonalitiesFromMidi(MidiFile midifile)
        {

            Tonalities? tonalities = null;
            
            foreach (var midievent in midifile.Events)
            {
                
                    if (midievent is KeySignatureEvent kse)
                    {
                        int sharpflat = (int)kse.SharpsFlats;
                        MODE mode = (MODE)kse.MajorMinor;                        
                        tonalities.GetTonalityFromKeySignature(sharpflat, mode);                        
                    }
                
            }
            return tonalities;
        }

        public int GetSharpFlats()
        {
            return Keysignatures();
        }


        public object Clone()
        {
            Tonalities Clone = new Tonalities(step, alter, mode);
            return Clone;
        }

        public override bool Equals(object obj)
        {
            if (obj is Tonalities other)
            {
                return step == other.step && alter == other.alter && mode == other.mode;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(step, alter, mode);
        }

        public static bool operator ==(Tonalities left, Tonalities right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(Tonalities left, Tonalities right)
        {
            return !(left == right);
        }

    }
}
