using ArloVsMocks.Data;

namespace ArloVsMocks;

class Program
{
    static void Main(string[] args)
    {
        var arguments = ParseInput(args);

        var db = new MovieReviewEntities();
        try
        {
            CreateOrUpdateNewRating(db, arguments);
            UpdateCriticRating(db);
            RecalculateWeights(db);

            db.SaveChanges();

            PrintSummary(GetSummary(db, arguments));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            db.Dispose();
        }

        Console.ReadKey();
    }

    private static void CreateOrUpdateNewRating(MovieReviewEntities db, CliArguments arguments)
    {
        var existingRating =
            db.Ratings.SingleOrDefault(r => r.MovieId == arguments.MovieId && r.CriticId == arguments.CriticId);
        if (existingRating == null)
        {
            existingRating = new Rating { MovieId = arguments.MovieId, CriticId = arguments.CriticId };
            db.Ratings.Add(existingRating);
        }

        existingRating.Stars = arguments.Stars;
    }

    private static void UpdateCriticRating(MovieReviewEntities db)
    {
        var criticsHavingRated = db.Critics.Where(c => c.Ratings.Count > 0);
        foreach (var critic in criticsHavingRated)
        {
            var ratingsWithAverages = critic.Ratings.Where(r => r.Movie.AverageRating.HasValue).ToList();
            var totalDisparity = ratingsWithAverages.Sum(r => Math.Abs(r.Stars - r.Movie.AverageRating!.Value));
            var relativeDisparity = totalDisparity / ratingsWithAverages.Count;

            critic.RatingWeight = CalculateRatingWeightByDisparity(relativeDisparity);
        }

        double CalculateRatingWeightByDisparity(double disparity)
        {
            return disparity switch
            {
                > 2 => 0.15,
                > 1 => 0.33,
                _ => 1.0
            };
        }
    }

    private static void RecalculateWeights(MovieReviewEntities db)
    {
        foreach (var movie in db.Movies)
        {
            var weightTotal = movie.Ratings.Select(r => r.Critic.RatingWeight).Sum();
            var ratingTotal = movie.Ratings.Select(r => r.Stars * r.Critic.RatingWeight).Sum();

            movie.AverageRating = ratingTotal / weightTotal;
        }
    }

    private static Summary GetSummary(MovieReviewEntities db, CliArguments arguments)
    {
        var newCriticRatingWeight = db.Critics.Single(c => c.Id == arguments.CriticId).RatingWeight;
        var newMovieRating = db.Movies.Single(m => m.Id == arguments.MovieId).AverageRating!.Value;

        return new Summary(newCriticRatingWeight, newMovieRating);
    }

    private static void PrintSummary(Summary summary)
    {
        Console.WriteLine("New critic rating weight: {0:N1}", summary.NewCriticRatingWeight);
        Console.WriteLine("New movie rating: {0:N1}", summary.NewMovieRating);
    }

    private static CliArguments ParseInput(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine(
                $"Must be 3 int arguments: {nameof(CliArguments.MovieId)}, {nameof(CliArguments.CriticId)}, {nameof(CliArguments.Stars)}");
        }

        try
        {
            return new CliArguments(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
}