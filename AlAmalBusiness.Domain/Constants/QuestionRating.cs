namespace AlAmalBusiness.Domain.Constants
{
    // The five answers every questionnaire question offers. The numeric
    // values ARE the score (1 worst .. 5 best) — averages are taken straight
    // over the stored column, so don't renumber these. Over the wire they
    // travel as their names like every other enum ("VeryGood").
    public enum QuestionRating
    {
        VeryBad = 1,
        Bad = 2,
        Mid = 3,
        Good = 4,
        VeryGood = 5
    }
}
