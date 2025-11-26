using System.Text.RegularExpressions;
using static Music.Globals;
using static Music.Messages;
using static System.Console;

namespace Music
{

    public class Engine
    {

        public static double abs_duration(DURATION duration)
        {
            return playspeed / (int)duration;


        }

        public static int addpitch(int pitch1, int pitch2)
        {
            // обчислює висоту звуку після додаання заданого інтервалу (в півнтоах)
            int pitch = pitch1 + pitch2;
            while (pitch < 0) pitch += NotesInOctave;
            while (pitch >= NotesInOctave) pitch -= NotesInOctave;
            return pitch;

        }

        public static int addpitch(int pitch1, ref int oct, int pitch2)
        {
            // обчислює висоту звуку після додаання заданого інтервалу (в півнтоах)
            int pitch = pitch1 + pitch2;
            while (pitch < 0) { pitch += NotesInOctave; oct--; }
            while (pitch >= NotesInOctave) { pitch -= NotesInOctave; oct++; }
            return pitch;

        }
        public static int addstep(int step1, int step2)
        {// обчислює ступінь після додаання заданого інтервалу (в ступенях)
            int step = step1 + step2;
            while (step < 0) step += 7;
            while (step > 6) step -= 7;
            return step;
        }

        public static int addstep(NOTES step1, int step2)
        {// обчислює ступінь після додаання заданого інтервалу (в ступенях)
            int step = (int)step1 + step2;
            while (step < 0) step += 7;
            while (step > 6) step -= 7;
            return step;
        }

        public static NOTES addstepN(NOTES step1, int step2)
        {// обчислює ступінь після додаання заданого інтервалу (в ступенях)
            int step = (int)step1 + step2;
            while (step < 0) step += 7;
            while (step > 6) step -= 7;
            return (NOTES)step;
        }

        public static void addstep(ref NOTES step1, int step2)
        {// обчислює ступінь після додаання заданого інтервалу (в ступенях)
            int step = (int)step1 + step2;
            while (step < 0) step += 7;
            while (step > 6) step -= 7;
            step1 = (NOTES)step;

        }

        public static int addstep(int step1, ref int oct, int step2)
        {// обчислює ступінь після додавання заданого інтервалу (в ступенях)
            int step = step1 + step2;
            while (step < 0) { step += 7; oct--; }
            while (step > 6) { step -= 7; oct++; }
            return step;
        }

        public static double avg_sharpness(double[] sharpness, int NoN)
        {//обчислює положення на квінтовому колі
            double avg_sharpness = 0;
            for (int i = 0; i < NoN; i++)
                avg_sharpness += sharpness[i];
            avg_sharpness /= NoN;
            return avg_sharpness;
        }

        public static int alter_from_standartpitch(int pitch, int standartpitch)
        {// для обчислення знаків альтерації
            if (pitch == 11 && standartpitch == 0)
                return -1;
            else if (pitch == 10 && standartpitch == 0)
                return -2;
            else if (pitch == 9 && standartpitch == 0)
                return -3;
            else if (pitch == 11 && standartpitch == 2)
                return -3;
            else if (pitch == 0 && standartpitch == 11)
                return 1;
            else if (pitch == 1 && standartpitch == 11)
                return 2;
            else if (pitch == 2 && standartpitch == 11)
                return 3;
            else if (pitch == 0 && standartpitch == 9)
                return 3;
            else if (pitch == 12 && standartpitch == 0)
                return 0;
            else if (pitch == -1 && standartpitch == 11)
                return 0;
            else if ((pitch - standartpitch) > 4 || (pitch - standartpitch) < -4) throw new IncorrectNote("Помилка визначення альтерації");
            else
                return pitch - standartpitch;
        }// визначає знак альтерації

        public static int alter_from_pitch(int step, int pitch)
        {// для обчислення знаків альтерації

            while (step >= StepsInOctave)
                step -= StepsInOctave;
            while (step < 0)
                step += 7;
            while (pitch >= NotesInOctave)
                pitch -= NotesInOctave;


            int standartpitch = standartpitch_from_step(step);
            return alter_from_standartpitch(pitch, standartpitch);
        }

        public static int alter_from_pitch(NOTES step, int pitch)
        { return alter_from_pitch((int)step, pitch); }


        public static int CountOccurrences(string input, string character)
        {
            int count = 0;
            int index = input.IndexOf(character);
            while (index != -1)
            {
                count++;
                index = input.IndexOf(character, index + 1);
            }
            return count;
        }

        public static double Diffpitch(double pitch, int octavecorrector)
        {
            // вихідний тон: А4 
            return pitch - standartpitch_from_step(NOTES.LA) + NotesInOctave * octavecorrector;
            
        }

        public static string Interval_name(int step0, int step1)
        {

            int steps = stepdiff(step0, step1);
            INTERVALS interval = (INTERVALS)steps;
            switch (interval)
            {
                case INTERVALS.PRIMA:
                    return "прима";

                case INTERVALS.SECUNDA:
                    return "секунда";

                case INTERVALS.TERZIA:
                    return "терція";

                case INTERVALS.QUARTA:
                    return "кварта";

                case INTERVALS.QUINTA:
                    return "квінта";

                case INTERVALS.SEKSTA:
                    return "секста";

                case INTERVALS.SEPTYMA:
                    return "септима";

                case INTERVALS.OCTAVA:
                    return "октава";

                default:
                    return "не можу вгадати!";
            }
        }

        public static QUALITY int_quality(int steps, int halftones)
        {//визначає якість інтервалу


            int temp_steps = steps, temp_halftones = halftones;
            while (steps > 6) temp_steps -= 7;
            while (halftones > NotesInOctave) temp_halftones -= NotesInOctave;

            // умови для альтерованих досконалих консонансів (0 - прима, 3 - кварта, 4 -квінта, 7 - октава)

            if ((temp_steps == 0 && temp_halftones == 0) ||
                (temp_steps == 3 && temp_halftones == 5) ||
                (temp_steps == 4 && temp_halftones == 7) ||
                (temp_steps == 7 && temp_halftones == 12)) return QUALITY.PERFECT; // "чистий"

            else if ((temp_steps == 0 && temp_halftones == 1) ||
                (temp_steps == 3 && temp_halftones == 6) ||
                (temp_steps == 4 && temp_halftones == 8) ||
                (temp_steps == 7 && temp_halftones == 1)) return QUALITY.AUG;//збільшений
            /**/
            else if ((temp_steps == 0 && temp_halftones == 2) ||
                (temp_steps == 3 && temp_halftones == 7) ||
                (temp_steps == 4 && temp_halftones == 9) ||
                (temp_steps == 7 && temp_halftones == 2)) return QUALITY.AUG2;//двічі збільшений

            else if ((temp_steps == 0 && temp_halftones == -1) ||
                (temp_steps == 0 && temp_halftones == 11) ||
                (temp_steps == 3 && temp_halftones == 4) ||
                (temp_steps == 4 && temp_halftones == 6) ||
                (temp_steps == 7 && temp_halftones == 11)) return QUALITY.DIM;//зменшений

            else if ((temp_steps == 0 && temp_halftones == -2) ||
                (temp_steps == 0 && temp_halftones == 10) ||
                (temp_steps == 3 && temp_halftones == 3) ||
                (temp_steps == 4 && temp_halftones == 5) ||
                (temp_steps == 7 && temp_halftones == 10)) return QUALITY.DIM2;//двічі зменшений


            // умови для альтерованих недосконалих консонансів (2 - терція, 5 - секста)

            else if ((temp_steps == 2 && temp_halftones == 3) ||
                (temp_steps == 5 && temp_halftones == 8)) return QUALITY.MIN; // малий

            else if ((temp_steps == 2 && temp_halftones == 2) ||
                (temp_steps == 5 && temp_halftones == 7)) return QUALITY.DIM; // зменшенй

            else if ((temp_steps == 2 && temp_halftones == 4) ||
                (temp_steps == 5 && temp_halftones == 9)) return QUALITY.MAJ; // великий

            else if ((temp_steps == 2 && temp_halftones == 5) ||
                (temp_steps == 5 && temp_halftones == 10)) return QUALITY.AUG; // збільшений


            // умови для альтерованих дисонансів (1 - секунда, 6 - септима)

            else if ((temp_steps == 1 && temp_halftones == 1) ||
                (temp_steps == 6 && temp_halftones == 10)) return QUALITY.MIN;// малий

            else if ((temp_steps == 1 && temp_halftones == 0) ||
                (temp_steps == 6 && temp_halftones == 9)) return QUALITY.DIM;// зменшений

            else if ((temp_steps == 1 && temp_halftones == 2) ||
                (temp_steps == 6 && temp_halftones == 11)) return QUALITY.MAJ;//великий

            else if ((temp_steps == 1 && temp_halftones == 3) ||
                (temp_steps == 6 && temp_halftones == 12)) return QUALITY.AUG;//збільшений

            else if ((temp_steps == 1 && temp_halftones == 3) ||
                   (temp_steps == 6 && temp_halftones == 0)) return QUALITY.AUG;//збільшена 

            // для тих хто вирішив повимахуватись і думає, що програма це схаває

            else throw new IncorrectNote("incorrect interval, int_quality func." + " steps = " + steps + " halftones = " + halftones);


        }// визначає якість інтервалу

        public static int int_to_pitch(INTERVALS interval, QUALITY intervalquality)
        {
            while ((int)interval > StepsInOctave) interval -= StepsInOctave;
            int standartpitch = -1;
            switch (interval)
            {
                case INTERVALS.PRIMA: standartpitch = 0; break;
                case INTERVALS.QUARTA: standartpitch = 5; break;
                case INTERVALS.QUINTA: standartpitch = 7; break;
                case INTERVALS.OCTAVA: standartpitch = NotesInOctave; break;
                default: break;
            }
            if (standartpitch > -1)
            {
                switch (intervalquality)
                {
                    case QUALITY.PERFECT: return standartpitch;
                    case QUALITY.AUG: return standartpitch + 1;
                    case QUALITY.AUG2: return standartpitch + 2;
                    case QUALITY.DIM: return standartpitch - 1;
                    case QUALITY.DIM2: return standartpitch - 2;
                    default: throw new IncorrectNote("Incorrect interval (int_to_pitch func.) intquality = " + intervalquality);
                }
            }
            else
            {
                switch (interval)
                {
                    default: throw new IncorrectNote("Incorrect interval (int_to_pitch func.) intquality = " + intervalquality);
                    case INTERVALS.SECUNDA:
                        {
                            switch (intervalquality)
                            {
                                case QUALITY.MIN: return 1;
                                case QUALITY.MAJ: return 2;
                                case QUALITY.DIM: return 0;
                                case QUALITY.AUG: return 3;
                                case QUALITY.AUG2: return 4;
                                default: throw new IncorrectNote("Incorrect interval (int_to_pitch func.) intquality = " + intervalquality);
                            }
                        }
                    case INTERVALS.TERZIA:
                        {
                            switch (intervalquality)
                            {
                                case QUALITY.MIN: return 3;
                                case QUALITY.MAJ: return 4;
                                case QUALITY.DIM: return 2;
                                case QUALITY.DIM2: return 1;
                                case QUALITY.AUG: return 5;
                                case QUALITY.AUG2: return 6;
                                default: throw new IncorrectNote("Incorrect interval (int_to_pitch func.) intquality = " + intervalquality);
                            }
                        }
                    case INTERVALS.SEKSTA:
                        {
                            switch (intervalquality)
                            {
                                case QUALITY.MIN: return 8;
                                case QUALITY.MAJ: return 9;
                                case QUALITY.DIM: return 7;
                                case QUALITY.DIM2: return 6;
                                case QUALITY.AUG: return 10;
                                case QUALITY.AUG2: return 11;
                                default: throw new IncorrectNote("Incorrect interval (int_to_pitch func.) intquality = " + intervalquality);
                            }
                        }
                    case INTERVALS.SEPTYMA:
                        {
                            switch (intervalquality)
                            {
                                case QUALITY.MIN: return 10;
                                case QUALITY.MAJ: return 11;
                                case QUALITY.DIM: return 9;
                                case QUALITY.DIM2: return 8;
                                case QUALITY.AUG: return 12;
                                case QUALITY.AUG2: return 13;
                                default: throw new IncorrectNote("Incorrect interval (int_to_pitch func.) intquality = " + intervalquality);
                            }
                        }

                }

            }
        }//повертає величину інтервалу в півтонах
        public static string key_to_notename(string key)
        {
            string notename, noteaccname;
            string note_as_written = key.Substring(0, 1);
            string n_acc = "";
            if (key.Length > 1)
                n_acc = key.Substring(1);

            if (note_as_written == "c") notename = ndo();
            else if (note_as_written == "d") notename = nre();
            else if (note_as_written == "e") notename = nmi();
            else if (note_as_written == "f") notename = nfa();
            else if (note_as_written == "g") notename = nsol();
            else if (note_as_written == "a") notename = nla();
            else if ((note_as_written == "b" && notation == Notation.eu && n_acc == "is") || (note_as_written == "b" && notation == Notation.eu && n_acc == "isis"))
                return note_error();

            else if (note_as_written == "h" && notation == Notation.eu && n_acc == "es")
                return note_error();

            else if (note_as_written == "b" && notation == Notation.eu && n_acc == "es") notename = nsi();
            else if (note_as_written == "b" && notation == Notation.eu) notename = nsi() + " b";
            else if (note_as_written == "b" && notation == Notation.us) notename = nsi();
            else if (note_as_written == "h")
            {
                if (notation == Notation.eu) notename = nsi();

                else
                    return note_error();
            }
            else
                return note_error();


            if (n_acc == "") noteaccname = "";
            else if (n_acc == "isisis" && notation == Notation.eu || n_acc == "###" || n_acc == "x#")
                noteaccname = " х#";
            else if (n_acc == "isis" && notation == Notation.eu || n_acc == "##" && notation == Notation.us || n_acc == "x" && notation == Notation.us)
                noteaccname = " х";
            else if (n_acc == "eseses" && notation == Notation.eu || n_acc == "seses" && notation == Notation.eu || n_acc == "bbb")
                noteaccname = " ььь";
            else if (n_acc == "eses" && notation == Notation.eu || n_acc == "ses" && notation == Notation.eu || n_acc == "bb")
                noteaccname = " ьь";
            else if (n_acc == "is" && notation == Notation.eu || n_acc == "#" && notation == Notation.us)
                noteaccname = " #";
            else if (n_acc == "es" && note_as_written == "b" && notation == Notation.us)
                noteaccname = " ьь";
            else if (n_acc == "es" && notation == Notation.eu || n_acc == "s" && notation == Notation.eu || n_acc == "b" && notation == Notation.us)
                noteaccname = " ь";
            else
                return note_error();

            return notename + noteaccname;
        } // назви

        public static int key_to_pitch(string key, bool ifext = false)
        {//перетворює латинське позначення у звуковисотність

            string note_as_written = key.Substring(0, 1);
            string n_acc = "";
            if (key.Length > 1)
                n_acc = key.Substring(1);

            int pitch, alteration;
            if (note_as_written == "c") pitch = 0;
            else if (note_as_written == "d") pitch = 2;
            else if (note_as_written == "e") pitch = 4;
            else if (note_as_written == "f") pitch = 5;
            else if (note_as_written == "g") pitch = 7;
            else if (note_as_written == "a") pitch = 9;
            else if ((note_as_written == "b" && notation == Notation.us && n_acc == "is") || (note_as_written == "b" && (notation == Notation.us) && n_acc == "isis"))
                throw new IncorrectNote(note_as_written);
            else if (note_as_written == "h" && notation == Notation.eu && n_acc == "es")
                throw new IncorrectNote(note_as_written);
            else if (note_as_written == "b" && notation == Notation.eu) pitch = 10;
            else if (note_as_written == "b") pitch = 11;
            else if (note_as_written == "h")
            {
                if (notation == Notation.eu) pitch = 11;
                else
                    throw new IncorrectNote("symbol 'h' vs us notation");
            }
            else
                throw new IncorrectNote(note_as_written);
            if (n_acc == "")
                alteration = 0;
            else if (n_acc == "isis" && notation == Notation.eu || n_acc == "##" && notation == Notation.us || n_acc == "x" && notation == Notation.us)
            { alteration = 2; }
            else if (n_acc == "isisis" && notation == Notation.eu || n_acc == "###" && notation == Notation.us || n_acc == "x#" && notation == Notation.us)
            { alteration = 3; }
            else if (n_acc == "eses" && notation == Notation.eu || n_acc == "ses" && notation == Notation.eu || n_acc == "bb")
            { alteration = -2; }
            else if (n_acc == "is" && notation == Notation.eu || n_acc == "#" && notation == Notation.us)
            { alteration = 1; }
            else if (n_acc == "es" && note_as_written == "b" && notation == Notation.eu)
            { alteration = -1; }
            else if (n_acc == "es" && notation == Notation.eu || n_acc == "s" && notation == Notation.eu || n_acc == "b" && notation == Notation.us)
            { alteration = -1; }
            else if (n_acc == "s" && note_as_written == "a" && notation == Notation.eu)
            { alteration = -1; }
            else if (ifext)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(n_acc, @"[\d',]+"))
                { alteration = 0; }
                else throw new IncorrectNote("\nUnrecognised postfix : " + n_acc + "\n");
            }
            else throw new IncorrectNote("\nUnrecognised postfix : " + n_acc + "\n");

            return pitch + alteration;

        } // звуковисотності

        public static int key_to_step(string key) // повертає ступінь за введеною клавішею
        {
            string note_as_written = key.Substring(0, 1);
            string n_acc = key.Substring(1);
            note_as_written = note_as_written.ToLower();
            int notes = 0;
            try
            {
                if (note_as_written == "c") notes = (int)NOTES.DO;
                else if (note_as_written == "d") notes = (int)NOTES.RE;
                else if (note_as_written == "e") notes = (int)NOTES.MI;
                else if (note_as_written == "f") notes = (int)NOTES.FA;
                else if (note_as_written == "g") notes = (int)NOTES.SOL;
                else if (note_as_written == "a") notes = (int)NOTES.LA;
                else if (note_as_written == "b" && notation == Notation.eu && (n_acc == "is" || n_acc == "es")) throw new IncorrectNote("incorrect notation");
                else if ((note_as_written == "b" || note_as_written == "h") && notation == Notation.eu) notes = (int)NOTES.SI;
                else if (note_as_written == "h" && notation == Notation.us) throw new IncorrectNote("symbol 'h' vs us notation");
                else throw new IncorrectNote(note_error());

                return notes;
            }
            catch (IncorrectNote e)
            {
                Message(12, e.Message);
                return -100;
            }
        }


        public static int keysign(NOTES step, ALTER alter, MODE mode)
        {
            int keysign;
            switch (step)
            {
                case NOTES.DO: keysign = 0; break;
                case NOTES.RE: keysign = 2; break;
                case NOTES.MI: keysign = 4; break;
                case NOTES.FA: keysign = -1; break;
                case NOTES.SOL: keysign = 1; break;
                case NOTES.LA: keysign = 3; break;
                case NOTES.SI: keysign = 5; break;
                default: throw new IncorrectNote("unknown step: " + step);
            }

            if (alter != 0) keysign += (int)alter * 7;
            if (mode == MODE.moll) keysign -= 3;
            return keysign;
        }





        public static string noteaccname_from_alter(int alter)
        {
            switch (alter)
            {
                case 0: return "";
                case 1: return " #";
                case 2: return " х";
                case 3: return " х#";
                case 4: return " хх";
                case -1: return " b";
                case -2: return " bb";
                case -3: return " bbb";
                case -4: return " bbbb";
                default: return "Помилка при введенні ноти\n";
            }

        }


        public static string note_to_key(int step, int pitch)
        {//Визначає латинську назву за ступенем і висотою
            string key;
            string postfix;
            int alter;

            while (step >= StepsInOctave)
                step -= StepsInOctave;
            while (step < 0) step += StepsInOctave;

            key = step_to_key(step, notation);
            alter = alter_from_pitch(step, pitch);

            if (notation == Notation.eu)
            {
                switch (alter)
                {
                    case 0: postfix = ""; break;
                    case 1: postfix = "is"; break;
                    case 2: postfix = "isis"; break;
                    case 3: postfix = "isisis"; break;
                    case -1: postfix = "es"; break;
                    case -2: postfix = "eses"; break;
                    case -3: postfix = "eseses"; break;
                    default: return note_error();
                }

                key = key + postfix;
                if (key == "hes") key = "b";
                else if (key == "heses") key = "bes";
                else if (key == "ees") key = "es";
                else if (key == "eeses") key = "eses";
                else if (key == "aes") key = "as";
                else if (key == "aeses") key = "ases";

            }

            if (notation == Notation.us)
            {
                switch (alter)
                {
                    case 0: postfix = ""; break;
                    case 1: postfix = "#"; break;
                    case 2: postfix = "x"; break;
                    case 3: postfix = "x#"; break;
                    case -1: postfix = "b"; break;
                    case -2: postfix = "bb"; break;
                    case -3: postfix = "bbb"; break;
                    default: return "error";
                }
                key = key + postfix;
            }

            return key;
        }

        public static void octdivide(string input, out int octave, out string key)
        {
            //синтаксис - після позначення ноти - позначення октави
            octave = 1; key = "";

            // Використання регулярного виразу для знаходження літер і цифр
            Match match = Regex.Match(input, @"([a-zA-Z]+)(-?\d+)?");
            try
            {
                if (match.Success)
                {
                    key = match.Groups[1].Value;
                    if (match.Groups[2].Success)
                    {
                        octave = int.Parse(match.Groups[2].Value);
                        if (octave > 5) throw new IncorrectNote("up to 4 octaves are permitted");
                        else if (octave < -3) throw new IncorrectNote("the lowest possible note is c -3");
                    }
                }
                else
                {
                    // Якщо не знайдено відповідності, видаємо помилку
                    throw new IncorrectNote();
                }

                Console.WriteLine($"Літери: {key}");
                Console.WriteLine($"Цифри: {octave}");
            }
            catch (IncorrectNote e)
            {
                Message(12, e.Message);
                octave = 0; key = input;
            }
        }


        public static int pitch_to_alter(int step, int pitch)
        {
            int standartpitch = standartpitch_from_step(step);

            return alter_from_standartpitch(pitch, standartpitch);
        }

        public static int pitch_to_alter(NOTES step, int pitch)
        {
            int standartpitch = standartpitch_from_step((int)step);

            return alter_from_standartpitch(pitch, standartpitch);
        }

        public static int pitchdiff(int low_pitch, int high_pitch)
        {
            if (low_pitch > high_pitch) high_pitch += NotesInOctave;
            int halftones = high_pitch - low_pitch;
            return halftones;
        }

        public static int pitchdiff(int? low_pitch, int? high_pitch)
        {
            if (low_pitch is null || high_pitch is null) return 0;
            if (low_pitch > high_pitch) high_pitch += NotesInOctave;
            int halftones = (int)high_pitch - (int)low_pitch;
            return halftones;
        }

        public static string pitch_to_notename(int step, int pitch)
        {
            while (step >= StepsInOctave)
                step -= StepsInOctave;
            while (pitch >= NotesInOctave)
                pitch -= NotesInOctave;
            if (step == -1) return "--";

            string notename = step_to_notename(step);
            int standartpitch = standartpitch_from_step(step);
            int alter = alter_from_standartpitch(pitch, standartpitch);
            string noteaccname = noteaccname_from_alter(alter);

            return notename + noteaccname;
        }

        static double Pitch(double diffpitch)
        {
            // за камертоном, заданим як const А4, формула рівномірно-темперованого строю
            return A4 * (Math.Pow(2, (diffpitch / NotesInOctave)));
        }

        public static int Pitch_to_hz(int pitch, int oct = 0)
        {
            return (int)Pitch(Diffpitch(pitch, oct));
        }

        public static Tuple<int, int> pitch_to_step_alter(int pitch)
        {
            if (pitch > NotesInOctave) pitch = pitch % NotesInOctave; 
            switch(pitch)
            {
                case 0: return new Tuple<int, int>(0, 0);
                case 1: return new Tuple<int, int>(0, 1);
                case 2: return new Tuple<int, int>(1, 0);
                case 3: return new Tuple<int, int>(2, -1);
                case 4: return new Tuple<int, int>(2, 0);
                case 5: return new Tuple<int, int>(3, 0);
                case 6: return new Tuple<int, int>(3, 1);
                case 7: return new Tuple<int, int>(4, 0);
                case 8: return new Tuple<int, int>(4, 1);
                case 9: return new Tuple<int, int>(5, 0);
                case 10: return new Tuple<int, int>(6, -1);
                case 11: return new Tuple<int, int>(7, 0);
                default: throw new NotImplementedException("incorrect pitch");
            }
        }


        public static int standartpitch_from_step(int step)
        {            
            try
            {
                switch (step)
                {
                    case (int)NOTES.DO: return 0;
                    case (int)NOTES.RE: return 2;
                    case (int)NOTES.MI: return 4;
                    case (int)NOTES.FA: return 5;
                    case (int)NOTES.SOL: return 7;
                    case (int)NOTES.LA: return 9;
                    case (int)NOTES.SI: return 11;
                    default: throw new IncorrectNote("unrecognised step");
                }
            }
            catch (IncorrectNote e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }
        }

        public static int standartpitch_from_step(NOTES step)
        { return standartpitch_from_step((int)step); }


        public static float sharpness_counter(int enterstep, int alter) // вводиться step, alter 
        {// положення на квінтовому колі
            int sharpness;

            while (enterstep >= StepsInOctave) enterstep -= StepsInOctave;
            if (enterstep == (int)NOTES.FA) sharpness = -3;
            else if (enterstep == (int)NOTES.DO) sharpness = -2;
            else if (enterstep == (int)NOTES.SOL) sharpness = -1;
            else if (enterstep == (int)NOTES.RE) sharpness = 0;
            else if (enterstep == (int)NOTES.LA) sharpness = 1;
            else if (enterstep == (int)NOTES.MI) sharpness = 2;
            else if (enterstep == (int)NOTES.SI) sharpness = 3;
            else throw new IncorrectNote(note_error());

            if (alter != 0) sharpness += alter * StepsInOctave;

            return sharpness;

        }

        public static string step_to_key(int step, Notation? notation)
        {
            if (notation == null) ChooseNotation();
            switch (step)
            {
                case (int)NOTES.DO: return "c";
                case (int)NOTES.RE: return "d";
                case (int)NOTES.MI: return "e";
                case (int)NOTES.FA: return "f";
                case (int)NOTES.SOL: return "g";
                case (int)NOTES.LA: return "a";
                case (int)NOTES.SI:
                    if (notation == Notation.eu) return "h";
                    else return "b";
                default: return errornote;
            }
        }

        public static void ChooseNotation()
        {
            Message(11, choose_notation());
            Message(7, notationtypes());

            string input;
            do
            {
                input = ReadLine();
                switch (input)
                {
                    case "1":
                    case "eu": notation = Notation.eu; break;
                    case "2":
                    case "us": notation = Notation.us; break;
                    default: WriteLine("incorrect choice"); break;
                }
            } while (input != "1" && input != "eu" && input != "2" && input != "us");
        }

        public static string step_to_notename(int step)
        {
            switch (step)
            {
                case (int)NOTES.DO: return ndo();
                case (int)NOTES.RE: return nre();
                case (int)NOTES.MI: return nmi();
                case (int)NOTES.FA: return nfa();
                case (int)NOTES.SOL: return nsol();
                case (int)NOTES.LA: return nla();
                case (int)NOTES.SI: return nsi();
                default: return note_error();
            }
        }
        public static string step_to_notename(int step, int alter)
        {
            string notename;
            string noteaccname;

            while (step >= StepsInOctave)
                step -= StepsInOctave;

            if (step == -1) return "Error";
            notename = step_to_notename(step);
            noteaccname = noteaccname_from_alter(alter);

            return notename + noteaccname;
        }

        public static string step_to_notename(NOTES step, ALTER alter)
        {
            return step_to_notename((int)step, (int)alter);
        }

        public static int stepdiff(int low_note, int high_note)
        {
            while (low_note > high_note) high_note += StepsInOctave;
            int interval = high_note - low_note;
            while (interval >= StepsInOctave) interval -= StepsInOctave;
            while (interval <= -StepsInOctave) interval += StepsInOctave;
            return interval;
        }

        public static void swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public static int steprange(int low_note, int high_note)
        {
            if (low_note > high_note) swap(ref low_note, ref high_note);

            return high_note - low_note;
        }

        public static void stringdivider(string input, out string key, out int octave, out int duration, out string? durmodifier)
        {
            //синтаксис - після позначення ноти - позначення октави (' або ,) і тривалості.
            //якщо не введено: тривалість - четертна, октава - перша.
            octave = 1; duration = 4; key = ""; durmodifier = null;

            // Використання регулярного виразу для знаходження літер і цифр
            // спочатку літери - ноти, потім апострофи 0 октава, потім цифри
            Match match = Regex.Match(input, @"([a-zA-Z]+)([',]+)?(\d+)?([.]+)?");
            try
            {

                if (match.Success)
                {
                    // нота
                    key = match.Groups[1].Value;
                    // октава
                    if (match.Groups[2].Success)
                    {
                        octave += CountOccurrences(match.Groups[2].Value, "'");//друга+
                        octave -= CountOccurrences(match.Groups[2].Value, ",");//мала-
                    }
                    // тривалість
                    if (match.Groups[3].Success)
                        duration = Convert.ToInt32(match.Groups[3].Value);

                    // ноти з крапками
                    if (match.Groups[4].Success)
                        durmodifier = match.Groups[4].Value;
                }
                else
                {
                    // Якщо не знайдено відповідності, видаємо помилку
                    throw new IncorrectNote($"помилка розпізнання рядку {match} під час генерування ноти");
                }

            }
            catch (IncorrectNote e)
            {
                Message(12, e.Message);
                key = input;
            }
        }


        public static int sum_pitchs(int[] pitch, int NoI)
        {
            int sum = 0;
            for (int i = 0; i < NoI; i++)
                sum += pitchdiff(pitch[i], pitch[i + 1]);
            return sum;

        }

        public static int sum_steps(int[] step, int NoI)
        {
            int sum = 0;
            for (int i = 0; i < NoI; i++)
                sum += stepdiff(step[i], step[i + 1]);
            return sum;

        }

        public int To_hz(int pitch, int oct = 0)
        {
            return (int)Pitch(Diffpitch(pitch, oct));
        }


    }

}
