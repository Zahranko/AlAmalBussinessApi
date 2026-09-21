namespace AlAmalBusiness.Domain.Constants
{
    // What a question asks for. Stored as int — append, never reorder, the
    // same rule every other enum in this database follows.
    //
    // Rating is the original and stays 0, so every question written before
    // this existed reads back as one without a data migration.
    public enum QuestionType
    {
        // The five-point QuestionRating scale. Averaged, charted, trended.
        Rating = 0,
        // Free text the patient types. It has no score, so it is absent from
        // every average, distribution and trend in this feature — a text
        // question's "result" is the answers themselves, read one by one.
        Text = 1
    }
}
