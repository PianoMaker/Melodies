using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using static Music.Globals;
using static Music.Messages;

namespace Music
{
    public class Duration : ICloneable
    {
        private const int quatersPerWholeNote = 4;
        private const int quaterContains64thNotes = 16;
        private DURATION duration;
        private DURMODIFIER modifier;
        private int tuplet;

        public Duration(DURATION duration)
        {
            this.duration = duration;
        }
        public Duration()
        { duration = DURATION.quater; modifier = DURMODIFIER.none; tuplet = 1; }

        public Duration(DURATION duration, DURMODIFIER modifier, int tuplet)
        {
            this.duration = duration;
            this.modifier = modifier;
            this.tuplet = tuplet;
        }

        private void Digit_to_duration(int digit)
        {
            //Messages.GrayMessage($"standart method ({digit}): ");
            int value = 1;
            if (digit == 0) throw new IncorrectNote("Incorrect duration: 0");
            CountValue(ref digit, ref value);
            modifier = DURMODIFIER.none;
        }

        private void CountValue(ref int digit, ref int value)
        {
            while (digit % 2 == 0)
            {
                digit /= 2;
                value *= 2;
            }
            duration = (DURATION)value;
            tuplet = digit;
        }

        public Duration(int digit)
        {
            Digit_to_duration(digit);
            modifier = DURMODIFIER.none;
        }

        public Duration(int mididuration, int tickperquater)
        {
           // Messages.GrayMessageL($"input: {mididuration} : {tickperquater}");

            int whole = tickperquater * quatersPerWholeNote;
            float base64th = (float)tickperquater / quaterContains64thNotes;

            if (whole % mididuration == 0)
            {
                Digit_to_duration(whole / mididuration);
            }
            else
            {
                double dur64th = mididuration / base64th;
                // Messages.GrayMessage($"determine method ({mididuration} / {base64th}) = {dur64th} ");
                DetermineDuration(dur64th);
            }
        }

        private void DetermineDuration(double dur64th)
        {
            int[] baseDurations = { 1, 2, 4, 8, 16, 32 };
            modifier = DURMODIFIER.none;

            foreach (var baseDur in baseDurations)
            {
                if (dur64th > baseDur*0.9 && dur64th < baseDur * 1.2)
                {
                    //Messages.GrayMessageL($"dur = {baseDur}");
                    duration = ConvertValue(baseDur);
                    return;
                }
                else if (dur64th >= baseDur * 1.2 && dur64th < baseDur * 1.3)
                {
                    duration = ConvertValue(baseDur);
                    //Messages.GrayMessageL($"dur = {duration}_");
                    modifier = DURMODIFIER.tied;                    
                    return;
                }
                else if (dur64th >= baseDur * 1.3 && dur64th < baseDur * 1.4)
                {
                    duration = ConvertValue(baseDur);
                    //Messages.GrayMessageL($"dur = {duration}/3");
                    modifier = DURMODIFIER.tuplet;
                    tuplet = 3;
                    return;
                }
                else if (dur64th >= (int)(baseDur * 1.4) && dur64th <= (int)(baseDur * 1.6))
                {
                    //Messages.GrayMessageL($"dur = {baseDur}.");
                    duration = ConvertValue(baseDur);
                    modifier = DURMODIFIER.dotted;
                    return;
                }
                else if (dur64th > (int)(baseDur * 1.6) && dur64th <= (int)(baseDur * 1.8))
                {
                    //GrayMessageL($"dur = {baseDur}..");
                    duration = ConvertValue(baseDur);
                    modifier = DURMODIFIER.doubledotted;
                    return;
                }
                else if (dur64th > (int)(baseDur * 1.8) && dur64th <= (int)(baseDur * 1.9))
                {
                    //GrayMessageL($"dur = {baseDur}...");
                    duration = ConvertValue(baseDur);
                    modifier = DURMODIFIER.tripledotted;
                    return;
                }
            }
            //GrayMessageL($"dur = indef");
            modifier = DURMODIFIER.tuplet; // Якщо тривалість не відповідає стандартним значенням
            duration = DURATION.quater; // за замовченням
        }

        private DURATION ConvertValue(int baseDur)
        {
            const int total64th = quatersPerWholeNote * quaterContains64thNotes; // 4 * 16 = 64
            if (baseDur <= 0) return DURATION.quater; // fallback
            
            int value = total64th / baseDur;
            // value тепер: baseDur=32 -> 2 (half), baseDur=16 -> 4 (quater), baseDur=1 -> 64 (sixtyfourth)
            return (DURATION)value;
        }

        public Duration(int digit, string? modifier)
        {
            Digit_to_duration(digit);
            switch (modifier)
            {
                case ".": this.modifier = DURMODIFIER.dotted; break;
                case "..": this.modifier = DURMODIFIER.doubledotted; break;
                case "...": this.modifier = DURMODIFIER.tripledotted; break;
                case "/": this.modifier = DURMODIFIER.tuplet; break;
                default: this.modifier = DURMODIFIER.none; break;
            }
        }

        public DURATION Dur
        { get { return duration; } set { duration = value; } }

        //повертає відносну тривалість у четвертних долях
        public double RelDuration()
        {
            double relduration = 4;
            relduration /= (double)duration;
            if (modifier == DURMODIFIER.none) relduration *= 1;
            else if (modifier == DURMODIFIER.dotted) relduration *= 1.5;
            else if (modifier == DURMODIFIER.doubledotted) relduration *= 1.75;
            else if (modifier == DURMODIFIER.tripledotted) relduration *= 1.875;
            if (tuplet > 0)
            {
                relduration /= tuplet;
                relduration = Math.Round(relduration, 2);
            }
            //GrayMessageL($"relduration = {relduration}");
            return relduration;
        }

        public int MidiDuration(int PPQN)
        {
            return (int)(PPQN * RelDuration());
            //Pulses Per Quarter Note - міряє тіки на чвертку
        }

        public int AbsDuration()
        {
            if (PlaySpeedLocal > 0)
                return (int)(PlaySpeedLocal * RelDuration());
            else return (int)(playspeed * RelDuration());
        }

        public object Clone()
        {
            return new Duration(this.duration, this.modifier, this.tuplet);
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append(duration.ToString());
            if (modifier != DURMODIFIER.none)
                str.Append(" " + modifier.ToString());
            if (tuplet != 1)
                str.Append(" tuplet: " + tuplet.ToString());
            return str.ToString();
        }

        public string Symbol(bool rest=false)
        {
            string symbol = "";
            if (!rest)
            {
                switch (duration)
                {
                    case DURATION.whole: symbol += "𝅝"; break;    // Ціла нота (U+1D15D)
                    case DURATION.half: symbol += "𝅗𝅥"; break;     // Половинна нота (U+1D15E)
                    case DURATION.quater: symbol += "♩"; break;  // Чверть нота (U+2669)
                    case DURATION.eigth: symbol += "♪"; break;    // Восьма нота (U+266A)
                    case DURATION.sixteenth: symbol += "𝅘𝅥𝅯"; break; // Шістнадцята нота (U+266B)                
                    case DURATION.thirtysecond: symbol += "𝅘𝅥𝅰"; break; // Шістнадцята нота (U+266B)   
                    default: return "??"; // Невідомий символ
                }
            }
            else
                switch (duration)
                {
                    case DURATION.whole: symbol += "𝄻"; break;     // Ціла пауза (U+1D13B)
                    case DURATION.half: symbol += "𝄼"; break;      // Половинна пауза (U+1D13C)
                    case DURATION.quater: symbol += "𝄽"; break;    // Чвертна пауза (U+1D13D)
                    case DURATION.eigth: symbol += "𝄾"; break;     // Восьма пауза (U+1D13E)
                    case DURATION.sixteenth: symbol += "𝄿"; break; // Шістнадцята пауза (U+1D13F)
                    case DURATION.thirtysecond: symbol += "𝅀"; break;
                    default: return "??"; // Невідомий символ
                }

            switch (modifier)
            {
                default: break;
                case DURMODIFIER.dotted: symbol += "."; break;
                case DURMODIFIER.doubledotted: symbol += ".."; break;
                case DURMODIFIER.tripledotted: symbol += ".."; break;
                case DURMODIFIER.tuplet: symbol += $"/{tuplet}"; break;
            }
            return symbol;
        }
    }
}
