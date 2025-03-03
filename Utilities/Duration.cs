﻿using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using static Music.Globals;

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
            int value = 1;
            if (digit == 0) throw new IncorrectNote("Incorrect duration: 0");
            while (digit % 2 == 0)
            {
                digit /= 2;
                value *= 2;
            }
            duration = (DURATION)value;
            tuplet = digit;
            modifier = DURMODIFIER.none;
        }
        public Duration(int digit)
        {
            Digit_to_duration(digit);
            modifier = DURMODIFIER.none;
        }


        public Duration(int mididuration, int tickperquater)
        {
            Messages.GrayMessageL($"input: {mididuration} : {tickperquater}");

            int whole = tickperquater * quatersPerWholeNote;
            var base64th = Math.Round((float)tickperquater / quaterContains64thNotes);

            if (whole % mididuration == 0)
            {
                Digit_to_duration(whole / mididuration);
            }
            else
            {
                int dur64th = (int)Math.Round(mididuration / base64th);
                DetermineDuration(dur64th);
            }
        }

        private void DetermineDuration(int dur64th)
        {
            Messages.GrayMessage("determine method: ");
            int[] baseDurations = { 1, 2, 4, 8, 16, 32 };
            modifier = DURMODIFIER.none;

            foreach (var baseDur in baseDurations)
            {
                if (dur64th == baseDur)
                {
                    Messages.GrayMessageL($"dur = {baseDur}");
                    duration = (DURATION)baseDur;
                    return;
                }
                else if (dur64th == (int)(baseDur * 1.5))
                {
                    Messages.GrayMessageL($"dur = {baseDur}.");
                    duration = (DURATION)baseDur;
                    modifier = DURMODIFIER.dotted;
                    return;
                }
                else if (dur64th == (int)(baseDur * 1.75))
                {
                    Messages.GrayMessageL($"dur = {baseDur}..");
                    duration = (DURATION)baseDur;
                    modifier = DURMODIFIER.doubledotted;
                    return;
                }
                else if (dur64th == (int)(baseDur * 1.875))
                {
                    Messages.GrayMessageL($"dur = {baseDur}...");
                    duration = (DURATION)baseDur;
                    modifier = DURMODIFIER.tripledotted;
                    return;
                }
            }

            modifier = DURMODIFIER.tuplet; // Якщо тривалість не відповідає стандартним значенням
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

        public double RelDuration()
        {
            double relduration = 4;
            relduration /= (int)duration;
            if (modifier == DURMODIFIER.none) relduration *= 1;
            else if (modifier == DURMODIFIER.dotted) relduration *= 1.5;
            else if (modifier == DURMODIFIER.doubledotted) relduration *= 1.75;
            else if (modifier == DURMODIFIER.tripledotted) relduration *= 1.875;
            if (tuplet > 0) relduration /= tuplet;
            return relduration;
        }

        public int MidiDuration(int PPQN)
        {
            return PPQN * (int)RelDuration();
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

        public string Symbol()
        {
            string symbol = "";
            switch (duration)
            {
                case DURATION.whole: symbol += "𝅝"; break;    // Ціла нота (U+1D15D)
                case DURATION.half: symbol += "𝅗𝅥"; break;     // Половинна нота (U+1D15E)
                case DURATION.quater: symbol += "♩"; break;  // Чверть нота (U+2669)
                case DURATION.eigth: symbol += "♪"; break;    // Восьма нота (U+266A)
                case DURATION.sixteenth: symbol += "𝅘𝅥𝅯"; break; // Шістнадцята нота (U+266B)                
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
