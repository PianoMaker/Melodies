using static Music.Messages;
using static Music.Engine;
using static System.Console;
using static System.Convert;
using System.Collections.ObjectModel;

namespace Music
{
    public class StringOut
    {
        private List<string> chords;

        public StringOut(List<string> chords)
        {
            this.chords = chords;
        }

        public List<string> Chords
        {
            get { return chords; }
            set { chords = value; }
        }

        // Якщо один акорд, то повертає string
        public string Display()
        {
            return string.Join(" ", chords.Select(note => key_to_notename(note)));
        }

        // Якщо масив акордів, то повертає ObservableCollection<string>
        public static ObservableCollection<string> Display(List<string> chords, int color = 14)
        {
            var output = new ObservableCollection<string>
            {
                string.Join(" ", chords.Select(note => key_to_notename(note)))
            };
            return output;
        }

        public static string Display(Note note, bool octtrigger = true, int color = 7)
        {
            if (octtrigger)
                return $"{note.GetName()} ({ToInt32(note.Oct)})";
            else
                return $"{note.GetName()}";
        }

        public static ObservableCollection<string> Display(List<Note> notes, bool octtrigger = true, int color = 14)
        {
            var output = new ObservableCollection<string>
            {
                string.Join(" ", notes.Select(note => Display(note, octtrigger, color)))
            };
            return output;
        }

        public static string Display<T>(T scale, bool octtrigger = true, int color = 14) where T : Scale
        {
            return string.Join(" ", scale.Notes.Select(note => Display(note, octtrigger, color)));
        }

        public static ObservableCollection<string> Display<T>(T[] scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                output.Add(Display(scale, octtrigger, color));
            }
            return output;
        }

        public static ObservableCollection<string> Display<T>(List<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                output.Add(Display(scale, octtrigger, color));
            }
            return output;
        }

        public static ObservableCollection<string> Display<T>(Queue<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                output.Add(Display(scale, octtrigger, color));
            }
            return output;
        }

        public static ObservableCollection<string> Display<T>(LinkedList<T> chords, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T chord in chords)
            {
                output.Add(Display(chord));
            }
            return output;
        }

        public static ObservableCollection<string> Display<T>(List<List<T>> chords, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            int counter = 0;
            foreach (List<T> chordList in chords)
            {
                foreach (T chord in chordList)
                {
                    counter++;
                    output.Add($"{counter}: {Display(chord, octtrigger, color)}");
                }
            }
            return output;
        }

        public static string DisplaySh<T>(T scale, bool octtrigger = true, int color = 14) where T : Scale
        {
            string result = string.Join(" ", scale.Notes.Select(note => Display(note, octtrigger, color)));
            float sharpness = scale.Sharpness();
            result += sharpness > 0 ? $" +{sharpness:f2}" : $" {sharpness:f2}";
            return result;
        }

        public static ObservableCollection<string> DisplayR_Sh<T>(T scale, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            string result = string.Join(" ", scale.Notes.Select(note => Display(note, octtrigger, color)));
            float sharpness = scale.Sharpness();
            result += sharpness > 0 ? $"\t+{sharpness:f2}\t{scale.Range()}" : $"\t{sharpness:f2}\t{scale.Range()}";
            output.Add(result);
            return output;
        }

        public static ObservableCollection<string> DisplaySh<T>(List<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                output.Add(DisplaySh(scale, octtrigger, color));
            }
            return output;
        }

        public static ObservableCollection<string> DisplayR_Sh<T>(List<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                foreach (var item in DisplayR_Sh(scale, octtrigger, color))
                {
                    output.Add(item);
                }
            }
            return output;
        }

        public static ObservableCollection<string> DisplaySh<T>(List<List<T>> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (List<T> scaleList in scales)
            {
                foreach (var scale in DisplaySh(scaleList, octtrigger, color))
                {
                    output.Add(scale);
                }
            }
            return output;
        }

        public static ObservableCollection<string> DisplayInline<T>(T scale) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (Note note in scale.Notes)
            {
                output.Add(Display(note));
            }
            return output;
        }

        public static ObservableCollection<string> DisplayInline<T>(List<T> scales) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                foreach (var item in DisplayInline(scale))
                {
                    output.Add(item);
                }
            }
            return output;
        }

        public static ObservableCollection<string> DisplayInline<T>(T[] scales) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                foreach (var item in DisplayInline(scale))
                {
                    output.Add(item);
                }
            }
            return output;
        }

        public static ObservableCollection<string> DisplayTable<T>(T scale) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (Note note in scale.Notes)
            {
                output.Add(Display(note));
            }
            return output;
        }

        public static ObservableCollection<string> DisplayTable<T>(List<T> scales) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                foreach (var item in DisplayTable(scale))
                {
                    output.Add(item);
                }
            }
            return output;
        }

        public static ObservableCollection<string> DisplayTable<T>(T[] scales) where T : Scale
        {
            var output = new ObservableCollection<string>();
            foreach (T scale in scales)
            {
                foreach (var item in DisplayTable(scale))
                {
                    output.Add(item);
                }
            }
            return output;
        }

        public static string ConvertFlats(string input)
        {
            return input.Replace('b', '♭');
        }
        public static ObservableCollection<string> ConvertFlatsInCollection(ObservableCollection<string> collection)
        {
            var convertedCollection = new ObservableCollection<string>();
            foreach (var item in collection)
            {
                convertedCollection.Add(ConvertFlats(item));
            }
            return convertedCollection;
        }

    }
}
