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

        /// <summary>A father of one's spouse (a step-father).</summary>
        StepFather,

        /// <summary>A mother of one's spouse (a step-mother).</summary>
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
    }
}
