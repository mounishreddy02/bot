using System.Collections.Generic;

namespace FinalBotcode
{
    public class UserProfile
    {
        public string Name { get; set; }

        public int? Age { get; set; }

        public string Date { get; set; }


        // The list of companies the user wants to review.
        public List<string> CompaniesToReview { get; set; } = new List<string>();
    }
}
