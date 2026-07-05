using System.Collections.Generic;

/// <summary>
/// Role blurbs for each class specialty shown in the decks collection.
/// </summary>
public static class ClassSpecialtyDescriptions
{
    static readonly Dictionary<string, string> RolesByKey = new Dictionary<string, string>
    {
        {
            "infantry",
            "The adaptable frontline soldier. Infantry cards focus on conventional gunplay, positioning, and versatility. They have the fewest gimmicks but are dependable in nearly every situation."
        },
        {
            "sniper",
            "Long-range specialists that control the battlefield through precision, information, and siege support rather than pure kills."
        },
        {
            "engineer",
            "Masters of construction and battlefield manipulation. Engineers shape the map through traps, transportation, and advanced structures."
        },
        {
            "support",
            "Keeps the team operating at peak efficiency through healing, logistics, and leadership rather than direct combat."
        },
        {
            "assault",
            "Objective breakers. Assault cards specialize in pushing fortified positions, crowd control, and aggressive close-range engagements."
        },
        {
            "heavy",
            "Walking tanks that trade mobility for overwhelming durability and sustained firepower."
        },
        {
            "assassin",
            "Eliminate key targets through stealth, speed, and precision. Their goal is creating openings rather than winning prolonged firefights."
        },
        {
            "demolition",
            "Specialists in destroying enemy structures and fortifications with explosives and siege weapons."
        },
        {
            "saboteur",
            "Objective infiltrators. They specialize in disabling enemy defenses, avoiding detection, and stealing victory through drill sabotage."
        },
        {
            "gunner",
            "Suppression experts that deny space through overwhelming sustained fire and make it difficult for enemies to advance."
        }
    };

    public static string GetRole(string specialtyKey)
    {
        if (string.IsNullOrEmpty(specialtyKey))
        {
            return string.Empty;
        }

        return RolesByKey.TryGetValue(specialtyKey, out var role) ? role : string.Empty;
    }
}
