using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// El ritmo: a que velocidad corre el juego y a que velocidad corre el ciclo dia/noche.
    /// </summary>
    /// <remarks>
    /// <b>De donde sale.</b> Estas dos ideas —separar la velocidad del juego de la del reloj,
    /// poder darle ritmos distintos al dia y a la noche, y dejar el cielo girando con la
    /// simulacion en pausa— son las de «Play It!» de keallu (MIT). La implementacion de aqui
    /// es propia; lo adoptado es el conjunto de funciones, no su codigo.
    ///
    /// <b>Por que el ciclo es solo visual.</b> Acelerar el ciclo de verdad significa mover
    /// <c>SimulationManager.m_dayTimeOffsetFrames</c>, que es estado de simulacion y viaja
    /// dentro de la partida guardada. Este mod no puede dejar rastro en una partida por el
    /// mero hecho de estar activo, asi que en vez de eso se reescribe
    /// <c>DayNightProperties.m_TimeOfDay</c>, que solo alimenta la imagen. El reloj de la
    /// ciudad y las politicas nocturnas siguen su curso normal; lo que cambia de ritmo es el
    /// cielo. Con la velocidad en 1 no se escribe nada distinto de lo que ya habia.
    ///
    /// <b>Por que se mide el ritmo en vez de calcularlo.</b> Cuantas horas avanza el juego por
    /// segundo real depende de la velocidad de simulacion elegida, de <see cref="GameSpeed"/>
    /// y de los fotogramas que de la maquina. En vez de suponerlo, se observa cuanto se movio
    /// el reloj del juego en el ultimo fotograma y se reescala. Asi velocidad 1 es exactamente
    /// el juego sin tocar, y el ritmo aprendido es lo que permite seguir girando en pausa,
    /// cuando el juego ya no mueve nada.
    /// </remarks>
    internal static class TimeController
    {
        internal const float MinGameSpeed = 0.1f;
        internal const float MaxGameSpeed = 5f;
        internal const float MinCycleSpeed = 0f;
        internal const float MaxCycleSpeed = 10f;

        /// <summary>Multiplicador de <see cref="Time.timeScale"/>. 1 = el juego sin tocar.</summary>
        internal static float GameSpeed = 1f;

        /// <summary>Si este mod manda en el ritmo del ciclo dia/noche.</summary>
        internal static bool CycleSpeedEnabled;

        /// <summary>Ritmo del ciclo. 1 = el del juego; 0 = detenido.</summary>
        internal static float CycleSpeed = 1f;

        /// <summary>Ritmo de la noche cuando <see cref="SeparateDayNight"/> esta activo.</summary>
        internal static float NightCycleSpeed = 1f;

        /// <summary>Si el dia y la noche llevan ritmos distintos.</summary>
        internal static bool SeparateDayNight;

        /// <summary>Si el cielo sigue girando con la simulacion en pausa.</summary>
        internal static bool CycleWhilePaused;

        private static bool _timeScaleTouched;
        private static DayNightProperties _dayNight;
        private static int _searchCooldown;

        private static float _lastGameTod = -1f;   // 0..1, tal como lo dejo el juego
        private static float _ownTod = -1f;        // 0..1, lo que imponemos
        private static float _learnedRate;         // vueltas de reloj por segundo real

        private static DayNightProperties DayNight
        {
            get
            {
                if (_dayNight == null)
                {
                    if (_searchCooldown <= 0)
                    {
                        _searchCooldown = 60;
                        _dayNight = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
                    }
                    else
                    {
                        _searchCooldown--;
                    }
                }

                return _dayNight;
            }
        }

        internal static void ClearCache()
        {
            _dayNight = null;
            _searchCooldown = 0;
            _lastGameTod = -1f;
            _ownTod = -1f;
            _learnedRate = 0f;
        }

        /// <summary>Lleva <see cref="GameSpeed"/> al motor.</summary>
        internal static void ApplyGameSpeed(float speed)
        {
            GameSpeed = Mathf.Clamp(speed, MinGameSpeed, MaxGameSpeed);
            PushGameSpeed();
        }

        private static void PushGameSpeed()
        {
            float s = Mathf.Clamp(GameSpeed, MinGameSpeed, MaxGameSpeed);
            if (Mathf.Approximately(s, 1f))
            {
                // Solo se devuelve a 1 si fuimos nosotros quienes lo movimos: si otro mod
                // esta usando timeScale, no se lo pisamos al pasar por aqui.
                if (_timeScaleTouched)
                {
                    Time.timeScale = 1f;
                    _timeScaleTouched = false;
                }

                return;
            }

            Time.timeScale = s;
            _timeScaleTouched = true;
        }

        internal static void Tick()
        {
            var dayNight = DayNight;
            if (dayNight == null)
            {
                return;
            }

            float now = dayNight.m_TimeOfDay;

            // Con la hora clavada manda WorldController: aqui solo se toma nota de donde quedo
            // el reloj, para no dar un salto cuando se suelte.
            if (!CycleSpeedEnabled || WorldController.TimeLocked)
            {
                _lastGameTod = now;
                _ownTod = now;
                return;
            }

            if (_lastGameTod < 0f)
            {
                _lastGameTod = now;
                _ownTod = now;
                return;
            }

            float advanced = Mathf.Repeat(now - _lastGameTod, 1f);
            if (advanced > 0.5f)
            {
                // Un salto hacia atras no es el ciclo avanzando: es alguien fijando la hora,
                // o una partida que acaba de cargar. Se reengancha sin arrastrar el salto.
                advanced = 0f;
                _ownTod = now;
            }

            _lastGameTod = now;

            float real = Time.unscaledDeltaTime;
            if (advanced > 0f && real > 0.0001f)
            {
                float observed = advanced / real;
                _learnedRate = _learnedRate <= 0f ? observed : Mathf.Lerp(_learnedRate, observed, 0.05f);
            }

            float speed = SpeedFor(_ownTod);
            float step = advanced * speed;

            if (advanced <= 0f && CycleWhilePaused && _learnedRate > 0f)
            {
                // El juego no movio el reloj (pausa). Se mueve con el ritmo aprendido.
                step = _learnedRate * real * speed;
            }

            if (step <= 0f && speed > 0f && advanced <= 0f)
            {
                return;
            }

            _ownTod = Mathf.Repeat(_ownTod + step, 1f);
            dayNight.m_TimeOfDay = _ownTod;
        }

        private static float SpeedFor(float normalizedTimeOfDay)
        {
            float day = Mathf.Clamp(CycleSpeed, MinCycleSpeed, MaxCycleSpeed);
            if (!SeparateDayNight)
            {
                return day;
            }

            return IsNight(normalizedTimeOfDay)
                ? Mathf.Clamp(NightCycleSpeed, MinCycleSpeed, MaxCycleSpeed)
                : day;
        }

        private static bool IsNight(float normalizedTimeOfDay)
        {
            var sim = SimulationManager.instance;
            if (sim != null)
            {
                return sim.m_isNightTime;
            }

            float hour = normalizedTimeOfDay * 24f;
            return hour < 6f || hour >= 20f;
        }

        /// <summary>Devuelve el ritmo al juego y suelta el reloj.</summary>
        internal static void Restore()
        {
            CycleSpeedEnabled = false;
            GameSpeed = 1f;
            PushGameSpeed();
            _lastGameTod = -1f;
            _ownTod = -1f;
            _learnedRate = 0f;
        }
    }
}
