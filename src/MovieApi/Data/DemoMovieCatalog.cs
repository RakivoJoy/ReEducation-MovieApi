using MovieApi.Models;

namespace MovieApi.Data;

internal static class DemoMovieCatalog
{
    public static Movie[] Create()
    {
        var amyAdams = new Actor { Name = "Amy Adams", BirthYear = 1974 };
        var arrival = new Movie
        {
            Title = "Arrival",
            Year = 2016,
            Genre = "Science fiction",
            Duration = 116,
            Details = new MovieDetails
            {
                Synopsis = "A linguist tries to communicate with visitors who have arrived on Earth.",
                Language = "English",
                Budget = 47_000_000m
            },
            Reviews =
            {
                new Review { ReviewerName = "Alex Morgan", Comment = "A thoughtful story about language and time.", Rating = 5 },
                new Review { ReviewerName = "Sam Rivera", Comment = "The slow build makes the ending land.", Rating = 4 }
            },
            Actors = { amyAdams, new Actor { Name = "Jeremy Renner", BirthYear = 1971 } }
        };
        var enchanted = new Movie
        {
            Title = "Enchanted",
            Year = 2007,
            Genre = "Fantasy",
            Duration = 107,
            Details = new MovieDetails
            {
                Synopsis = "A fairy-tale heroine is transported from her animated kingdom to New York City.",
                Language = "English",
                Budget = 85_000_000m
            },
            Reviews =
            {
                new Review { ReviewerName = "Jordan Lee", Comment = "The musical numbers are the highlight.", Rating = 4 }
            },
            Actors = { amyAdams, new Actor { Name = "Patrick Dempsey", BirthYear = 1966 } }
        };

        return [arrival, enchanted];
    }
}
