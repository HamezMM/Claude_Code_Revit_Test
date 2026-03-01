// PDG GENERATED: 2026-03-01 | Revit 2024

using System.Collections.Generic;

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Canonical fire rating names and mappings used by all PDG Fire Safety tools.
    /// Wall types must have their "Fire Rating" type parameter set to one of the
    /// <see cref="StandardRatings"/> strings for the tools to recognise them.
    /// </summary>
    public static class FireRatingStandards
    {
        /// <summary>
        /// Ordered list of standard wall fire rating display names.
        /// The Fire Rating Lines tool ensures a line style with each of these names
        /// exists in the document before drawing, creating any that are missing.
        /// </summary>
        public static readonly string[] StandardRatings = new[]
        {
            "45 MIN",
            "1 HR",
            "1.5 HR",
            "2 HR",
            "3 HR",
            "4 HR"
        };

        /// <summary>
        /// Standard door fire rating names, parallel to <see cref="StandardRatings"/>.
        /// <c>DoorRatings[i]</c> is the required door rating when the host wall has
        /// <c>StandardRatings[i]</c>. Use <see cref="WallToDoorRating"/> for lookups.
        /// </summary>
        public static readonly string[] DoorRatings = new[]
        {
            "20 MIN",   // 45 MIN wall
            "45 MIN",   // 1 HR wall
            "1 HR",     // 1.5 HR wall
            "1.5 HR",   // 2 HR wall
            "2 HR",     // 3 HR wall
            "3 HR"      // 4 HR wall
        };

        /// <summary>
        /// Maps a wall fire rating string to the required door fire rating string.
        /// Lookup is case-insensitive and trimmed. Keys are the standard wall ratings;
        /// values are the standard door ratings.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> WallToDoorRating =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "45 MIN", "20 MIN"  },
                { "1 HR",   "45 MIN"  },
                { "1.5 HR", "1 HR"    },
                { "2 HR",   "1.5 HR"  },
                { "3 HR",   "2 HR"    },
                { "4 HR",   "3 HR"    }
            };
    }
}
