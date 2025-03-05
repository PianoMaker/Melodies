using System.CodeDom;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using static Music.Engine;
using static Music.Messages;
using static System.Console;

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
            string[] tokens = name.Split('-', ' '); // Розбиваємо рядок на токени за допомогою роздільників '-'' та ' '
            string key = tokens[0]; // Отримуємо перший токен як ключ
            string tempmode = tokens.Length > 1 ? tokens[1] : "dur"; // Отримуємо другий токен як лад, якщо відсутній - використовуємо мажор

            step = (NOTES)key_to_step(key); // Отримуємо крок
            int pitch = key_to_pitch(key); // Отримуємо висоту тона
            alter = (ALTER)pitch_to_alter(step, pitch); // Отримуємо альтерацію якщо є
            mode = (tempmode == "dur") ? MODE.dur : MODE.moll; // Визначаємо, чи мінорний лад

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
            string temp = "";
            for (int i = 0; i < tonality.Length; i++)
            {
                temp += char.ToLower(tonality[i]);
            }
            tonality = temp;

            if (!tonality.EndsWith("dur") && !tonality.EndsWith("moll"))
            {
                throw new IncorrectNote("impossible to determint input: " + tonality);
            }

            if (tonality.EndsWith("dur") && tonality.Substring(tonality.Length - 4, 1) != "-")
            {
                tonality = tonality.Insert(tonality.Length - 3, "-");
            }

            if (tonality.EndsWith("moll") && tonality.Substring(tonality.Length - 5, 1) != "-")
            {
                tonality = tonality.Insert(tonality.Length - 4, "-");
            }

            if (tonality[2] == '-' && tonality[3] == '-')
            {
                tonality = tonality.Remove(3, 1);
            }

            if (tonality[3] == '-' && tonality[4] == '-')
            {
                tonality = tonality.Remove(3, 1);
            }

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
            var scale = new Scale();
            //I
            var noteI = new Note(step, alter);            
            scale.AddNote(noteI);
            //II
            var noteII = noteI.TransposeToNote(INTERVALS.SECUNDA, QUALITY.MAJ);
            scale.AddNote(noteII);
            //III
            var noteIII = mode == MODE.moll?noteI.TransposeToNote(INTERVALS.TERZIA, QUALITY.MAJ) : noteI.TransposeToNote(INTERVALS.TERZIA, QUALITY.MIN);
            scale.AddNote(noteIII);
            //IV
            var noteIV = noteI.TransposeToNote(INTERVALS.QUARTA, QUALITY.PERFECT);
            scale.AddNote(noteIV);
            //V
            var noteV = noteI.TransposeToNote(INTERVALS.QUINTA, QUALITY.PERFECT);
            scale.AddNote(noteV);
            //VI
            var noteVI = mode == MODE.moll ? noteI.TransposeToNote(INTERVALS.SEKSTA, QUALITY.MAJ) : noteI.TransposeToNote(INTERVALS.SEKSTA, QUALITY.MIN);
            scale.AddNote(noteVI);
            //VII
            var noteVII = mode == MODE.moll ? noteI.TransposeToNote(INTERVALS.SEKSTA, QUALITY.MAJ) : noteI.TransposeToNote(INTERVALS.SEPTYMA, QUALITY.MIN);
            scale.AddNote(noteVII);

            return scale;
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
