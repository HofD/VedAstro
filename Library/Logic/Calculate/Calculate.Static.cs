using SwissEphNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static VedAstro.Library.PlanetName;

namespace VedAstro.Library
{
    /// <summary>
    /// Static properties and base calculation methods for VedAstro.
    /// This partial class contains declarations that were split from the original Calculate class.
    /// </summary>
    public partial class Calculate
    {
        // ReSharper disable once InconsistentNaming - matches enum type name collision, use explicit property
        private static int _ayanamsa = 46; // LAHIRI_ICRC default

        /// <summary>
        /// Ayanamsa type for sidereal calculations (Swiss Ephemeris sid_mode).
        /// Default: Lahiri ICRC (46).
        /// </summary>
        public static int Ayanamsa
        {
            get => _ayanamsa;
            set => _ayanamsa = value;
        }

        /// <summary>
        /// Solar year length in days for Dasa calculations.
        /// Default varies by Ayanamsa (Raman=360, Krishnamurti=365.2564).
        /// </summary>
        public static double SolarYearTimeSpan { get; set; } = 365.2564;

        /// <summary>
        /// Use Mean vs True for Rahu/Ketu nodes.
        /// </summary>
        public static bool UseMeanRahuKetu { get; set; }

        /// <summary>
        /// Convert longitude to LMT offset (1 degree = 4 minutes).
        /// </summary>
        public static TimeSpan LongitudeToLMTOffset(double longitude)
        {
            var totalMinutes = longitude * 4.0;
            return TimeSpan.FromMinutes(totalMinutes);
        }

        /// <summary>
        /// Convert LMT to Standard time.
        /// </summary>
        public static DateTimeOffset LmtToStd(LocalMeanTime lmtDateTime, TimeSpan stdOffset)
        {
            var lmtOffset = LongitudeToLMTOffset(lmtDateTime.Longitude);
            var lmtAsOffset = new DateTimeOffset(lmtDateTime.Date, lmtOffset);
            return lmtAsOffset.ToOffset(stdOffset);
        }

        /// <summary>
        /// Convert Time to Julian Day in UT (Universal Time).
        /// </summary>
        public static double TimeToJulianUniversalTime(Time time)
        {
            var utc = time.GetStdDateTimeOffset().UtcDateTime;
            using var swissEph = new SwissEph();
            return swissEph.swe_julday(utc.Year, utc.Month, utc.Day, utc.Hour + utc.Minute / 60.0 + utc.Second / 3600.0, SwissEph.SE_GREG_CAL);
        }

        /// <summary>
        /// Convert Time to Julian Day in ET (Ephemeris Time).
        /// </summary>
        public static double TimeToJulianEphemerisTime(Time time)
        {
            var jdUt = TimeToJulianUniversalTime(time);
            double deltaT;
            using (var swissEph = new SwissEph())
                deltaT = swissEph.swe_deltat(jdUt);
            return jdUt + deltaT / 86400.0;
        }

        /// <summary>
        /// Gets Local Mean Time at Greenwich (UTC) in Julian days.
        /// At Greenwich LMT = UTC, so this is the JD in UT for the given time.
        /// </summary>
        public static double GreenwichLmtInJulianDays(Time time) => TimeToJulianUniversalTime(time);

        /// <summary>
        /// Converts Julian days (UT at Greenwich) to DateTimeOffset at UTC.
        /// </summary>
        public static DateTimeOffset GreenwichTimeFromJulianDays(double julianDays)
        {
            using var swissEph = new SwissEph();
            int year = 0, month = 0, day = 0;
            double hour = 0;
            swissEph.swe_revjul(julianDays, SwissEph.SE_GREG_CAL, ref year, ref month, ref day, ref hour);
            var hourPart = (int)hour;
            var minPart = (int)((hour - hourPart) * 60);
            var secPart = (int)((hour - hourPart - minPart / 60.0) * 3600);
            var dt = new DateTime(year, month, day, hourPart, minPart, secPart, DateTimeKind.Utc);
            return new DateTimeOffset(dt, TimeSpan.Zero);
        }

        /// <summary>
        /// Local apparent time from Swiss Eph (LMT + equation of time).
        /// </summary>
        public static DateTime LocalApparentTime(Time time)
        {
            var jdUt = TimeToJulianUniversalTime(time);
            using var swissEph = new SwissEph();
            string errMsg = "";
            swissEph.swe_time_equ(jdUt, out var eqOfTime, ref errMsg);
            var lmt = time.GetLmtDateTimeOffset();
            var lat = lmt.AddDays(eqOfTime);
            return lat.DateTime;
        }

        /// <summary>
        /// Convert address string to GeoLocation (sync wrapper - uses cache).
        /// </summary>
        public static GeoLocation AddressToGeoLocation(string address)
        {
            return new LocationManager().AddressToGeoLocation(address).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get timezone string for a location at given time.
        /// </summary>
        public static async Task<string> GeoLocationToTimezone(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
        {
            return await new LocationManager().GeoLocationToTimezone(geoLocation, timeAtLocation);
        }

        /// <summary>
        /// Get Nirayana (sidereal) longitude of planet.
        /// </summary>
        public static Angle PlanetNirayanaLongitude(PlanetName planetName, Time time)
        {
            var swissPlanet = Tools.VedAstroToSwissEph(planetName);
            if (planetName == Ketu)
            {
                var rahuLong = PlanetNirayanaLongitude(Rahu, time);
                return Angle.FromDegrees((rahuLong.TotalDegrees + 180) % 360);
            }

            var julDayUt = TimeToJulianUniversalTime(time);
            var location = time.GetGeoLocation();

            using var swissEph = new SwissEph();
            swissEph.swe_set_sid_mode(Ayanamsa, 0, 0);

            double[] results = new double[6];
            string errMsg = "";
            var iflag = SwissEph.SEFLG_SWIEPH | SwissEph.SEFLG_SIDEREAL;
            swissEph.swe_calc_ut(julDayUt, swissPlanet, iflag, results, ref errMsg);

            return Angle.FromDegrees(results[0]);
        }

        /// <summary>
        /// Get Sayana (tropical) longitude of planet.
        /// </summary>
        public static Angle PlanetSayanaLongitude(PlanetName planetName, Time time)
        {
            var swissPlanet = Tools.VedAstroToSwissEph(planetName);
            if (planetName == Ketu)
            {
                var rahuLong = PlanetSayanaLongitude(Rahu, time);
                return Angle.FromDegrees((rahuLong.TotalDegrees + 180) % 360);
            }

            var julDayUt = TimeToJulianUniversalTime(time);
            double[] results = new double[6];
            string errMsg = "";
            var iflag = SwissEph.SEFLG_SWIEPH;
            using var swissEph = new SwissEph();
            swissEph.swe_calc_ut(julDayUt, swissPlanet, iflag, results, ref errMsg);

            return Angle.FromDegrees(results[0]);
        }

        /// <summary>
        /// Get Rasi (D1) sign of planet at time.
        /// </summary>
        public static ZodiacSign PlanetRasiD1Sign(PlanetName planetName, Time time)
        {
            var longitude = PlanetNirayanaLongitude(planetName, time);
            return ZodiacSignAtLongitude(longitude);
        }

        /// <summary>
        /// Gets longitudinal space between 2 planets (PlanetName overload).
        /// Uses Nirayana longitudes. Returns directed arc from planet1 to planet2 (0-360°).
        /// </summary>
        public static Angle DistanceBetweenPlanets(PlanetName planet1, PlanetName planet2, Time time)
        {
            var long1 = PlanetNirayanaLongitude(planet1, time);
            var long2 = PlanetNirayanaLongitude(planet2, time);
            return DistanceBetweenPlanetsDirected(long1, long2);
        }

        /// <summary>
        /// Gets longitudinal space between 2 planets (Angle overload).
        /// Expects you to calculate longitude. Returns shortest arc 0-180° for conjunction checks.
        /// </summary>
        public static Angle DistanceBetweenPlanets(Angle longitude1, Angle longitude2)
        {
            var a = longitude1.Normalize360().TotalDegrees;
            var b = longitude2.Normalize360().TotalDegrees;
            var diff = Math.Abs(a - b);
            if (diff > 180) diff = 360 - diff;
            return Angle.FromDegrees(diff);
        }

        /// <summary>
        /// Directed arc from longitude1 to longitude2 (0-360°).
        /// </summary>
        private static Angle DistanceBetweenPlanetsDirected(Angle longitude1, Angle longitude2)
        {
            var a = longitude1.Normalize360().TotalDegrees;
            var b = longitude2.Normalize360().TotalDegrees;
            var diff = (b - a + 360) % 360;
            if (diff < 0) diff += 360;
            return Angle.FromDegrees(diff);
        }

        /// <summary>
        /// Convert longitude to ZodiacSign.
        /// </summary>
        public static ZodiacSign ZodiacSignAtLongitude(Angle longitude)
        {
            var totalDeg = longitude.Normalize360().TotalDegrees;
            var signIndex = (int)(totalDeg / 30.0) % 12;
            var degreesInSign = totalDeg % 30.0;
            if (degreesInSign < 0) degreesInSign += 30;

            var signName = ZodiacSign.All12ZodiacNames[signIndex];
            return new ZodiacSign(signName, Angle.FromDegrees(degreesInSign));
        }

        /// <summary>
        /// <summary>
        /// Get constellation at given longitude.
        /// </summary>
        public static Constellation ConstellationAtLongitude(Angle longitude)
        {
            var totalDeg = longitude.Normalize360().TotalDegrees;
            var nakshatraIndex = (int)(totalDeg / 13.333333333333334) % 27;
            if (nakshatraIndex < 0) nakshatraIndex += 27;
            var degreesInNakshatra = totalDeg % 13.333333333333334;
            if (degreesInNakshatra < 0) degreesInNakshatra += 13.333333333333334;
            var quarter = (int)(degreesInNakshatra / 3.3333333333333335) + 1;
            if (quarter > 4) quarter = 4;
            var degInConst = new Angle(0, (int)(degreesInNakshatra * 60), 0);
            return new Constellation(nakshatraIndex + 1, quarter, degInConst);
        }

        /// <summary>
        /// Get zodiac sign of house at time.
        /// </summary>
        public static ZodiacName HouseSignName(HouseName houseNumber, Time time)
        {
            var houses = AllHouseLongitudes(time);
            var house = houses.Find(h => h.GetHouseName() == houseNumber);
            return ZodiacSignAtLongitude(house.GetMiddleLongitude()).GetSignName();
        }

        /// <summary>
        /// Get all house zodiac signs at time.
        /// </summary>
        public static Dictionary<HouseName, ZodiacSign> AllHouseZodiacSigns(Time time)
        {
            var houses = AllHouseLongitudes(time);
            var result = new Dictionary<HouseName, ZodiacSign>();
            foreach (var house in houses)
            {
                result[house.GetHouseName()] = ZodiacSignAtLongitude(house.GetMiddleLongitude());
            }
            return result;
        }

        /// <summary>
        /// Zodiac signs owned by a planet.
        /// </summary>
        public static List<ZodiacName> ZodiacSignsOwnedByPlanet(PlanetName planet)
        {
            if (planet == PlanetName.Sun) return new() { ZodiacName.Leo };
            if (planet == PlanetName.Moon) return new() { ZodiacName.Cancer };
            if (planet == PlanetName.Mars) return new() { ZodiacName.Aries, ZodiacName.Scorpio };
            if (planet == PlanetName.Mercury) return new() { ZodiacName.Gemini, ZodiacName.Virgo };
            if (planet == PlanetName.Jupiter) return new() { ZodiacName.Sagittarius, ZodiacName.Pisces };
            if (planet == PlanetName.Venus) return new() { ZodiacName.Taurus, ZodiacName.Libra };
            if (planet == PlanetName.Saturn) return new() { ZodiacName.Capricorn, ZodiacName.Aquarius };
            if (planet == PlanetName.Rahu || planet == PlanetName.Ketu) return new();
            return new();
        }

        /// <summary>
        /// Lord of zodiac sign.
        /// </summary>
        public static PlanetName LordOfZodiacSign(ZodiacName signName)
        {
            return signName switch
            {
                ZodiacName.Aries => Mars,
                ZodiacName.Taurus => Venus,
                ZodiacName.Gemini => Mercury,
                ZodiacName.Cancer => Moon,
                ZodiacName.Leo => Sun,
                ZodiacName.Virgo => Mercury,
                ZodiacName.Libra => Venus,
                ZodiacName.Scorpio => Mars,
                ZodiacName.Sagittarius => Jupiter,
                ZodiacName.Capricorn => Saturn,
                ZodiacName.Aquarius => Saturn,
                ZodiacName.Pisces => Jupiter,
                _ => Empty
            };
        }

        /// <summary>
        /// Vedic day of week.
        /// </summary>
        public static DayOfWeek DayOfWeek(Time time)
        {
            var dt = time.GetStdDateTimeOffset().DateTime;
            return (DayOfWeek)((int)dt.DayOfWeek);
        }

        /// <summary>
        /// Main Pancha Pakshi bird activity.
        /// </summary>
        public static BirdActivity MainActivity(Time birthTime, Time currentTime)
        {
            return PanchaPakshi.MainActivity(birthTime, currentTime);
        }

        /// <summary>
        /// Next zodiac sign in order.
        /// </summary>
        public static ZodiacName NextZodiacSign(ZodiacName sign)
        {
            var next = (int)sign + 1;
            if (next > 11) next = 0;
            return (ZodiacName)next;
        }

        /// <summary>
        /// Next house number.
        /// </summary>
        public static HouseName NextHouseNumber(HouseName house)
        {
            var next = (int)house + 1;
            if (next > 12) next = 1;
            return (HouseName)next;
        }

        /// <summary>
        /// Auto-calculate time range for events (1-arg overload).
        /// </summary>
        public static (Time start, Time end) AutoCalculateTimeRange(Time birthTime)
        {
            var start = birthTime.AddYears(-1);
            var end = birthTime.AddYears(100);
            return (start, end);
        }

        /// <summary>
        /// Auto-calculate time range for events from preset (age1to10, 3weeks, fulllife, 19902000, etc.).
        /// </summary>
        public static (Time start, Time end) AutoCalculateTimeRange(Time birthTime, string timePreset, TimeSpan outputTimezone)
        {
            var (start, end) = ParseTimeRangePreset(birthTime, timePreset, outputTimezone);
            return (start, end);
        }

        /// <summary>
        /// Days between start and end of time range preset.
        /// </summary>
        public static double DaysBetweenTimeRangePreset(Time birthTime, string timePreset, TimeSpan outputTimezone)
        {
            var (start, end) = ParseTimeRangePreset(birthTime, timePreset, outputTimezone);
            return (end.GetStdDateTimeOffset() - start.GetStdDateTimeOffset()).TotalDays;
        }

        private static (Time start, Time end) ParseTimeRangePreset(Time birthTime, string timePreset, TimeSpan outputTimezone)
        {
            var location = birthTime.GetGeoLocation();
            var birthStd = birthTime.GetStdDateTimeOffset();
            if (string.IsNullOrEmpty(timePreset)) return (birthTime.AddYears(-1), birthTime.AddYears(100));

            // age1to10, age4to50, etc.
            if (timePreset.StartsWith("age", StringComparison.OrdinalIgnoreCase))
            {
                var parts = timePreset.Substring(3).Split(new[] { "to" }, StringSplitOptions.None);
                if (parts.Length >= 2 && int.TryParse(parts[0], out var ageStart) && int.TryParse(parts[1], out var ageEnd))
                {
                    var startTime = birthTime.AddYears(ageStart);
                    var endTime = birthTime.AddYears(ageEnd);
                    return (startTime, endTime);
                }
            }

            // 3weeks, 3months, 3years
            if (timePreset.EndsWith("weeks", StringComparison.OrdinalIgnoreCase) && int.TryParse(timePreset.Substring(0, timePreset.Length - 5), out var weeks))
            {
                var start = birthTime;
                var endDto = birthTime.GetStdDateTimeOffset().AddDays(weeks * 7);
                var end = new Time(endDto, location);
                return (start, end);
            }
            if (timePreset.EndsWith("months", StringComparison.OrdinalIgnoreCase) && int.TryParse(timePreset.Substring(0, timePreset.Length - 6), out var months))
            {
                var start = birthTime;
                var endDto = birthTime.GetStdDateTimeOffset().AddMonths(months);
                var end = new Time(endDto, location);
                return (start, end);
            }
            if (timePreset.EndsWith("years", StringComparison.OrdinalIgnoreCase) && int.TryParse(timePreset.Substring(0, timePreset.Length - 5), out var years))
            {
                var start = birthTime;
                var end = birthTime.AddYears(years);
                return (start, end);
            }

            // fulllife
            if (timePreset.Equals("fulllife", StringComparison.OrdinalIgnoreCase))
                return (birthTime.AddYears(-1), birthTime.AddYears(100));

            // 19902000 - year range
            if (timePreset.Length >= 8 && int.TryParse(timePreset.Substring(0, 4), out var y1) && int.TryParse(timePreset.Substring(4, 4), out var y2))
            {
                var startDto = new DateTimeOffset(new DateTime(y1, 1, 1), outputTimezone);
                var endDto = new DateTimeOffset(new DateTime(y2, 12, 31, 23, 59, 59), outputTimezone);
                var start = new Time(startDto, location);
                var end = new Time(endDto, location);
                return (start, end);
            }

            return (birthTime.AddYears(-1), birthTime.AddYears(100));
        }

        /// <summary>
        /// Gets all possible calculations for a Planet at a given Time.
        /// Uses AutoCalculator to find and execute all methods that accept (PlanetName, Time).
        /// </summary>
        public static List<APIFunctionResult> AllPlanetData(PlanetName planetName, Time time)
        {
            var self = System.Reflection.MethodBase.GetCurrentMethod() as System.Reflection.MethodInfo;
            return AutoCalculator.FindAndExecuteFunctions(self, planetName, time);
        }

        /// <summary>
        /// Gets all possible calculations for a House at a given Time.
        /// Uses AutoCalculator to find and execute all methods that accept (HouseName, Time).
        /// </summary>
        public static List<APIFunctionResult> AllHouseData(HouseName houseName, Time time)
        {
            var self = System.Reflection.MethodBase.GetCurrentMethod() as System.Reflection.MethodInfo;
            return AutoCalculator.FindAndExecuteFunctions(self, houseName, time);
        }

        // House varga signs - delegate to Vargas
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetSignsBasedOnHouseLongitudes(Time time) => AllPlanetRasiSigns(time);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetRasiSigns(Time time) => AllPlanetVargaSigns(time, 1);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetHoraSign(Time time) => AllPlanetVargaSigns(time, 2);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetDrekkanaSign(Time time) => AllPlanetVargaSigns(time, 3);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetChaturthamsaSign(Time time) => AllPlanetVargaSigns(time, 4);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetSaptamshaSign(Time time) => AllPlanetVargaSigns(time, 7);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetNavamshaSign(Time time) => AllPlanetVargaSigns(time, 9);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetDashamamshaSign(Time time) => AllPlanetVargaSigns(time, 10);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetDwadashamshaSign(Time time) => AllPlanetVargaSigns(time, 12);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetShodashamshaSign(Time time) => AllPlanetVargaSigns(time, 16);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetVimshamshaSign(Time time) => AllPlanetVargaSigns(time, 20);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetChaturvimshamshaSign(Time time) => AllPlanetVargaSigns(time, 24);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetBhamshaSign(Time time) => AllPlanetVargaSigns(time, 27);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetTrimshamshaSign(Time time) => AllPlanetVargaSigns(time, 30);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetKhavedamshaSign(Time time) => AllPlanetVargaSigns(time, 40);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetAkshavedamshaSign(Time time) => AllPlanetVargaSigns(time, 45);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetShashtyamshaSign(Time time) => AllPlanetVargaSigns(time, 60);

        public static Dictionary<HouseName, ZodiacSign> AllHouseRasiSigns(Time time) => AllHouseVargaSigns(time, 1);
        public static Dictionary<HouseName, ZodiacSign> AllHouseHoraSign(Time time) => AllHouseVargaSigns(time, 2);
        public static Dictionary<HouseName, ZodiacSign> AllHouseDrekkanaSign(Time time) => AllHouseVargaSigns(time, 3);
        public static Dictionary<HouseName, ZodiacSign> AllHouseChaturthamsaSign(Time time) => AllHouseVargaSigns(time, 4);
        public static Dictionary<HouseName, ZodiacSign> AllHouseSaptamshaSign(Time time) => AllHouseVargaSigns(time, 7);
        public static Dictionary<HouseName, ZodiacSign> AllHouseNavamshaSign(Time time) => AllHouseVargaSigns(time, 9);
        public static Dictionary<HouseName, ZodiacSign> AllHouseDashamamshaSign(Time time) => AllHouseVargaSigns(time, 10);
        public static Dictionary<HouseName, ZodiacSign> AllHouseDwadashamshaSign(Time time) => AllHouseVargaSigns(time, 12);
        public static Dictionary<HouseName, ZodiacSign> AllHouseShodashamshaSign(Time time) => AllHouseVargaSigns(time, 16);
        public static Dictionary<HouseName, ZodiacSign> AllHouseVimshamshaSign(Time time) => AllHouseVargaSigns(time, 20);
        public static Dictionary<HouseName, ZodiacSign> AllHouseChaturvimshamshaSign(Time time) => AllHouseVargaSigns(time, 24);
        public static Dictionary<HouseName, ZodiacSign> AllHouseBhamshaSign(Time time) => AllHouseVargaSigns(time, 27);
        public static Dictionary<HouseName, ZodiacSign> AllHouseTrimshamshaSign(Time time) => AllHouseVargaSigns(time, 30);
        public static Dictionary<HouseName, ZodiacSign> AllHouseKhavedamshaSign(Time time) => AllHouseVargaSigns(time, 40);
        public static Dictionary<HouseName, ZodiacSign> AllHouseAkshavedamshaSign(Time time) => AllHouseVargaSigns(time, 45);
        public static Dictionary<HouseName, ZodiacSign> AllHouseShashtyamshaSign(Time time) => AllHouseVargaSigns(time, 60);

        public static ZodiacSign HouseZodiacSign(HouseName house, Time time) => ZodiacSignAtLongitude(AllHouseLongitudes(time).Find(h => h.GetHouseName() == house).GetMiddleLongitude());
        public static ZodiacSign HouseRasiSign(HouseName house, Time time) => HouseZodiacSign(house, time);
        public static ZodiacSign HouseHoraD2Sign(HouseName house, Time time) => HouseVargaSign(house, time, 2);
        public static ZodiacSign HouseDrekkanaD3Sign(HouseName house, Time time) => HouseVargaSign(house, time, 3);
        public static ZodiacSign HouseChaturthamshaD4Sign(HouseName house, Time time) => HouseVargaSign(house, time, 4);
        public static ZodiacSign HouseSaptamshaD7Sign(HouseName house, Time time) => HouseVargaSign(house, time, 7);
        public static ZodiacSign HouseNavamshaD9Sign(HouseName house, Time time) => HouseVargaSign(house, time, 9);
        public static ZodiacSign HouseDashamamshaD10Sign(HouseName house, Time time) => HouseVargaSign(house, time, 10);
        public static ZodiacSign HouseDwadashamshaD12Sign(HouseName house, Time time) => HouseVargaSign(house, time, 12);
        public static ZodiacSign HouseShodashamshaD16Sign(HouseName house, Time time) => HouseVargaSign(house, time, 16);
        public static ZodiacSign HouseVimshamshaD20Sign(HouseName house, Time time) => HouseVargaSign(house, time, 20);
        public static ZodiacSign HouseChaturvimshamshaD24Sign(HouseName house, Time time) => HouseVargaSign(house, time, 24);
        public static ZodiacSign HouseBhamshaD27Sign(HouseName house, Time time) => HouseVargaSign(house, time, 27);
        public static ZodiacSign HouseTrimshamshaD30Sign(HouseName house, Time time) => HouseVargaSign(house, time, 30);
        public static ZodiacSign HouseKhavedamshaD40Sign(HouseName house, Time time) => HouseVargaSign(house, time, 40);
        public static ZodiacSign HouseAkshavedamshaD45Sign(HouseName house, Time time) => HouseVargaSign(house, time, 45);
        public static ZodiacSign HouseShashtyamshaD60Sign(HouseName house, Time time) => HouseVargaSign(house, time, 60);

        private static Dictionary<PlanetName, ZodiacSign> AllPlanetVargaSigns(Time time, int division)
        {
            var result = new Dictionary<PlanetName, ZodiacSign>();
            var houseSigns = division == 1 ? AllHouseZodiacSigns(time) : AllHouseVargaSigns(time, division);
            foreach (var planet in PlanetName.All9Planets)
            {
                var house = HousePlanetOccupiesBasedOnSign(planet, time);
                result[planet] = houseSigns[house];
            }
            return result;
        }

        private static Dictionary<HouseName, ZodiacSign> AllHouseVargaSigns(Time time, int division)
        {
            var result = new Dictionary<HouseName, ZodiacSign>();
            var houseSigns = AllHouseZodiacSigns(time);
            foreach (var kv in houseSigns)
            {
                var vargaSign = Vargas.VargasCoreCalculator(kv.Value, GetVargaTable(division), division);
                result[kv.Key] = vargaSign;
            }
            return result;
        }

        private static Dictionary<ZodiacName, Dictionary<DegreeRange, ZodiacName>> GetVargaTable(int division) => division switch
        {
            2 => Vargas.HoraTable,
            3 => Vargas.DrekkanaTable,
            4 => Vargas.ChaturthamshaTable,
            7 => Vargas.SaptamshaTable,
            9 => Vargas.NavamshaTable,
            10 => Vargas.DashamamshaTable,
            12 => Vargas.DwadashamshaTable,
            16 => Vargas.ShodashamshaTable,
            20 => Vargas.VimshamshaTable,
            24 => Vargas.ChaturvimshamshaTable,
            27 => Vargas.BhamshaTable,
            30 => Vargas.KhavedamshaTable,
            45 => Vargas.AkshavedamshaTable,
            60 => Vargas.ShashtyamshaTable,
            _ => Vargas.HoraTable
        };

        private static ZodiacSign HouseVargaSign(HouseName house, Time time, int division)
        {
            var houseSigns = division == 1 ? AllHouseZodiacSigns(time) : AllHouseVargaSigns(time, division);
            return houseSigns[house];
        }

        public static ZodiacSign PlanetNavamshaD9Sign(PlanetName planet, Time time)
        {
            var rasi = PlanetRasiD1Sign(planet, time);
            return Vargas.VargasCoreCalculator(rasi, Vargas.NavamshaTable, 9);
        }

        // Shadbala and strength methods (stub implementations)
        public static List<PlanetName> AllPlanetOrderedByStrength(Time time) => new List<PlanetName>(PlanetName.All9Planets);
        public static List<HouseName> AllHousesOrderedByStrength(Time time) => new List<HouseName>(House.AllHouses);
        public static bool IsPlanetStrongInShadbala(PlanetName planet, Time time) => true;
        public static bool IsHouseBeneficInShadbala(HouseName house, Time time) => true;
        public static bool IsHouseBeneficInShadbala(HouseName house, Time time, int threshold) => true;
        public static PlanetName PickOutStrongestPlanet(Time time) => Sun;
        public static PlanetName PickOutStrongestPlanet(IEnumerable<PlanetName> planets, Time time) => planets?.FirstOrDefault() ?? Sun;
        public static double PlanetIshtaKashtaScoreDegree(PlanetName planet, Time time) => 0;
        public static double PlanetPowerPercentage(PlanetName planet, Time time) => 50;
        public static List<PlanetName> BeneficPlanetList(Time time) => new() { Moon, Mercury, Jupiter, Venus };
        public static List<PlanetName> MaleficPlanetList(Time time) => new() { Sun, Mars, Saturn };
        public static bool IsMercuryAfflicted(Time time) => false;
        public static PlanetToPlanetRelationship PlanetPermanentRelationshipWithPlanet(PlanetName p1, PlanetName p2) => PlanetToPlanetRelationship.Neutral;
        public static ConstellationAnimal YoniKutaAnimalFromConstellation(ConstellationName constellation) => new ConstellationAnimal("Male", AnimalName.Horse);
        public static int PlanetAshtakvargaBindu(PlanetName planet, ZodiacName signToCheck, Time time) =>
            Ashtakavarga.BhinnashtakavargaChartForPlanet(planet, time).GetValueOrDefault(signToCheck, 0);
        public static int PlanetAshtakvargaBindu(PlanetName planet, HouseName house, Time time) =>
            PlanetAshtakvargaBindu(planet, HouseSignName(house, time), time);

        public static Shashtiamsa HouseStrength(HouseName house, Time time) => new Shashtiamsa(100);
        public static Shashtiamsa PlanetStrength(PlanetName planet, Time time) => new Shashtiamsa(100);

        /// <summary>Check if transmitting planet aspects receiving planet (longitudinal aspect).</summary>
        public static bool IsPlanetAspectedByPlanet(PlanetName receiving, PlanetName transmitting, Time time)
        {
            var recLong = PlanetNirayanaLongitude(receiving, time);
            var txLong = PlanetNirayanaLongitude(transmitting, time);
            var signsBetween = (int)((txLong.Normalize360().TotalDegrees - recLong.Normalize360().TotalDegrees + 360) % 360 / 30);
            if (signsBetween < 0) signsBetween += 12;
            return GetAspectSignsForPlanet(transmitting).Contains(signsBetween);
        }

        /// <summary>Check if planet aspects house (by house cusp longitude).</summary>
        public static bool IsHouseAspectedByPlanet(HouseName house, PlanetName planet, Time time)
        {
            var houses = AllHouseLongitudes(time);
            var h = houses.Find(x => x.GetHouseName() == house);
            var houseLong = h.GetMiddleLongitude();
            var planetLong = PlanetNirayanaLongitude(planet, time);
            var signsBetween = (int)((planetLong.Normalize360().TotalDegrees - houseLong.Normalize360().TotalDegrees + 360) % 360 / 30);
            if (signsBetween < 0) signsBetween += 12;
            return GetAspectSignsForPlanet(planet).Contains(signsBetween);
        }

        /// <summary>Planets that aspect the given planet.</summary>
        public static List<PlanetName> PlanetsAspectingPlanet(PlanetName planet, Time time)
        {
            var result = new List<PlanetName>();
            foreach (var p in PlanetName.All9Planets)
            {
                if (p == planet) continue;
                if (IsPlanetAspectedByPlanet(planet, p, time)) result.Add(p);
            }
            return result;
        }

        /// <summary>Check if two planets are conjunct (within 8°).</summary>
        public static bool IsPlanetConjunctWithPlanet(PlanetName p1, PlanetName p2, Time time)
        {
            var long1 = PlanetNirayanaLongitude(p1, time);
            var long2 = PlanetNirayanaLongitude(p2, time);
            var dist = DistanceBetweenPlanets(long1, long2);
            return dist.TotalDegrees <= 8;
        }

        private static int[] GetAspectSignsForPlanet(PlanetName planet)
        {
            if (planet == Mars) return new[] { 4, 7, 8 };
            if (planet == Jupiter) return new[] { 5, 7, 9 };
            if (planet == Saturn) return new[] { 3, 7, 10 };
            return new[] { 7 };
        }

        /// <summary>Midpoint between two house cusps (junction point).</summary>
        public static Angle HouseJunctionPoint(Angle house1Middle, Angle house2Middle)
        {
            var mid = (house1Middle.TotalDegrees + house2Middle.TotalDegrees) / 2;
            return Angle.FromDegrees((mid % 360 + 360) % 360);
        }

        public static ZodiacSign PlanetExaltationPoint(PlanetName planet)
        {
            if (planet == Sun) return new ZodiacSign(ZodiacName.Aries, Angle.FromDegrees(10));
            if (planet == Moon) return new ZodiacSign(ZodiacName.Taurus, Angle.FromDegrees(3));
            if (planet == Mars) return new ZodiacSign(ZodiacName.Capricorn, Angle.FromDegrees(28));
            if (planet == Mercury) return new ZodiacSign(ZodiacName.Virgo, Angle.FromDegrees(15));
            if (planet == Jupiter) return new ZodiacSign(ZodiacName.Cancer, Angle.FromDegrees(5));
            if (planet == Venus) return new ZodiacSign(ZodiacName.Pisces, Angle.FromDegrees(27));
            if (planet == Saturn) return new ZodiacSign(ZodiacName.Libra, Angle.FromDegrees(20));
            if (planet == Rahu) return new ZodiacSign(ZodiacName.Taurus, Angle.FromDegrees(0));
            if (planet == Ketu) return new ZodiacSign(ZodiacName.Scorpio, Angle.FromDegrees(0));
            return new ZodiacSign(ZodiacName.Aries, Angle.FromDegrees(0));
        }

        public static ZodiacSign PlanetDebilitationPoint(PlanetName planet)
        {
            if (planet == Sun) return new ZodiacSign(ZodiacName.Libra, Angle.FromDegrees(10));
            if (planet == Moon) return new ZodiacSign(ZodiacName.Scorpio, Angle.FromDegrees(3));
            if (planet == Mars) return new ZodiacSign(ZodiacName.Cancer, Angle.FromDegrees(28));
            if (planet == Mercury) return new ZodiacSign(ZodiacName.Pisces, Angle.FromDegrees(15));
            if (planet == Jupiter) return new ZodiacSign(ZodiacName.Capricorn, Angle.FromDegrees(5));
            if (planet == Venus) return new ZodiacSign(ZodiacName.Virgo, Angle.FromDegrees(27));
            if (planet == Saturn) return new ZodiacSign(ZodiacName.Aries, Angle.FromDegrees(20));
            if (planet == Rahu) return new ZodiacSign(ZodiacName.Scorpio, Angle.FromDegrees(0));
            if (planet == Ketu) return new ZodiacSign(ZodiacName.Taurus, Angle.FromDegrees(0));
            return new ZodiacSign(ZodiacName.Libra, Angle.FromDegrees(0));
        }

        public static Shashtiamsa PlanetDrikBala(PlanetName planet, Time time) => new Shashtiamsa(100);
        public static Shashtiamsa PlanetSthanaBala(PlanetName planet, Time time) => new Shashtiamsa(100);
        public static LunarMonth LunarMonth(Time time)
        {
            var sunSign = PlanetRasiD1Sign(Sun, time).GetSignName();
            return sunSign switch
            {
                ZodiacName.Aries => global::VedAstro.Library.LunarMonth.Chaitra,
                ZodiacName.Taurus => global::VedAstro.Library.LunarMonth.Vaisaakha,
                ZodiacName.Gemini => global::VedAstro.Library.LunarMonth.Jyeshtha,
                ZodiacName.Cancer => global::VedAstro.Library.LunarMonth.Aashaadha,
                ZodiacName.Leo => global::VedAstro.Library.LunarMonth.Sraavana,
                ZodiacName.Virgo => global::VedAstro.Library.LunarMonth.Bhaadrapada,
                ZodiacName.Libra => global::VedAstro.Library.LunarMonth.Aaswayuja,
                ZodiacName.Scorpio => global::VedAstro.Library.LunarMonth.Kaarteeka,
                ZodiacName.Sagittarius => global::VedAstro.Library.LunarMonth.Maargasira,
                ZodiacName.Capricorn => global::VedAstro.Library.LunarMonth.Pushya,
                ZodiacName.Aquarius => global::VedAstro.Library.LunarMonth.Maagha,
                ZodiacName.Pisces => global::VedAstro.Library.LunarMonth.Phaalguna,
                _ => global::VedAstro.Library.LunarMonth.Chaitra
            };
        }
        public static bool IsFixedSign(ZodiacName sign) => sign is ZodiacName.Taurus or ZodiacName.Leo or ZodiacName.Scorpio or ZodiacName.Aquarius;
        public static bool IsMovableSign(ZodiacName sign) => sign is ZodiacName.Aries or ZodiacName.Cancer or ZodiacName.Libra or ZodiacName.Capricorn;
        public static bool IsCommonSign(ZodiacName sign) => sign is ZodiacName.Gemini or ZodiacName.Virgo or ZodiacName.Sagittarius or ZodiacName.Pisces;
        public static bool IsPlanetGocharaBindu(Time birthTime, Time currentTime, PlanetName planet, int bindu) => false;

        public static Bhinnashtakavarga BhinnashtakavargaChart(Time birthTime)
        {
            var result = new Bhinnashtakavarga();
            foreach (var planet in PlanetName.All7Planets)
            {
                result[planet] = Ashtakavarga.BhinnashtakavargaChartForPlanet(planet, birthTime);
            }
            return result;
        }

        /// <summary>
        /// Calculates dasa periods for a specific time range.
        /// </summary>
        /// <param name="birthTime">Birth time for dasa calculation.</param>
        /// <param name="startTime">Start of time range.</param>
        /// <param name="endTime">End of time range.</param>
        /// <param name="levels">1-7: Dasa/Bhukti/Antaram/Sukshma/Prana/Avi Prana/Viprana. Default 4.</param>
        /// <param name="precisionHours">Precision in hours (higher = faster but less accurate). Default 504 (~21 days).</param>
        public static List<DasaEvent> DasaAtRange(Time birthTime, Time startTime, Time endTime, int levels = 4, int precisionHours = 504)
        {
            var tagList = new List<EventTag>();
            if (levels >= 1) tagList.Add(EventTag.PD1);
            if (levels >= 2) tagList.Add(EventTag.PD2);
            if (levels >= 3) tagList.Add(EventTag.PD3);
            if (levels >= 4) tagList.Add(EventTag.PD4);
            if (levels >= 5) tagList.Add(EventTag.PD5);
            if (levels >= 6) tagList.Add(EventTag.PD6);
            if (levels >= 7) tagList.Add(EventTag.PD7);
            if (tagList.Count == 0) tagList.Add(EventTag.PD1);

            var person = new Person("", birthTime, Gender.Empty);
            var eventList = EventManager.CalculateEvents(precisionHours, startTime, endTime, person, tagList);
            return eventList.Select(e => new DasaEvent(e)).ToList();
        }

        // Chart generation
        public static string SkyChart(Time time) => SkyChartFactory.GenerateChart(time, 800, 600).GetAwaiter().GetResult();
        public static string SouthIndianChart(Time time, ChartType chartType) => new SouthChartFactory(time, chartType).SVGChart;
        public static string NorthIndianChart(Time time, ChartType chartType) => new NorthChartFactory(time, chartType).SVGChart;
        public static string GenerateTimeListCSV(Time startTime, Time endTime, double hoursBetween) => string.Join("\n", Time.GetTimeListFromRange(startTime, endTime, hoursBetween).Select(t => $"{t},{t.GetGeoLocation()}"));

        /// <summary>
        /// Check if planet is transiting (gochara) in the given house at current time.
        /// </summary>
        public static bool IsGocharaOccurring(Time birthTime, Time currentTime, PlanetName planet, int houseNumber)
        {
            var planetLong = PlanetNirayanaLongitude(planet, currentTime);
            var houses = AllHouseLongitudes(currentTime);
            var targetHouse = (HouseName)houseNumber;
            var house = houses.Find(h => h.GetHouseName() == targetHouse);
            return house.IsLongitudeInHouseRange(planetLong);
        }

        /// <summary>
        /// Check if planet is in the same house as the lord of the given house at birth time.
        /// </summary>
        public static bool IsPlanetSameHouseWithHouseLord(int houseNumber, PlanetName planet, Time birthTime)
        {
            var houseName = (HouseName)houseNumber;
            var planetHouse = HousePlanetOccupiesBasedOnSign(planet, birthTime);
            var houseLord = LordOfHouse(houseName, birthTime);
            var lordHouse = HousePlanetOccupiesBasedOnSign(houseLord, birthTime);
            return planetHouse == lordHouse;
        }

        /// <summary>
        /// Get the Yama (2h24m interval) for the given time. 5 yamas in day, 5 in night.
        /// </summary>
        public static BirthYama BirthYama(Time time)
        {
            var sunrise = SunriseTime(time);
            var sunset = SunsetTime(time);
            var current = time.GetStdDateTimeOffset();
            var sunriseDt = sunrise.GetStdDateTimeOffset();
            var sunsetDt = sunset.GetStdDateTimeOffset();
            const double yamaMinutes = 2 * 60 + 24; // 2h 24m
            if (current >= sunriseDt && current < sunsetDt)
            {
                var dayStart = new DateTimeOffset(sunriseDt.Year, sunriseDt.Month, sunriseDt.Day, sunriseDt.Hour, sunriseDt.Minute, sunriseDt.Second, sunriseDt.Offset);
                var elapsed = (current - dayStart).TotalMinutes;
                var yamaIndex = (int)(elapsed / yamaMinutes) + 1;
                if (yamaIndex > 5) yamaIndex = 5;
                var yamaStart = dayStart.AddMinutes((yamaIndex - 1) * yamaMinutes);
                var yamaEnd = dayStart.AddMinutes(yamaIndex * yamaMinutes);
                return new BirthYama(yamaIndex, new Time(yamaStart, time.GetGeoLocation()), new Time(yamaEnd, time.GetGeoLocation()));
            }
            else
            {
                var nightStart = current >= sunsetDt ? sunsetDt : sunsetDt.AddDays(-1);
                var elapsed = (current - nightStart).TotalMinutes;
                if (elapsed < 0) elapsed += 24 * 60;
                var yamaIndex = (int)(elapsed / yamaMinutes) + 1;
                if (yamaIndex > 5) yamaIndex = 5;
                var yamaStart = nightStart.AddMinutes((yamaIndex - 1) * yamaMinutes);
                var yamaEnd = nightStart.AddMinutes(yamaIndex * yamaMinutes);
                return new BirthYama(yamaIndex, new Time(yamaStart, time.GetGeoLocation()), new Time(yamaEnd, time.GetGeoLocation()));
            }
        }

        /// <summary>
        /// Convert longitude to divisional chart degrees within sign.
        /// Maps position within division slice to 0-30 in resulting varga sign.
        /// </summary>
        public static Angle DivisionalLongitude(double degreesInSign, int divisionNumber)
        {
            var totalDeg = degreesInSign % 30;
            if (totalDeg < 0) totalDeg += 30;
            var divSize = 30.0 / divisionNumber;
            var divDegrees = (totalDeg % divSize) * divisionNumber;
            return Angle.FromDegrees(divDegrees % 30);
        }
    }
}
