#region Copyright
// /************************************************************************
//    Copyright (c) 2016 Jamie Rees
//    File: SearchModule.cs
//    Created By: Jamie Rees
//   
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
//   
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
//   
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ************************************************************************/
#endregion
using System;
using System.Collections.Generic;
using Nancy;
using Nancy.Responses.Negotiation;

using NLog;

using PlexRequests.Api;
using PlexRequests.Core;
using PlexRequests.Core.SettingModels;
using PlexRequests.Helpers;
using PlexRequests.Store;
using PlexRequests.UI.Jobs;
using PlexRequests.UI.Models;

namespace PlexRequests.UI.Modules
{
    public class SearchModule : BaseModule
    {
        public SearchModule(ICacheProvider cache, ISettingsService<CouchPotatoSettings> cpSettings,
            ISettingsService<PlexRequestSettings> prSettings, IAvailabilityChecker checker,
            IRequestService request) : base("search")
        {
            CpService = cpSettings;
            PrService = prSettings;
            MovieApi = new TheMovieDbApi();
            TvApi = new TheTvDbApi();
            Cache = cache;
            Checker = checker;
            RequestService = request;

            Get["/"] = parameters => RequestLoad();

            Get["movie/{searchTerm}"] = parameters => SearchMovie((string)parameters.searchTerm);
            Get["tv/{searchTerm}"] = parameters => SearchTvShow((string)parameters.searchTerm);

            Get["movie/upcoming"] = parameters => UpcomingMovies();
            Get["movie/playing"] = parameters => CurrentlyPlayingMovies();

            Post["request/movie"] = parameters => RequestMovie((int)Request.Form.movieId);
            Post["request/tv"] = parameters => RequestTvShow((int)Request.Form.tvId, (bool)Request.Form.latest);
        }
        private TheMovieDbApi MovieApi { get; }
        private TheTvDbApi TvApi { get; }
        private IRequestService RequestService { get; }
        private ICacheProvider Cache { get; }
        private ISettingsService<CouchPotatoSettings> CpService { get; }
        private ISettingsService<PlexRequestSettings> PrService { get; }
        private IAvailabilityChecker Checker { get; }
        private static Logger Log = LogManager.GetCurrentClassLogger();
        private string AuthToken => Cache.GetOrSet(CacheKeys.TvDbToken, TvApi.Authenticate, 50);

        private Negotiator RequestLoad()
        {
            var settings = PrService.GetSettings();

            Log.Trace("Loading Index");
            return View["Search/Index", settings];
        }

        private Response SearchMovie(string searchTerm)
        {
            Log.Trace("Searching for Movie {0}", searchTerm);
            var movies = MovieApi.SearchMovie(searchTerm);
            var result = movies.Result;
            return Response.AsJson(result);
        }

        private Response SearchTvShow(string searchTerm)
        {
            Log.Trace("Searching for TV Show {0}", searchTerm);
            var tvShow = TvApi.SearchTv(searchTerm, AuthToken);

            if (tvShow?.data == null)
            {
                return Response.AsJson("");
            }
            var model = new List<SearchTvShowViewModel>();

            foreach (var t in tvShow.data)
            {
                model.Add(new SearchTvShowViewModel
                {
                    Added = t.added,
                    AirsDayOfWeek = t.airsDayOfWeek,
                    AirsTime = t.airsTime,
                    Aliases = t.aliases,
                    // We are constructing the banner with the id: 
                    // http://thetvdb.com/banners/_cache/posters/ID-1.jpg
                    Banner = t.id.ToString(),
                    FirstAired = t.firstAired,
                    Genre = t.genre,
                    Id = t.id,
                    ImdbId = t.imdbId,
                    LastUpdated = t.lastUpdated,
                    Network = t.network,
                    NetworkId = t.networkId,
                    Overview = t.overview,
                    Rating = t.rating,
                    Runtime = t.runtime,
                    SeriesId = t.id,
                    SeriesName = t.seriesName,
                    SiteRating = t.siteRating,
                    Status = t.status,
                    Zap2ItId = t.zap2itId
                });
            }
            return Response.AsJson(model);
        }

        private Response UpcomingMovies()
        {
            var movies = MovieApi.GetUpcomingMovies();
            var result = movies.Result;
            return Response.AsJson(result);
        }

        private Response CurrentlyPlayingMovies()
        {
            var movies = MovieApi.GetCurrentPlayingMovies();
            var result = movies.Result;
            return Response.AsJson(result);
        }

        private Response RequestMovie(int movieId)
        {
            Log.Trace("Requesting movie with id {0}", movieId);
            if (RequestService.CheckRequest(movieId))
            {
                Log.Trace("movie with id {0} exists", movieId);
                return Response.AsJson(new { Result = false, Message = "Movie has already been requested!" });
            }
            Log.Trace("movie with id {0} doesnt exists", movieId);
            var cpSettings = CpService.GetSettings();
            if (cpSettings.ApiKey == null)
            {
                Log.Warn("CP apiKey is null");
                return Response.AsJson(new { Result = false, Message = "CouchPotato is not yet configured, If you are the Admin, please log in." });
            }
            Log.Trace("Settings: ");
            Log.Trace(cpSettings.DumpJson);

            var movieApi = new TheMovieDbApi();
            var movieInfo = movieApi.GetMovieInformation(movieId).Result;
            Log.Trace("Getting movie info from TheMovieDb");
            Log.Trace(movieInfo.DumpJson);

            var model = new RequestedModel
            {
                ProviderId = movieInfo.Id,
                Type = RequestType.Movie,
                Overview = movieInfo.Overview,
                ImdbId = movieInfo.ImdbId,
                PosterPath = "http://image.tmdb.org/t/p/w150/" + movieInfo.PosterPath,
                Title = movieInfo.Title,
                ReleaseDate = movieInfo.ReleaseDate ?? DateTime.MinValue,
                Status = movieInfo.Status,
                RequestedDate = DateTime.Now,
                Approved = false,
                RequestedBy = Session[SessionKeys.UsernameKey].ToString()
            };


            var settings = PrService.GetSettings();
            if (!settings.RequireApproval)
            {
                var cp = new CouchPotatoApi();
                Log.Trace("Adding movie to CP (No approval required)");
                var result = cp.AddMovie(model.ImdbId, cpSettings.ApiKey, model.Title, cpSettings.FullUri);
                Log.Trace("Adding movie to CP result {0}", result);
                if (result)
                {
                    model.Approved = true;
                    Log.Trace("Adding movie to database requests (No approval required)");
                    RequestService.AddRequest(movieId, model);

                    return Response.AsJson(new { Result = true });
                }
                return Response.AsJson(new { Result = false, Message = "Something went wrong adding the movie to CouchPotato! Please check your settings." });
            }

            try
            {
                Log.Trace("Adding movie to database requests");
                var id = RequestService.AddRequest(movieId, model);
                //BackgroundJob.Enqueue(() => Checker.CheckAndUpdate(model.Title, (int)id));

                return Response.AsJson(new { Result = true });
            }
            catch (Exception e)
            {
                Log.Fatal(e);

                return Response.AsJson(new { Result = false, Message = "Something went wrong adding the movie to CouchPotato! Please check your settings." });
            }
        }

        /// <summary>
        /// Requests the tv show.
        /// </summary>
        /// <param name="showId">The show identifier.</param>
        /// <param name="latest">if set to <c>true</c> [latest].</param>
        /// <returns></returns>
        private Response RequestTvShow(int showId, bool latest)
        {
            // Latest send to Sonarr and no need to store in DB
            if (RequestService.CheckRequest(showId))
            {
                return Response.AsJson(new { Result = false, Message = "TV Show has already been requested!" });
            }

            var tvApi = new TheTvDbApi();
            var token = GetAuthToken(tvApi);

            var showInfo = tvApi.GetInformation(showId, token).data;

            DateTime firstAir;
            DateTime.TryParse(showInfo.firstAired, out firstAir);

            var model = new RequestedModel
            {
                ProviderId = showInfo.id,
                Type = RequestType.TvShow,
                Overview = showInfo.overview,
                PosterPath = "http://image.tmdb.org/t/p/w150/" + showInfo.banner, // This is incorrect
                Title = showInfo.seriesName,
                ReleaseDate = firstAir,
                Status = showInfo.status,
                RequestedDate = DateTime.Now,
                Approved = false,
                RequestedBy = Session[SessionKeys.UsernameKey].ToString()
            };

            RequestService.AddRequest(showId, model);
            return Response.AsJson(new { Result = true });
        }
        private string GetAuthToken(TheTvDbApi api)
        {
            return Cache.GetOrSet(CacheKeys.TvDbToken, api.Authenticate, 50);
        }
    }
}