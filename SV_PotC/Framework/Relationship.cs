namespace SpaceBaby.PartOfTheCommunity.Framework
{
    /// <summary>A relationship type exposed through the Part of the Community API.</summary>
    public enum Relationship
    {
        /****
        ** Siblings
        ****/
        /// <summary>A brother.</summary>
        Brother,

        /// <summary>A sister.</summary>
        Sister,

        /// <summary>An adoptive brother.</summary>
        HalfBrother,

        /// <summary>An adoptive sister.</summary>
        HalfSister,

        /****
        ** Descendents
        ****/
        /// <summary>A son.</summary>
        Son,

        /// <summary>A daughter.</summary>
        Daughter,

        /// <summary>A son of one's spouse from a previous relationship.</summary>
        StepSon,

        /// <summary>A daughter of one's spouse from a previous relationship.</summary>
        StepDaughter,

        /// <summary>A grandson.</summary>
        Grandson,

        /// <summary>A grandson.</summary>
        Granddaughter,

        /// <summary>A great-grandson.</summary>
        GreatGrandson,

        /// <summary>A great-granddaughter.</summary>
        GreatGranddaughter,

        /****
        ** Ancestors
        ****/
        /// <summary>A father.</summary>
        Father,

        /// <summary>A mother.</summary>
        Mother,

        /// <summary>A male step-parent (a parent's spouse who is not a biological father).</summary>
        StepFather,

        /// <summary>A female step-parent (a parent's spouse who is not a biological mother).</summary>
        StepMother,

        /// <summary>A grandfather.</summary>
        Grandfather,

        /// <summary>A grandmother.</summary>
        Grandmother,

        /// <summary>A great-grandfather.</summary>
        GreatGrandfather,

        /// <summary>A great-grandmother.</summary>
        GreatGrandmother,


        /****
        ** Other family
        ****/
        /// <summary>A male spouse.</summary>
        Husband,

        /// <summary>A female spouse.</summary>
        Wife,

        /// <summary>A father-in-law.</summary>
        FatherInLaw,

        /// <summary>A mother-in-law.</summary>
        MotherInLaw,

        /// <summary>A brother-in-law.</summary>
        BrotherInLaw,

        /// <summary>A sister-in-law.</summary>
        SisterInLaw,

        /// <summary>A son-in-law.</summary>
        SonInLaw,

        /// <summary>A daughter-in-law.</summary>
        DaughterInLaw,

        /// <summary>An aunt.</summary>
        Aunt,

        /// <summary>An uncle.</summary>
        Uncle,

        /// <summary>A niece.</summary>
        Niece,

        /// <summary>A nephew.</summary>
        Nephew,

        /// <summary>A godfather.</summary>
        Godfather,

        /// <summary>A godmother.</summary>
        Godmother,

        /// <summary>A godson.</summary>
        Godson,

        /// <summary>A goddaughter.</summary>
        Goddaughter,

        /// <summary>A cousin.</summary>
        Cousin,

        /****
        ** Non-family
        ****/
        /// <summary>A non-family friend.</summary>
        Friend,

        /// <summary>An acquaintance separated by the gulf of a past war.</summary>
        WarTorn
    }

    /// <summary>Helpers for working with relationship values.</summary>
    public static class RelationshipExtensions
    {
        /// <summary>Get the inverse relationship from the target character back to the source character.</summary>
        /// <param name="relationship">The relationship from source to target.</param>
        /// <param name="sourceIsMale">Whether the source character is male.</param>
        /// <returns>The inverse relationship from target to source.</returns>
        public static Relationship GetInverse(this Relationship relationship, bool sourceIsMale)
        {
            return relationship switch
            {
                Relationship.Brother or Relationship.Sister => sourceIsMale ? Relationship.Brother : Relationship.Sister,
                Relationship.HalfBrother or Relationship.HalfSister => sourceIsMale ? Relationship.HalfBrother : Relationship.HalfSister,
                Relationship.Son or Relationship.Daughter => sourceIsMale ? Relationship.Father : Relationship.Mother,
                Relationship.StepSon or Relationship.StepDaughter => sourceIsMale ? Relationship.StepFather : Relationship.StepMother,
                Relationship.Father or Relationship.Mother => sourceIsMale ? Relationship.Son : Relationship.Daughter,
                Relationship.StepFather or Relationship.StepMother => sourceIsMale ? Relationship.StepSon : Relationship.StepDaughter,
                Relationship.Grandfather or Relationship.Grandmother => sourceIsMale ? Relationship.Grandson : Relationship.Granddaughter,
                Relationship.GreatGrandfather or Relationship.GreatGrandmother => sourceIsMale ? Relationship.GreatGrandson : Relationship.GreatGranddaughter,
                Relationship.Grandson or Relationship.Granddaughter => sourceIsMale ? Relationship.Grandfather : Relationship.Grandmother,
                Relationship.GreatGrandson or Relationship.GreatGranddaughter => sourceIsMale ? Relationship.GreatGrandfather : Relationship.GreatGrandmother,
                Relationship.Husband or Relationship.Wife => sourceIsMale ? Relationship.Husband : Relationship.Wife,
                Relationship.FatherInLaw or Relationship.MotherInLaw => sourceIsMale ? Relationship.SonInLaw : Relationship.DaughterInLaw,
                Relationship.BrotherInLaw or Relationship.SisterInLaw => sourceIsMale ? Relationship.BrotherInLaw : Relationship.SisterInLaw,
                Relationship.SonInLaw or Relationship.DaughterInLaw => sourceIsMale ? Relationship.FatherInLaw : Relationship.MotherInLaw,
                Relationship.Aunt or Relationship.Uncle => sourceIsMale ? Relationship.Nephew : Relationship.Niece,
                Relationship.Nephew or Relationship.Niece => sourceIsMale ? Relationship.Uncle : Relationship.Aunt,
                Relationship.Godfather or Relationship.Godmother => sourceIsMale ? Relationship.Godson : Relationship.Goddaughter,
                Relationship.Godson or Relationship.Goddaughter => sourceIsMale ? Relationship.Godfather : Relationship.Godmother,
                Relationship.Cousin => Relationship.Cousin,
                Relationship.Friend => Relationship.Friend,
                Relationship.WarTorn => Relationship.WarTorn,
                _ => relationship
            };
        }

        /// <summary>Get the relationship pair created for the player when they marry into a character's existing family.</summary>
        /// <param name="spouseRelation">The spouse's relationship to their relative.</param>
        /// <param name="playerIsMale">Whether the player is male.</param>
        /// <param name="playerToRelative">The relationship from the player to the spouse's relative.</param>
        /// <param name="relativeToPlayer">The relationship from the spouse's relative back to the player.</param>
        /// <returns>Returns whether the spouse relationship produces a marriage-derived relationship pair.</returns>
        public static bool TryGetMarriageDerivedRelationship(this Relationship spouseRelation, bool playerIsMale, out Relationship playerToRelative, out Relationship relativeToPlayer)
        {
            switch (spouseRelation)
            {
                case Relationship.Father:
                case Relationship.StepFather:
                    playerToRelative = Relationship.FatherInLaw;
                    relativeToPlayer = playerIsMale ? Relationship.SonInLaw : Relationship.DaughterInLaw;
                    return true;

                case Relationship.Mother:
                case Relationship.StepMother:
                    playerToRelative = Relationship.MotherInLaw;
                    relativeToPlayer = playerIsMale ? Relationship.SonInLaw : Relationship.DaughterInLaw;
                    return true;

                case Relationship.Brother:
                case Relationship.HalfBrother:
                    playerToRelative = Relationship.BrotherInLaw;
                    relativeToPlayer = playerIsMale ? Relationship.BrotherInLaw : Relationship.SisterInLaw;
                    return true;

                case Relationship.Sister:
                case Relationship.HalfSister:
                    playerToRelative = Relationship.SisterInLaw;
                    relativeToPlayer = playerIsMale ? Relationship.BrotherInLaw : Relationship.SisterInLaw;
                    return true;

                case Relationship.Son:
                case Relationship.StepSon:
                    playerToRelative = Relationship.StepSon;
                    relativeToPlayer = playerIsMale ? Relationship.StepFather : Relationship.StepMother;
                    return true;

                case Relationship.Daughter:
                case Relationship.StepDaughter:
                    playerToRelative = Relationship.StepDaughter;
                    relativeToPlayer = playerIsMale ? Relationship.StepFather : Relationship.StepMother;
                    return true;

                default:
                    playerToRelative = default;
                    relativeToPlayer = default;
                    return false;
            }
        }
    }
}
