using System;

namespace VedAstro.Library
{
    /// <summary>
    /// Horoscope calculator methods - each returns CalculatorResult for a specific yoga/combination.
    /// Used by EventManager.GetHoroscopeCalculatorMethod and HoroscopeDataListStatic.
    /// </summary>
    public static class CalculateHoroscope
    {
        [HoroscopeCalculator(HoroscopeName.MarsVenusIn7th)]
        public static CalculatorResult MarsVenusIn7th(Time birthTime) =>
            CalculatorResult.New(Calculate.PlanetRasiD1Sign(PlanetName.Mars, birthTime).GetSignName() == Calculate.HouseSignName(HouseName.House7, birthTime) ||
                                 Calculate.PlanetRasiD1Sign(PlanetName.Venus, birthTime).GetSignName() == Calculate.HouseSignName(HouseName.House7, birthTime));

        [HoroscopeCalculator(HoroscopeName.MercuryOrJupiterIn7th)]
        public static CalculatorResult MercuryOrJupiterIn7th(Time birthTime) =>
            CalculatorResult.New(Calculate.PlanetRasiD1Sign(PlanetName.Mercury, birthTime).GetSignName() == Calculate.HouseSignName(HouseName.House7, birthTime) ||
                                 Calculate.PlanetRasiD1Sign(PlanetName.Jupiter, birthTime).GetSignName() == Calculate.HouseSignName(HouseName.House7, birthTime));

        [HoroscopeCalculator(HoroscopeName.GajakesariYoga)]
        public static CalculatorResult GajakesariYoga(Time birthTime) =>
            CalculatorResult.New(Calculate.DistanceBetweenPlanets(PlanetName.Jupiter, PlanetName.Moon, birthTime).TotalDegrees < 90 ||
                                 Calculate.DistanceBetweenPlanets(PlanetName.Jupiter, PlanetName.Moon, birthTime).TotalDegrees > 270);

        [HoroscopeCalculator(HoroscopeName.SunaphaYoga)]
        public static CalculatorResult SunaphaYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.AnaphaYoga)]
        public static CalculatorResult AnaphaYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.DhurdhuraYoga)]
        public static CalculatorResult DhurdhuraYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.KemadrumaYoga)]
        public static CalculatorResult KemadrumaYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.ChandraMangalaYoga)]
        public static CalculatorResult ChandraMangalaYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.AdhiYoga)]
        public static CalculatorResult AdhiYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.ChatussagaraYoga)]
        public static CalculatorResult ChatussagaraYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.VasumathiYoga)]
        public static CalculatorResult VasumathiYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.SakataYoga)]
        public static CalculatorResult SakataYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.AmalaYoga)]
        public static CalculatorResult AmalaYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.ParvataYoga)]
        public static CalculatorResult ParvataYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.KahalaYoga)]
        public static CalculatorResult KahalaYoga(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga10)]
        public static CalculatorResult SunAshtakavargaYoga10(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga3)]
        public static CalculatorResult SunAshtakavargaYoga3(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga1A)]
        public static CalculatorResult MoonAshtakavargaYoga1A(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga2B)]
        public static CalculatorResult MoonAshtakavargaYoga2B(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga7)]
        public static CalculatorResult MarsAshtakavargaYoga7(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga8)]
        public static CalculatorResult MercuryAshtakavargaYoga8(Time birthTime) => CalculatorResult.NotOccuring();

        [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga12A)]
        public static CalculatorResult MercuryAshtakavargaYoga12A(Time birthTime) => CalculatorResult.NotOccuring();

        // Stub for any other HoroscopeName - add more as needed
        public static CalculatorResult Empty(Time birthTime) => CalculatorResult.NotOccuring();
    }
}
