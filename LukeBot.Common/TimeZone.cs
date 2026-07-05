using System;

namespace LukeBot.Common
{
    public enum AlternativeKind
    {
        NONE = 0, // for unique abbrevs
        AMBIGUOUS, // for abbrevs which clash ex. CST -> US Central Standard Time, China Standard Time, Cuba Standard Time
        DST_ALT, // for abbrevs which have a "Summer" or "Daylight Savings" alternative ex. CET -> CEST.
        DST_ALIAS // for abbrevs which alias into a different zone completely depending on DST ex. ET -> EST or EDT. We assume first alt is standard and second is DST.
    }

    public record TimeZoneShort(string Abbreviation, string FullName, TimeSpan UTC, AlternativeKind AlternativeKind, string[] Alternatives);
}
