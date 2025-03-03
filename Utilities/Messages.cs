using System.Drawing;
using static System.Console;
using static Music.Globals;

namespace Music
{
    public class Messages
    {

        private const string above_en = " above ";
        private const string above_uk = " вищі ";
        private const string aboveroot_en = "the root \n";
        private const string aboveroot_uk = "за основний тон щонайменш на нону - 1\n";
        private const string all_voicings_en = "0 - Search all voicings\n";
        private const string all_voicings_uk = "0 - Шукати усі обернення та розташування\n";
        private const string another_en = "\nTry another chord? 1 - yes, 0 - proceed with current chord\n";
        private const string another_uk = "\nСпробувати інший акорд? 1 - так, 0 - продовжити роботу з цим акордом\n";
        private const string anyway_en = " chord. Proceed anyway? \n";
        private const string anyway_uk = "акордом. Все одно продовжити? \n";

        private const string aug_int_en = "augmented ";
        private const string aug_int_uk = "збільшений ";


        private const string between_en = " between melody and root";
        private const string between_uk = "и між крайніми голосами";
        private const string btw1and5_en = "Choose between 1 and 5\n";
        private const string btw1and5_uk = "Оберіть від 1 до 5\n";
        private const string c_all_en = "\nAll ";
        private const string c_all_uk = "\nУсі ";
        private const string c_chords_en = " chords";
        private const string c_chords_uk = "акорди";
        private const string c_chordswith_en = " chords with ";
        private const string c_chordswith_uk = "акорди з ";
        private const string c11th_en = "11th";
        private const string c11th_uk = "ундецим";
        private const string c13th_en = "13th";
        private const string c13th_uk = "терцдецим";
        private const string c7th_en = "7th";
        private const string c7th_uk = "септ";
        private const string c9th_en = "9th";
        private const string c9th_uk = "нон";

        private const string checksounds_uk = "Перевірка Біпера. Якщо ви не почули звуковий сигнал, перевірте, чи включено звук у Мікшері гучності ('System sounds') ";
        private const string checksounds_en = "Checking Beeper. If you didn't here sound sygnal check if you System sounds are switched of in a Volume Mixer";
        private const string choose_action_en = "\nChoose action";
        private const string choose_action_uk = "\nОберіть операцію";
        private const string choose_chords_en = "1 - 7th chords\n2 - 9th chords \n3 - 11th chords \n4 - Extended with determined number of sounds \n5 - stats\n";
        private const string choose_chords_uk = "1 - Септакорди\n2 - Нонакорди\n3 - Ундецимакорди \n4 - Політерцієві із заданою кількістю звуків \n5 - статистика\n";
        private const string choose_interval_en = "choose interval";
        private const string choose_interval_uk = "Оберіть інтервал";
        private const string choose_intervals_en = "1 - unison, 2 - second, 3 - third, 4 - fourth, 5 - fifth, 6 - sixth, 7 - seventh, 8 - octave\n+ major, - minor, aug - augmentioned, dim - diminished";
        private const string choose_intervals_uk = "1 - прима, 2 - секунда, 3 - терція, 4-кварта, 5 - квінта, 6 - секста, 7 - септима, 8 - октава\n + великий, - малий, aug - збільшений, dim - зменшений";
        private static string choose_harmonisation_mode_en = "1 - triads, 2 - triads and inversions, 3 - all seventh, 4 - all";
        private static string choose_harmonisation_mode_uk = "1 - тризвуки, 2 - тризвуки з оберненнями, 3 - септакорди, 4 - всі акорди";
        private const string choose_model_en = "\n\nChoose model\n";
        private const string choose_model_uk = "\n\nОберіть модель\n";
        private const string choose_notation_en = "Сhoose notation";
        private const string choose_notation_uk = "Оберіть нотацію";
        private const string choose_player_en = "Choose player";
        private const string choose_player_uk = "Оберіть відтворювач звуку";
        private const string choose_program_en = "\nChoose program\n";
        private const string choose_program_uk = "\nОберіть програму\n";
        private const string choose_timbre_uk = "Оберіть тембр";
        private const string choose_timbre_en = "Choose timbre";
        private const string chord_choose_en = "\nchoose ";
        private const string chord_choose_uk = "\nоберіть вид ";
        private const string chord_entered_en = "\nChord entered: ";
        private const string chord_entered_uk = "\nАкорд введено: ";
        private const string chord_structure_en = " chord structure: \n";
        private const string chord_structure_uk = "акорда: \n";
        private const string chord_structure1_en = "1 - major, 2 - dominant, 3 - minor, 4 - major 6/9, 5 - augmented, 0 - enter manually ";
        private const string chord_structure1_uk = "1 - мажорний, 2 - домінантовий, 3 - мінорний, 4 - мажорний із секстою, 5 - збільшений, 0 - ввести вручну ";
        private const string chord_structure2_en = "1 - major, 2 - dominant, 3 - minor, 4 - major 6/9, 5 - diminished, 0 - enter manually ";
        private const string chord_structure2_uk = "1 - мажорний, 2 - домінантовий, 3 - мінорний, 4 - мажорний із секстою, 5 - зменшений, 0 - ввести вручну ";
        private const string chord_structure3_en = "1 - major, 2 - dominant, 3 - minor major, 4 - minor, 5 - diminished, 0 - enter manually ";
        private const string chord_structure3_uk = "1 - великий мажорний, 2 - малий мажорний, 3 - великий мінорний, 4 - малий мінорний, 5 - малий зменшений, 0 - ввести вручну ";
        private const string chordmode_en = "1 -chords, 2 - arpeggio\n";
        private const string chordmode_uk = "1 - акорди, 2 - арпеджіо\n";
        private const string chords_achieved_en = "\nChords achieved:\n";
        private const string chords_achieved_uk = "\nАкорди отримано:\n";
        private const string chords_containing_en = "\nChords containing ";
        private const string chords_containing_uk = "\nАкорди із ";

        private const string dim_int_en = "diminished ";
        private const string dim_int_uk = "зменшений ";


        private const string display_all_en = "Display all ";
        private const string display_all_uk = "Вивести усі ";
        private const string display_en = "Display ";
        private const string display_only_en = "Display only ";
        private const string display_only_uk = "Вивести лише ";
        private const string display_uk = "Вивести ";
        private const string enter_en = "Enter ";
        private const string enter_uk = "Введіть ";
        private const string enterc_en = "choose interval\n";
        private const string enterinterval_en = "оберіть інтервал (1 - unison, 2 - second, 3 - third, 4 - forth, 5 - fifth, 6 - sixth, 7 - seventh)\n";
        private const string enterinterval_uk = "оберіть інтервал (1 - прима, 2 - секунда, 3 - терція, 4 - кварта, 5 - квінта, 6 - секста, 7 - септима)\n";
        private const string entermaintone_uk = "введіть основний тон";
        private const string entermaintone_en = "enter main tone";
        private const string entertempo_en = "Enter tempo (20-500)\n";
        private const string entertempo_uk = "Введіть темп (20-500)\n";
        private const string enternotes_en = enter_en + notes_en;
        private const string enternotes_uk = enter_uk + notes_uk;
        private const string enternote_en = enter_en + note_en;
        private const string enternote_uk = enter_uk + note_uk;
        private const string enterspeed_uk = enter_uk + "тривалість нот у мс (1-2000)";
        private const string enterspeed_en = enter_en + "notes duration in ms (1-2000)";
        private static string enternumberofnotes_uk = "Введіть кількість нот";
        private static string enternumberofnotes_en = "Enter number of notes";

        private const string final_action_en = "\n1 - Play chords\n2 - Simplify alteration\n3 - Convert flats to sharps\n4 - Convert sharps to flats\n5 - Smart alteration\n6 - Save as text\n7 - Save as xml\n8 - Try another chord\n0 - Exit\n";
        private const string final_action_uk = "\n1 - Зіграти акорди\n2 - Спростити альтерацію\n3 - Перетворити бемолі на дієзи\n4 - Перетворити дієзи на бемолі\n5 - Розумна переальтерація \n6 - Зберегти як текст\n7 - Зберегти як xml\n8 - Cпробувати інший акорд\n0 - завершення роботи\n";
        private const string fsounds_en = " sounds";
        private const string fsounds_uk = " звуків";
        private const string i7th_en = "7th";
        private const string i7th_uk = "септим";
        private const string ifsave_en = "\nSave as file? 1 - yes, 2 - no";
        private const string ifsave_uk = "\nЗберегти у файл? 1 - так, 2 - ні";
        private const string iftrymore_uk = "Спробувати ще раз?";
        private const string iftrymore_en = "Do you want to try once more?";
        private const string in_melody_en = " in melody";
        private const string in_melody_uk = "ою в мелодичному положенні";
        private const string increase_range_en = " chords by range increase";
        private const string increase_range_mel_en = " chords by range increase with a given melody";
        private const string increase_range_mel_uk = "акорди за зростанням діапазону із заданим мелодичним тоном";
        private const string increase_range_root_en = " chords by range increase with a given root";
        private const string increase_range_root_uk = "акроди за зростанням діапазону від заданої ноти";
        private const string increase_range_uk = "акорди за зростанням діапазону";
        private const string incorrect_value_uk = "невірне значення";
        private const string incorrect_value_en = "incorrect value";
        private static string integer_uk = "ціле число";
        private static string integer_en = "integer";

        private const string intotal_en = "\ntotal: ";
        private const string intotal_uk = "\nусього: ";
        private const string inversions_melody_en = " and inversions with a given melody tone";
        private const string inversions_melody_uk = " та обернення із заданим мелодичним тоном";
        private const string inversions_order_en = " in inversions order";
        private const string inversions_order_uk = " в порядку обернень";
        private const string inversions_root_en = " and inversions from a given root";
        private const string inversions_root_uk = " та обернення від заданої ноти";
        private const string less_en = "the number must be less than ";
        private const string less_uk = "число має бути менше ніж ";
        private const string melody_en = "melody\n";
        private const string melody_uk = "мелодичний тон\n";

        private const string major_int_uk = "великий ";
        private const string major_int_en = "minor ";
        private const string major_ton_uk = "мажорний ";
        private const string major_ton_en = "major ";
        private const string minor_int_uk = "малий ";
        private const string minor_int_en = "minor ";
        private const string minor_ton_uk = "мінорний ";
        private const string minor_ton_en = "minor ";
        private const string major_uk = "мажор";
        private const string major_en = "major";
        private const string minor_uk = "мінор";
        private const string minor_en = "minor";

        private const string more_en = "the number must be more than ";
        private const string more_uk = "число має бути більше ніж ";
        private const string ndo_en = "do";
        private const string ndo_uk = "до";
        private const string nfa_en = "fa";
        private const string nfa_uk = "фа";
        private const string nla_en = "la";
        private const string nla_uk = "ля";
        private const string nmi_en = "mi";
        private const string nmi_uk = "мі";
        private const string noentered_en = "chord entered is not a ";
        private const string noentered_uk = "введений акорд не є ";
        private const string notationtypes_en = "\n1 - classic european\n2 - modern american\n";
        private const string notationtypes_uk = "\n1 - класична європейська нотація\n2 - американська нотація\n";
        private const string note_error_en = "Incorrect note ";
        private const string note_error_uk = "Помилка при введенні ноти ";
        private const string note_uk = " ноту\n";
        private const string note_en = " note\n";
        private const string notes_uk = " ноти\n";
        private const string notes_en = " notes\n";
        private const string notes_eu_en = "\nс - do, \td - re, \te - mi, \nf - fa, \tg - sol, \ta - la, \nb - si-b\th - si \nis - sharp, \tes - flat  \n";
        private const string notes_eu_uk = "\nс - до, \td - ре, \te - мі, \nf - фа, \tg - соль, \ta - ля, \nb - сі-бемоль\th - сі \nis - дiєз, \tes - бемоль  \n";
        private const string notes_us_en = "\n# - sharp, \tb - flat  \n";
        private const string notes_us_uk = "\nс - до, \td - ре, \te - мі, \nf - фа, \tg - соль, \ta - ля, \nb - сі \t#   - дiєз, \tb - бемоль  \n";
        private const string notes_octave_uk = "Октави:\n' - 2-га, '' - 3-я і т.д.\t , - мала ,, - велика і т.д. ";
        private const string notes_octave_en = "\n' - octaves above middle C\t , - octaves below middle C";
        private const string notes_durations_uk = "Тривалості:\n 1 - ціла, 2 - половинка, 4 - четвертна, . - з крапками";
        private const string notes_durations_en = "Durations:\n 1 - whole note 2 - half note, 4 - quater note, . - dotted";

        private const string notonality_en = "impossible to detect tonality";
        private const string notonality_uk = "неможливо визначити тональність";

        private const string nre_en = "re";
        private const string nre_uk = "ре";
        private const string nsi_en = "si";
        private const string nsi_uk = "сі";
        private const string nsol_en = "sol";
        private const string nsol_uk = "соль";
        private const string oneperrow_en = "one per row";
        private const string oneperrow_uk = "по одній на рядок";
        private const string range_order_en = " in ascending order of range\n";
        private const string range_order_uk = " в порядку зростання діапазону\n";
        private const string root_en = "root\n";
        private const string root_uk = "основний тон\n";
        private const string save_as_en = " Saved as ";
        private const string save_as_uk = " Збережено у файлі ";
        private const string sound_en = "\nsound ";
        private const string sound_uk = "\nзвук ";
        private const string sp_voicings_en = "1 - Search for voicing with 9th, 11th and 13th are more than octave ";
        private const string sp_voicings_uk = "1 - Шукати лише розташування, в яких 9-, 11- і 13- тони";
        private const string table_header_en = "\t\t\tTones\t\t| range\n \t\t\t\t\t|degrees / halftones | consonanse rate| sharpness ";
        private const string table_header_uk = "\t\t\tЗвуки\t\t| діапазон\n \t\t\t\t\t|ступенів / півтонів | консонансність | кв.коло ";
        private const string thank_en = "\nThank you for using our program!";
        private const string thank_uk = "\nДякуємо за використання програми!";
        private const string timbres_uk = "0 - синусоїда, 1 - трикутник, 2 - пила, 3 - прямокутник";
        private const string timbres_en = "0 - sin, 1 - triangle, 2 - sawtooth, 3 - square";
        private const string tonetable_en = "\t\t\tPitches\t\t| range\n \t\t\t\t\t|degree | halftones\n";
        private const string tonetable_uk = "\t\t\tЗвуки\t\t| діапазон\n \t\t\t\t\t|ступенів | півтонів\n";
        private const string toomuchnotes_uk = "Забагто нот в акорді, фільтрацію скасовано";
        private const string toomuchnotes_en = "Chord does not as much sounds, remain unfiltered";
        private const string try_or_more_en = "1 - yes, 2 - try once more\n";
        private const string try_or_more_uk = "1 - так, 2 - спробувати знову\n";
        private const string trymore_en = "Try once more";
        private const string trymore_uk = "Спробуйте ще раз";
        private const string upto12_en = "number of notes (up to 12)\n";
        private const string upto12_uk = "кількість нот(до 12)\n";
        private const string use_eu_en = "\nUse european traditional notation to enter notes:";
        private const string use_eu_uk = "\nДля введення нот використовуйте європейську буквенну нотацію:";
        private const string use_us_en = "\nUse american scientific notation to enter notes:";
        private const string use_us_uk = "\nДля введення нот використовуйте американську буквенну нотацію:";
        private const string with_interval_en = " with ";
        private const string with_interval_uk = " з інтервалом ";
        private static string yesno_en = "1 - yes, 2 - no";
        private static string yesno_uk = "1 - так, 2 - ні";
        
        private const string zeroend_en = "to finish entering enter \"0\" in a new line";
        private const string zeroend_uk = "для завершення вводу введіть \"0\" з нового рядку \n";

        // CHORDS


        public const string SEPT_uk = "септакорд";
        public const string TERZQ_uk = "терцквартакорд";
        public const string QUINTS_uk = "квінтсекстакорд";
        public const string SEC_uk = "секундакорд";
        public const string TRI_uk = "тризвук";
        public const string SEXT_uk = "секстакорд";
        public const string QSEXT_uk = "квартсекстакорд";
        public const string HSEPT_uk = "неповний септакорд";
        public const string HSEC_uk = "неповний секундакорд";
        public const string HQUINTS_uk = "неповний квінтсекстакорд";
        public const string ADDSEC_uk = "тризвук з доданою секундою";
        public const string ADDQUARTA_uk = "септакорд з доданою квартою";
        public const string SUSQUARTA_uk = "септакорд з затриманою квартою";
        public const string NONACORD_uk = "нонакорд";
        public const string CORD69_uk = "нонакорд з секстою";
        public const string NONACORD_1i_uk = "нонакорд в 1-му оберненні";
        public const string NONACORD_2i_uk = "нонакорд в 2-му оберненні";
        public const string NONACORD_3i_uk = "нонакорд в 3-му оберненні";
        public const string NONACORD_4i_uk = "нонакорд в 4-му оберненні";
        public const string UNDECCORD_uk = "ундецимакорд";
        public const string HUNDECCORD_uk = "ундецимакорд";
        public const string HUNDECCORD_1i_uk = "неповний ундецимакорд в 1-му оберненні";
        public const string HUNDECCORD_2i_uk = "неповний ундецимакорд в 2-му оберненні";
        public const string HUNDECCORD_3i_uk = "неповний ундецимакорд в 3-му оберненні";
        public const string HUNDECCORD_4i_uk = "неповний ундецимакорд в 4-му оберненні";
        public const string UNDECCORD_1i_uk = "ундецимакорд в 1-му оберненні";
        public const string UNDECCORD_2i_uk = "ундецимакорд в 2-му оберненні";
        public const string UNDECCORD_3i_uk = "ундецимакорд в 3-му оберненні";
        public const string UNDECCORD_4i_uk = "ундецимакорд в 4-му оберненні";
        public const string UNDECCORD_5i_uk = "ундецимакорд в 5-му оберненні";
        public const string TERZDEC_uk = "терцдецимакорд";
        public const string CLUSTER_uk = "кластер";

        public const string SEPT_en = "seventhchord";
        public const string TERZQ_en = "seventhchord in 2nd inversion";
        public const string QUINTS_en = "seventhchord in 1st inversion";
        public const string SEC_en = "seventhchord in 3rd inversion";
        public const string TRI_en = "triad";
        public const string SEXT_en = "triadin 1st inversion";
        public const string QSEXT_en = "triad in 2nd inversion";
        public const string HSEPT_en = "incomplete seventhchord";
        public const string HSEC_en = "incomplete seventhchord in 3rd inversion";
        public const string HQUINTS_en = "incomplete seventhchord in 1 st inversion";
        public const string ADDSEC_en = "triad with add 2";
        public const string ADDQUARTA_en = "seventhchord with added 4";
        public const string SUSQUARTA_en = "seventhchord with sustained 4";
        public const string NONACORD_en = "ninth chord";
        public const string CORD69_en = "ninth chord with sixth";
        public const string NONACORD_1i_en = "ninth chord in 1st inversion";
        public const string NONACORD_2i_en = "ninth chord in 2nd inversion";
        public const string NONACORD_3i_en = "ninth chord in 3rd inversion";
        public const string NONACORD_4i_en = "ninth chord in 4th inversion";
        public const string UNDECCORD_en = "eleventh chord";
        public const string HUNDECCORD_en = "incomplete eleventh chord";
        public const string HUNDECCORD_1i_en = "incomplete eleventh chord in 1st inversion";
        public const string HUNDECCORD_2i_en = "incomplete eleventh chord in 2nd inversion";
        public const string HUNDECCORD_3i_en = "incomplete eleventh chordв in 3rd inversion";
        public const string HUNDECCORD_4i_en = "incomplete eleventh chordв in 4th inversion";
        public const string UNDECCORD_1i_en = "eleventh chord in 1st inversion";
        public const string UNDECCORD_2i_en = "eleventh chord in 2nd inversion";
        public const string UNDECCORD_3i_en = "eleventh chord in 3rd inversion";
        public const string UNDECCORD_4i_en = "eleventh chord in 4th inversion";
        public const string UNDECCORD_5i_en = "eleventh chord in 5th inversion";
        public const string TERZDEC_en = "thirteenth chord";
        public const string CLUSTER_en = "cluster";




        public static void Color(int color)
        {
            ForegroundColor = (ConsoleColor)color;
        }
        public static void Message(int color, string msg)
        {
            ForegroundColor = (ConsoleColor)color;
            Write(msg);
            ResetColor();
        }
        public static void GrayMessage(string msg)
        {
            ForegroundColor = (ConsoleColor)COLORS.gray;
            Write(msg);
            ResetColor();
        }

        public static void GrayMessageL(string msg)
        {
            ForegroundColor = (ConsoleColor)COLORS.gray;
            WriteLine(msg);
            ResetColor();
        }

        public static void ErrorMessage(string msg)
        {
            ForegroundColor = (ConsoleColor)COLORS.red;
            Write(msg);
            ResetColor();
        }

        public static void Header(string msg)
        {
            ForegroundColor = (ConsoleColor)11;
            WriteLine(msg);
            ResetColor();
        }


        public static void MessageL(COLORS color, string msg)
        {
            ForegroundColor = (ConsoleColor)color;
            WriteLine(msg);
            ResetColor();
        }
        public static void Message(COLORS color, string msg)
        {
            ForegroundColor = (ConsoleColor)color;
            Write(msg);
            ResetColor();
        }

        public static void MessageL(int color, string msg)
        {
            ForegroundColor = (ConsoleColor)color;
            WriteLine(msg);
            ResetColor();
        }

            public static string above()
            {
                if (lng == LNG.uk) return above_uk;
                else return above_en;
            }
            public static string another()
            {
                if (lng == LNG.uk) return another_uk;
                else return another_en;
            }

            public static string anyway()
            {
                if (lng == LNG.uk) return anyway_uk;
                else return anyway_en;
            }


            public static string all_voicings()
            {
                if (lng == LNG.uk) return all_voicings_uk;
                else return all_voicings_en;
            }

           public static string aboveroot()
            {
                if (lng == LNG.uk) return aboveroot_uk;
                else return aboveroot_en;
            }

            public static string between()
            {
                if (lng == LNG.uk) return between_uk;
                else return between_en;
            }

            public static string btw1and5()
            {
                if (lng == LNG.uk) return btw1and5_uk;
                else return btw1and5_en;
            }

            public static string checkSounds()
            {
                if (lng == LNG.uk) return checksounds_uk;
                else return checksounds_en;
            }

            public static string chooseTimbre()
            {
                if (lng == LNG.uk) return choose_timbre_uk;
                else return choose_timbre_en;
            }

            public static string c7th()
            {
                if (lng == LNG.uk) return c7th_uk;
                else return c7th_en;
            }

            public static string i7th()
            {
                if (lng == LNG.uk) return i7th_uk;
                else return i7th_en;
            }

            public static string c9th()
            {
                if (lng == LNG.uk) return c9th_uk;
                else return c9th_en;
            }
            public static string c11th()
            {
                if (lng == LNG.uk) return c11th_uk;
                else return c11th_en;
            }
            public static string c13th()
            {
                if (lng == LNG.uk) return c13th_uk;
                else return c13th_en;
            }



            public static string c_all()
            {
                if (lng == LNG.uk) return c_all_uk;
                else return c_all_en;
            }
            public static string c_chords()
            {
                if (lng == LNG.uk) return c_chords_uk;
                else return c_chords_en;
            }

            public static string c_chordswith()
            {
                if (lng == LNG.uk) return c_chordswith_uk;
                else return c_chordswith_en;
            }
            public static string chords_containing()
            {
                if (lng == LNG.uk) return chords_containing_uk;
                else return chords_containing_en;
            }
            public static string chord_entered()
            {
                if (lng == LNG.uk) return chord_entered_uk;
                else return chord_entered_en;
            }
            public static string chord_structure()
            {
                if (lng == LNG.uk) return chord_structure_uk;
                else return chord_structure_en;
            }

            public static string chord_choose()
            {
                if (lng == LNG.uk) return chord_choose_uk;
                else return chord_choose_en;
            }

            public static string chord_structure1()
            {
                if (lng == LNG.uk) return chord_structure1_uk;
                else return chord_structure1_en;
            }
            public static string chord_structure2()
            {
                if (lng == LNG.uk) return chord_structure2_uk;
                else return chord_structure2_en;
            }
            public static string chord_structure3()
            {
                if (lng == LNG.uk) return chord_structure3_uk;
                else return chord_structure3_en;
            }

            public static string chords_achieved()
            {
                if (lng == LNG.uk) return chords_achieved_uk;
                else return chords_achieved_en;
            }
            public static string chordmode()
            {
                if (lng == LNG.uk) return chordmode_uk;
                else return chordmode_en;
            }

            public static string choose_action()
            {
                if (lng == LNG.uk) return choose_action_uk;
                else return choose_action_en;
            }
            public static string choose_notation()
            {
                if (lng == LNG.uk) return choose_notation_uk;
                else return choose_notation_en;
            }

            public static string choose_player()
            {
                if (lng == LNG.uk) return choose_player_uk;
                else return choose_player_en;

            }

            public static string choose_program()
            {
                if (lng == LNG.uk) return choose_program_uk;
                else return choose_program_en;
            }

            public static string choose_chords()
            {
                if (lng == LNG.uk) return choose_chords_uk;
                else return choose_chords_en;
            }

            public static string choose_interval()
            { if (lng == LNG.uk) return choose_interval_uk;
                else return choose_interval_en;
            }

            public static string choose_intervals()
            {
                if (lng == LNG.uk) return choose_intervals_uk;
                else return choose_intervals_en;
            }

            public static string choose_harmonisation_mode()
            {
                if (lng == LNG.uk) return choose_harmonisation_mode_uk;
                else return choose_harmonisation_mode_en;

            
            }

            public static string choose_model()
            {
                if (lng == LNG.uk) return choose_model_uk;
                else return choose_model_en;
            }

            public static string display_only()
            {
                if (lng == LNG.uk) return display_only_uk;
                else return display_only_en;
            }
            public static string display_all()
            {
                if (lng == LNG.uk) return display_all_uk;
                else return display_all_en;
            }
            public static string display()
            {
                if (lng == LNG.uk) return display_uk;
                else return display_en;
            }
            public static string enter()
            {
                if (lng == LNG.uk) return enter_uk;
                else return enter_en;
            }

            public static string enterinterval()
            {
                if (lng == LNG.uk) return enterinterval_uk;
                else return enterinterval_en;
            }

            public static string entermaintone()
            {
                if (lng == LNG.uk) return entermaintone_uk;
                else return entermaintone_en;
            }


        public static string enternotes()
            {
                if (lng == LNG.uk) return enternotes_uk;
                else return enternotes_en;
            }

            public static string enternote()
            {
                if (lng == LNG.uk) return enternote_uk;
                else return enternote_en;
            }


        public static string enterspeed()
        {
            if (lng == LNG.uk) return enterspeed_uk;
            else return enterspeed_en;
        }
        public static string enternumberofnotes()
            {
                if (lng == LNG.uk) return enternumberofnotes_uk;
                else return enternumberofnotes_en;
            }
            public static string entertempo()
            {
                if (lng == LNG.uk) return entertempo_uk;
                else return entertempo_en;
            }
            public static string final_action()
            {
                if (lng == LNG.uk) return final_action_uk;
                else return final_action_en;
            }
            public static string fsounds()
            {
                if (lng == LNG.uk) return fsounds_uk;
                else return fsounds_en;
            }

            public static string iftrymore()
            {
                if (lng == LNG.uk) return iftrymore_uk;
                else return iftrymore_en;

            }
            public static string incorrect_value()
            {
                if (lng == LNG.uk) return incorrect_value_uk;
                else return incorrect_value_en;
            }
            public static string increase_range_mel()
            {
                if (lng == LNG.uk) return increase_range_mel_uk;
                else return increase_range_mel_en;
            }

            public static string increase_range()
            {
                if (lng == LNG.uk) return increase_range_uk;
                else return increase_range_en;
            }

            public static string increase_range_root()
            {
                if (lng == LNG.uk) return increase_range_root_uk;
                else return increase_range_root_en;
            }
            public static string integer()
            {
                if (lng == LNG.uk) return integer_uk;
                else return integer_en;
            }

            public static string inversions_root()
            {
                if (lng == LNG.uk) return inversions_root_uk;
                else return inversions_root_en;
            }

            public static string inversions_melody()
            {
                if (lng == LNG.uk) return inversions_melody_uk;
                else return inversions_melody_en;
            }

            public static string ifsave()
            {
                if (lng == LNG.uk) return ifsave_uk;
                else return ifsave_en;
            }

            public static string intotal()
            {
                if (lng == LNG.uk) return intotal_uk;
                else return intotal_en;
            }

            public static string inversions_order()
            {
                if (lng == LNG.uk) return inversions_order_uk;
                else return inversions_order_en;
            }


            public static string less()
            {
                if (lng == LNG.uk) return less_uk;
                else return less_en;
            }

            public static string melody()
            {
                if (lng == LNG.uk) return melody_uk;
                else return melody_en;
            }

            public static string major_int()
            {
                if (lng == LNG.uk) return major_int_uk;
                else return major_int_en;
            }

            public static string major_ton()
            {
                if (lng == LNG.uk) return major_ton_uk;
                else return major_ton_en;
            }

            public static string minor_int()
            {
                if (lng == LNG.uk) return minor_int_uk;
                else return minor_int_en;
            }

            public static string minor()
            {
                if (lng == LNG.uk) return minor_uk;
                else return minor_en;
            }
            public static string major()
            {
                if (lng == LNG.uk) return major_uk;
                else return major_en;
            }

            public static string dim_int()
            {
                if (lng == LNG.uk) return dim_int_uk;
                else return dim_int_en;
            }

            public static string aug_int()
            {
                if (lng == LNG.uk) return aug_int_uk;
                else return aug_int_en;
            }

            public static string minor_ton()
            {
                if (lng == LNG.uk) return minor_ton_uk;
                else return minor_ton_en;
            }

            public static string more()
            {
                if (lng == LNG.uk) return more_uk;
                else return more_en;
            }
            public static string notationtypes()
            {
                if (lng == LNG.uk) return notationtypes_uk;
                else return notationtypes_en;
            }
            public static string noentered()
            {
                if (lng == LNG.uk) return noentered_uk;
                else return noentered_en;
            }

            public static string note_error()
            {
                if (lng == LNG.uk) return note_error_uk;
                else return note_error_en;
            }

            public static string notes_eu()
            {
                if (lng == LNG.uk) return notes_eu_uk;
                else return notes_eu_en;
            }

            public static string notes_us()
            {
                if (lng == LNG.uk) return notes_us_uk;
                else return notes_us_en;
            }

        public static string notes_durations()
        {
            if (lng == LNG.uk) return notes_durations_uk;
            else return notes_durations_en;
        }

        public static string notes_octave()
        {
            if (lng == LNG.uk) return notes_octave_uk;
            else return notes_octave_en;
        }

            public static string notonality()
                    {
                if (lng == LNG.uk) return notonality_uk;
                else return notonality_en;
            }


        public static string ndo()
            {
                if (lng == LNG.uk) return ndo_uk;
                else return ndo_en;
            }

            public static string nre()
            {
                if (lng == LNG.uk) return nre_uk;
                else return nre_en;
            }

            public static string nmi()
            {
                if (lng == LNG.uk) return nmi_uk;
                else return nmi_en;
            }

            public static string nfa()
            {
                if (lng == LNG.uk) return nfa_uk;
                else return nfa_en;
            }

            public static string nsol()
            {
                if (lng == LNG.uk) return nsol_uk;
                else return nsol_en;
            }

            public static string nla()
            {
                if (lng == LNG.uk) return nla_uk;
                else return nla_en;
            }

            public static string nsi()
            {
                if (lng == LNG.uk) return nsi_uk;
                else return nsi_en;
            }

            public static string oneperrow()
            {
                if (lng == LNG.uk) return oneperrow_uk;
                else return oneperrow_en;
            }


            public static string root()
            {
                if (lng == LNG.uk) return root_uk;
                else return root_en;
            }

            public static string in_melody()
            {
                if (lng == LNG.uk) return in_melody_uk;
                else return in_melody_en;
            }

            public static string players()
            {
                return "\n1 - beeper; 2 - naudio; 3 - midiPlayer\n";
            }

            public static string save_as()
            {
                if (lng == LNG.uk) return save_as_uk;
                else return save_as_en;
            }
            public static string range_order()
            {
                if (lng == LNG.uk) return range_order_uk;
                else return range_order_en;
            }
            public static string sound()
            {
                if (lng == LNG.uk) return sound_uk;
                else return sound_en;
            }
            public static string sp_voicings()
            {
                if (lng == LNG.uk) return sp_voicings_uk;
                else return sp_voicings_en;
            }

            void tablestats()
            {
                if (lng == LNG.en) Console.WriteLine("\nnumberofnotes \tsteps \tchords possible \t");
                else Console.WriteLine("\nкількість нот \tступені \tакордів існує \t");
            }

            public static string timbres()
            {
                if (lng == LNG.uk) return timbres_uk;
                else return timbres_en;
            }

            public static string thank()
            {
                if (lng == LNG.uk) return thank_uk;
                else return thank_en;
            }
            public static string tonetable()
            {
                if (lng == LNG.uk) return tonetable_uk;
                else return tonetable_en;
            }


            public static string table_header()
            {
                if (lng == LNG.uk) return table_header_uk;
                else return table_header_en;
            }

            public static string toomuchnotes()
            {
                if (lng == LNG.uk) return toomuchnotes_uk;
                else return toomuchnotes_en;
            }
        public static string try_or_more()
            {
                if (lng == LNG.uk) return try_or_more_uk;
                else return try_or_more_en;
            }

            public static string trymore()
            {
                if (lng == LNG.uk) return trymore_uk;
                else return trymore_en;
            }

            public static string upto12()
            {
                if (lng == LNG.uk) return upto12_uk;
                else return upto12_en;
            }
            public static string use_eu()
            {
                if (lng == LNG.uk) return use_eu_uk;
                else return use_eu_en;
            }

            public static string use_us()
            {
                if (lng == LNG.uk) return use_us_uk;
                else return use_us_en;
            }

            public static string with_interval()
            {
                if (lng == LNG.uk) return with_interval_uk;
                else return with_interval_en;
            }

            public static string yesno()
            {
                if (lng == LNG.uk) return yesno_uk;
                else return yesno_en;
            }
            public static string zeroend()
            {
                if (lng == LNG.uk) return zeroend_uk;
                else return zeroend_en;
            }


            public static string SEPT()
            {
                if (lng == LNG.uk) return SEPT_uk;
                else return SEPT_en;
            }
            public static string TERZQ()
            {
                if (lng == LNG.uk) return TERZQ_uk;
                else return TERZQ_en;
            }
            public static string QUINTS()
            {
                if (lng == LNG.uk) return QUINTS_uk;
                else return QUINTS_en;
            }
            public static string SEC()
            {
                if (lng == LNG.uk) return SEC_uk;
                else return SEC_en;
            }
            public static string TRI()
            {
                if (lng == LNG.uk) return TRI_uk;
                else return TRI_en;
            }
            public static string SEXT()
            {
                if (lng == LNG.uk) return SEXT_uk;
                else return SEXT_en;
            }
            public static string QSEXT()
            {
                if (lng == LNG.uk) return SEPT_uk;
                else return SEPT_en;
            }
            public static string HSEPT()
            {
                if (lng == LNG.uk) return HSEPT_uk;
                else return HSEPT_en;
            }
            public static string HSEC()
            {
                if (lng == LNG.uk) return HSEC_uk;
                else return HSEC_en;
            }
            public static string HQUINTS()
            {
                if (lng == LNG.uk) return HQUINTS_uk;
                else return HQUINTS_en;
            }
            public static string ADDSEC()
            {
                if (lng == LNG.uk) return ADDSEC_uk;
                else return ADDSEC_en;
            }
            public static string ADDQUARTA()
            {
                if (lng == LNG.uk) return ADDQUARTA_uk;
                else return ADDQUARTA_en;
            }
            public static string SUSQUARTA()
            {
                if (lng == LNG.uk) return SUSQUARTA_uk;
                else return SUSQUARTA_en;
            }
            public static string NONACORD()
            {
                if (lng == LNG.uk) return NONACORD_uk;
                else return NONACORD_en;
            }
            public static string CORD69()
            {
                if (lng == LNG.uk) return CORD69_uk;
                else return CORD69_en;
            }
            public static string NONACORD_1i()
            {
                if (lng == LNG.uk) return NONACORD_1i_uk;
                else return NONACORD_1i_en;
            }
            public static string NONACORD_2i()
            {
                if (lng == LNG.uk) return NONACORD_2i_uk;
                else return NONACORD_2i_en;
            }
            public static string NONACORD_3i()
            {
                if (lng == LNG.uk) return NONACORD_3i_uk;
                else return NONACORD_3i_en;
            }
            public static string NONACORD_4i()
            {
                if (lng == LNG.uk) return NONACORD_4i_uk;
                else return NONACORD_4i_en;
            }
            public static string UNDECCORD()
            {
                if (lng == LNG.uk) return UNDECCORD_uk;
                else return UNDECCORD_en;
            }
            public static string HUNDECCORD()
            {
                if (lng == LNG.uk) return HUNDECCORD_uk;
                else return HUNDECCORD_en;
            }
            public static string HUNDECCORD_1i()
            {
                if (lng == LNG.uk) return HUNDECCORD_1i_uk;
                else return HUNDECCORD_1i_en;
            }
            public static string HUNDECCORD_2i()
            {
                if (lng == LNG.uk) return HUNDECCORD_2i_uk;
                else return HUNDECCORD_2i_en;
            }
            public static string HUNDECCORD_3i()
            {
                if (lng == LNG.uk) return HUNDECCORD_3i_uk;
                else return HUNDECCORD_3i_en;
            }
            public static string HUNDECCORD_4i()
            {
                if (lng == LNG.uk) return HUNDECCORD_4i_uk;
                else return HUNDECCORD_4i_en;
            }
            public static string UNDECCORD_1i()
            {
                if (lng == LNG.uk) return UNDECCORD_1i_uk;
                else return UNDECCORD_1i_en;
            }
            public static string UNDECCORD_2i()
            {
                if (lng == LNG.uk) return UNDECCORD_2i_uk;
                else return UNDECCORD_2i_en;
            }
            public static string UNDECCORD_3i()
            {
                if (lng == LNG.uk) return UNDECCORD_3i_uk;
                else return UNDECCORD_3i_en;
            }
            public static string UNDECCORD_4i()
            {
                if (lng == LNG.uk) return UNDECCORD_4i_uk;
                else return UNDECCORD_4i_en;
            }
            public static string UNDECCORD_5i()
            {
                if (lng == LNG.uk) return UNDECCORD_5i_uk;
                else return UNDECCORD_5i_en;
            }
            public static string TERZDEC()
            {
                if (lng == LNG.uk) return TERZDEC_uk;
                else return TERZDEC_en;
            }
            public static string CLUSTER()
            {
                if (lng == LNG.uk) return CLUSTER_uk;
                else return CLUSTER_en;
            }


    }
}

