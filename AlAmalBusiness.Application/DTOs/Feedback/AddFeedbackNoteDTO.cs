using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Feedback
{
    // A standalone timeline comment, posted from the box under the history —
    // no status change attached. (UpdateFeedbackStatusDTO's optional Note is
    // the other way a note reaches the timeline: as the reason for a change.)
    public class AddFeedbackNoteDTO
    {
        [Required(ErrorMessage = "Write something first.")]
        [StringLength(2000, MinimumLength = 1)]
        public string? Note { get; set; }
    }
}
