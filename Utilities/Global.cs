using Microsoft.VisualBasic;
using NAudio.Midi;

namespace Music
{
    public enum NOTES { DO = 0, RE, MI, FA, SOL, LA, SI }; // ноти
    public enum Notation { eu = 1, us = 2 } // вибір нотації
    public enum LNG { uk = 1, en = 2 } // мова інтерфейсу
    public enum PLAYER { beeper, naudio, midiplayer }; // вибір присторїв відтворення

    public enum TIMBRE { sin, tri, sawtooth, square }; // вибір тембрів відтворення
    public enum COLORS { blue = 1, darkred = 4, olive = 6, standart = 7, gray = 8, green = 10, cyan = 11, red = 12, purple = 13, yellow = 14, white = 15 }
    public enum DIR { UP, DOWN }; // транспорт вгору/вниз
    public enum ALTER { SHARP = 1, DOUBLESHARP = 2, TRIPLESHARP = 3, FLAT = -1, DOUBLEFLAT = -2, FLATFLAT = -2, TREPLEFLAT = -3, NATURAL = 0 }; // знаки альтерації
    public enum QUALITY { PERFECT = 0, MAJ = 1, MIN = -1, AUG = 2, DIM = -2, AUG2 = 3, DIM2 = -3 }; // якість інтервалів
    public enum INTERVALS { PRIMA, SECUNDA, TERZIA, QUARTA, QUINTA, SEKSTA, SEPTYMA, OCTAVA }; // інтервали
    public enum CHORDS
    {
        TRI, SEXT, QSEXT, SEPT, SEC, TERZQ, QUINTS, HSEPT, HQUINTS, HTERZQ, HSEC, ADDQUARTA, ADDSEC, SUSQUARTA,
        NONACORD, NONACORD_1i, NONACORD_2i, NONACORD_3i, NONACORD_4i, CORD69, HUNDECCORD, HUNDECCORD_1i, HUNDECCORD_2i, HUNDECCORD_3i, HUNDECCORD_4i, HUNDECCORD_5i,
        UNDECCORD, UNDECCORD_1i, UNDECCORD_2i, UNDECCORD_3i, UNDECCORD_4i, UNDECCORD_5i, TERZDEC, CLUSTER, UNKNOWN, UNIS
    }; // тип акордів
    public enum SEPTS { MAJAUG, MAJMAJ, MAJMIN, MINMAJ, MINMIN, MINDIM, DIMDIM, ALTQUINT, ALTPRIM }; // якість септакордів

    public enum NINTHS { NMJAUG, NMNAUG, NDAUG, NMAJ, NDOM, NMDOM, NMIN, NMDIM, NDDIM, HMAJ, HMIN, HAUG, HDOM, OTHER }; // якість акордів
    public enum MODE { dur, moll, chrome }; // лад

    public enum DURATION { whole = 1, half = 2, quater = 4, eigth = 8, sixteenth = 16, thirtysecond = 32 } // тривалості
    public enum DURMODIFIER { none, dotted, doubledotted, tripledotted, tuplet, tied } // тривалості з крапками і види ритмічного поділу

    enum MAJTONALITIES { Cdur, Gdur, Ddur, Adur, Edur, Hdur, Fisdur, Cisdur, Fdur, Bdur, Esdur, Asdur, Desdur, Gesdur, Cesdur, Fesdur, Gisdur };
    enum MINTONALITIES { amoll, emoll, hmoll, fismoll, cismoll, gismoll, dismoll, aismoll, eismoll, dmoll, gmoll, cmoll, fmoll, bmoll, esmoll, asmoll, desmoll };
    enum TRIPOSITIONS { open1, open3, open5, closed1, closed3, closed5 };

    
    public static class Globals
    {
        public const string flatsymbol = "♭";
        public const string sharpsymbol = "♯";

        public const string errornote = "Помилка введення ноти";
        public const string warning = "Попередження";
        public const int A4 = 440; // камертон (стандарт = 440)        
        public const int fermata = 100; // пауза між рядками (реком.100)
        public const int tonalities = 15;
        public const int MidiFileType = 0;        
        public const int TrackNumber = 0;
        public const int ChannelNumber = 1;        
        public const int GMCorrection = 60;//translate abs pitch to midi note
        public const int GMOctaveCorrection = 4;//translate MIDI octave to classic octave
        public const int NotesInOctave = 12;
        public const int StepsInOctave = 7;

        public static int playspeed = 400;// тривалість звуку (реком.300 для біпера, 1000 - для NAudio)
        public static int PPQN = 480;
        public static int PlaySpeedLocal = 0;
        public static LNG lng = new(); // мова (укр. та англ.)
        public static Notation? notation = new(); // нотація (європейська та американська)
        public static PLAYER? player = null; // відтворення звуку
        public static TIMBRE timbre; // тембр звуку
        public static ConsoleColor[] colors = (ConsoleColor[])ConsoleColor.GetValues(typeof(ConsoleColor)); // кольори
        public static Interval[] allintervals =
        // список усіх інтервалів
        [
            new(0, 0),
            new(1, -1),
            new(1, 1),
            new(2, -1),
            new(2, 1),
            new(3, 0),
            new(3, 2),
            new(4, 0),
            new(5, -1),
            new(5, 1),
            new(6, -1),
            new(6, 1)
        ];
        public static Note SopranoH = new("a'"), SopranoL = new("c"), AltoH = new("e'"), AltoL = new("g,"),
                TenorH = new("a"), TenorL = new("c,"), BaseH = new("d"), BaseL = new("f,,");
        public static void Ukrainian()
        { Console.OutputEncoding = System.Text.Encoding.UTF8; }

        public static int GetTempo()
        {
            if (playspeed != 0)
                return 48000 / playspeed;
            else
                return 480;//за замовченням, відповідає 100 bpm
        }
        public static void SetTempo(int bpm)
        {
            if (bpm != 0)
                playspeed = 48000 / bpm;
            else
                playspeed = 480;//за замовченням, відповідає 100 bpm
        }

        public static void SetTempo(double bpm)
        {
            
            playspeed = (int)Math.Round(48000 / bpm);
            Console.WriteLine($"tempo = {bpm}bpm, playspeed = {playspeed} ms / quater" );

        }

        public static double GetBpmFromTempoEvent(TempoEvent tempoEvent)
        {
            double tempo = tempoEvent.Tempo;
            Console.WriteLine($"tempo from event = {tempo}");
            return tempo;
        }

        public static void GetPlayspeedFromTempoEvent(TempoEvent tempoEvent)
        {

            playspeed = (int)Math.Round(48000 / GetBpmFromTempoEvent(tempoEvent));
        }

    }

    public class IncorrectNote : Exception
    {
        protected string msg;
        protected string input;
        public IncorrectNote(string input, string msg = Globals.warning)
        {
            this.msg = msg + " : " + input + "\n";
        }

        public IncorrectNote()
        {
            msg = Globals.warning;
        }

        public override string Message
        {
            get { return msg; }
        }
    }
    public class ImpossibleVoicing : IncorrectNote
    {
        public ImpossibleVoicing() { msg = "impossible to find voicing"; }

    }


}





