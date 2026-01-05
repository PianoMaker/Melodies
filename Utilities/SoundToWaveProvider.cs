using Music;
using NAudio.Dsp;
using NAudio.Wave;
using static Music.Messages;

namespace Melodies25.Utilities
{
    public class SoundToWaveProvider : WaveProvider32
    {
        private readonly List<Sound> _sequence;
        private int _currentIndex = 0;
        private int _samplesRemaining;
        private double _phase;
        private double _phaseIncrement;
        private int _sampleRate;
        private int _noteIndex = 0; // Відстежуємо поточну ноту

        private EnvelopeGenerator _adsr;
        private float attackSeconds;
        new public WaveFormat WaveFormat { get; }
        public float AttackSeconds
        {
            get => attackSeconds;
            set
            {
                attackSeconds = value;
                _adsr.AttackRate = attackSeconds * WaveFormat.SampleRate;
            }
        }

        private float decaySeconds;
        public float DecaySeconds
        {
            get => decaySeconds;
            set
            {
                decaySeconds = value;
                _adsr.DecayRate = decaySeconds * WaveFormat.SampleRate;
            }
        }

        private float sustainLevel;
        public float SustainLevel
        {
            get => sustainLevel;
            set
            {
                sustainLevel = value;
                _adsr.SustainLevel = sustainLevel;
            }
        }

        private float releaseSeconds;
        public float ReleaseSeconds
        {
            get => releaseSeconds;

            set
            {
                releaseSeconds = value;
                _adsr.ReleaseRate = releaseSeconds * WaveFormat.SampleRate;
            }
        }

        public SoundToWaveProvider(List<Sound> sequence, int sampleRate = 44100)
            : base(sampleRate, 1) // Виклик конструктора базового класу з WaveFormat
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence)); // Перевірка на null
            _sampleRate = sampleRate;
            var channels = 1; // Mono
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            _adsr = new EnvelopeGenerator();

            //Defaults
            AttackSeconds = 0.01f;
            DecaySeconds = 0.4f;
            SustainLevel = 0.5f;
            ReleaseSeconds = 0.3f;

            _adsr.Gate(true);
        }

        public SoundToWaveProvider(List<Sound> sequence, float attack, float decay, float sustain, float release, int sampleRate = 44100)
                : base(sampleRate, 1) // Виклик конструктора базового класу з WaveFormat
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence)); // Перевірка на null
            _sampleRate = sampleRate;
            var channels = 1; // Mono
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            _adsr = new EnvelopeGenerator();

            //Defaults
            AttackSeconds = attack;
            DecaySeconds = decay;
            SustainLevel = sustain;
            ReleaseSeconds = release;

            _adsr.Gate(true);
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            int samplesPerMs = _sampleRate / 1000;
            int index = 0;

            while (index < count && _noteIndex < _sequence.Count)
            {
                var sound = _sequence[_noteIndex];

                //паузи представлені як звуки 0 hz
                if (sound.Frequency == 0)
                {
                    if (_samplesRemaining <= 0)
                    {
                        _adsr.Gate(false);  // Запуск фази релізу
                        Console.WriteLine($"Release phase started for note {_noteIndex}");
                        _samplesRemaining = sound.Duration * samplesPerMs; // Задаємо тривалість релізу
                    }
                }
                //нормальні ноти
                else
                {
                    if (_samplesRemaining <= 0) // Початок нової ноти
                    {
                        _phaseIncrement = 2 * Math.PI * sound.Frequency / _sampleRate;
                        _samplesRemaining = sound.Duration * samplesPerMs;
                        _adsr.Gate(true);  // Запуск ADSR
                        MessageL(COLORS.gray, $"Recording note {_noteIndex}: {sound.Frequency} Hz, {sound.Duration} ms, {sound.Amplitude} %");
                    }
                }

                int samplesToProcess = Math.Min(_samplesRemaining, count - index);

                //обчислення значень амплітуди для кожного семплу з урахуванням ADSR
                for (int i = 0; i < samplesToProcess; i++, index++)
                {
                    float amplitude = _adsr.Process();
                    buffer[offset + index] = SynthFormula(_phase) * amplitude * sound.Amplitude;
                    _phase += _phaseIncrement;
                    if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
                }

                _samplesRemaining -= samplesToProcess;

                if (_samplesRemaining <= 0) // Закінчили поточну ноту
                {
                    _adsr.Gate(false);  // Закриття ASDR
                                        //Console.WriteLine($"Finished note {_noteIndex}: {frequency} Hz");

                    _noteIndex++; // Перехід до наступної ноти
                }
            }

            return index;
        }

        private float SynthFormula(double phase)
        {
            return (float)Math.Sin(_phase); // Синусоїда. 
                                            //Згодом слід додати інші!                                            
        }
    }
}

 