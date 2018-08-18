﻿using System;
using System.Threading.Tasks;
using Ombi.Api.Plex.Models;
using Ombi.Api.Plex.Models.Friends;
using Ombi.Api.Plex.Models.OAuth;
using Ombi.Api.Plex.Models.Server;
using Ombi.Api.Plex.Models.Status;

namespace Ombi.Api.Plex
{
    public interface IPlexApi
    {
        Task<PlexStatus> GetStatus(string authToken, string uri);
        Task<PlexAuthentication> SignIn(UserRequest user);
        Task<PlexServer> GetServer(string authToken);
        Task<PlexContainer> GetLibrarySections(string authToken, string plexFullHost);
        Task<PlexContainer> GetLibrary(string authToken, string plexFullHost, string libraryId);
        Task<PlexMetadata> GetEpisodeMetaData(string authToken, string host, int ratingKey);
        Task<PlexMetadata> GetMetadata(string authToken, string plexFullHost, int itemId);
        Task<PlexMetadata> GetSeasons(string authToken, string plexFullHost, int ratingKey);
        Task<PlexContainer> GetAllEpisodes(string authToken, string host, string section, int start, int retCount);
        Task<PlexFriends> GetUsers(string authToken);
        Task<PlexAccount> GetAccount(string authToken);
        Task<PlexMetadata> GetRecentlyAdded(string authToken, string uri, string sectionId);
        Task<OAuthPin> GetPin(int pinId);
        Task<Uri> GetOAuthUrl(int pinId, string code, string applicationUrl, bool wizard);
        Task<PlexAddWrapper> AddUser(string emailAddress, string serverId, string authToken, int[] libs);
    }
}