﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Newtonsoft.Json;
using VLC_WINRT.Common;
using VLC_WINRT.Model;
using VLC_WINRT.ViewModels;
using Windows.Storage;
using Windows.Storage.Streams;
using VLC_WINRT.ViewModels.MainPage;
using XboxMusicLibrary;
using XboxMusicLibrary.Models;
using XboxMusicLibrary.Settings;
using Artist = XboxMusicLibrary.Models.Artist;

namespace VLC_WINRT.Utility.Helpers.MusicLibrary
{
    public static class ArtistInformationsHelper
    {
        public static async Task<Artist> GetArtistFromXboxMusic(string artist)
        {
            Artist XBoxArtistItem = new Artist();
            try
            {
                Locator.MusicLibraryVM.XBOXMusicToken = await Locator.MusicLibraryVM.XboxMusicHelper.GetAccessToken("5bf9b614-1651-4b49-98ee-1831ae58fb99", "copuMsVkCAFLQlP38bV3y+Azysz/crELZ5NdQU7+ddg=");
                Debug.WriteLine("Connecting to XBOX Music API for " + artist);
                Music XboxMusic;
                Debug.WriteLine("XBOX Music token " + Locator.MusicLibraryVM.XBOXMusicToken);
                XboxMusic = await Locator.MusicLibraryVM.XboxMusicHelper.SearchMediaCatalog(Locator.MusicLibraryVM.XBOXMusicToken, artist, null, 3);

                XBoxArtistItem = XboxMusic.Artists.Items.FirstOrDefault(x => x.Name == artist);
                if (XBoxArtistItem == null)
                    XBoxArtistItem = XboxMusic.Artists.Items.FirstOrDefault();

                Debug.WriteLine("XBOX Music artist found : " + XBoxArtistItem.Name);

                Locator.MusicLibraryVM.ImgCollection.Add(XBoxArtistItem.ImageUrl);
            }
            catch (Exception e)
            {
                Debug.WriteLine("XBOX Error\n" + e.ToString());
            }
            return XBoxArtistItem ?? null;
        }

        static async Task DownloadPicFromDeezerToLocalFolder(MusicLibraryViewModel.ArtistItemViewModel artist)
        {

            HttpClient clientPic = new HttpClient();
            string json = await clientPic.GetStringAsync("http://api.deezer.com/search/artist?q=" + artist.Name);
            if(json == "{\"data\":[],\"total\":0}")
            {
                return;
            }

            DeezerArtistItem.RootObject deezerData = JsonConvert.DeserializeObject<DeezerArtistItem.RootObject>(json);
            Debug.WriteLine("Deezer picture for " + artist.Name + " : " + deezerData.data[0].picture);
            HttpResponseMessage responsePic = await clientPic.GetAsync(deezerData.data[0].picture + "?size=450x450");

            byte[] img = await responsePic.Content.ReadAsByteArrayAsync();
            InMemoryRandomAccessStream streamWeb = new InMemoryRandomAccessStream();

            DataWriter writer = new DataWriter(streamWeb.GetOutputStreamAt(0));
            writer.WriteBytes(img);

            await writer.StoreAsync();

            StorageFolder artistPic = await ApplicationData.Current.LocalFolder.CreateFolderAsync("artistPic",
                CreationCollisionOption.OpenIfExists);
            string fileName = artist.Name + "_" + "dPi";

            var file = await artistPic.CreateFileAsync(fileName + ".jpg", CreationCollisionOption.OpenIfExists);
            var raStream = await file.OpenAsync(FileAccessMode.ReadWrite);

            using (var thumbnailStream = streamWeb.GetInputStreamAt(0))
            {
                using (var stream = raStream.GetOutputStreamAt(0))
                {
                    await RandomAccessStream.CopyAsync(thumbnailStream, stream);
                }
            }
            StorageFolder appDataFolder = ApplicationData.Current.LocalFolder;
            string supposedPictureUriLocal = appDataFolder.Path + "\\artistPic\\" + artist.Name + "_" + "dPi" + ".jpg";

            DispatchHelper.Invoke(() => artist.Picture = supposedPictureUriLocal);
        }

        public static async Task GetArtistPicture(MusicLibraryViewModel.ArtistItemViewModel artist)
        {
            StorageFolder appDataFolder = ApplicationData.Current.LocalFolder;
            string supposedPictureUriLocal = appDataFolder.Path + "\\artistPic\\" + artist.Name + "_" + "dPi" + ".jpg";
            if (NativeOperationsHelper.FileExist(supposedPictureUriLocal))
            {
                artist.Picture = supposedPictureUriLocal;
            }
            else
            {
                Task.Run(() =>
                {
                    DownloadPicFromDeezerToLocalFolder(artist);
                });
            }
        }

        public static async Task GetArtistTopAlbums(MusicLibraryViewModel.ArtistItemViewModel artist)
        {
            try
            {
                Debug.WriteLine("Getting TopAlbums from LastFM API");
                HttpClient lastFmClient = new HttpClient();
                var response =
                    await
                        lastFmClient.GetStringAsync(
                            string.Format("http://ws.audioscrobbler.com/2.0/?method=artist.gettopalbums&limit=8&format=json&api_key={0}&artist={1}", App.ApiKeyLastFm, artist.Name));
                var albums = JsonConvert.DeserializeObject<TopAlbumInformation>(response);
                Debug.WriteLine("Receive TopAlbums from LastFM API");
                if (albums != null)
                {
                    artist.OnlinePopularAlbumItems = albums.TopAlbums.Album;
                    artist.IsOnlinePopularAlbumItemsLoaded = true;
                }
            }
            catch
            {
                Debug.WriteLine("Error getting top albums from artist.");
            }
        }

        public static async Task GetArtistSimilarsArtist(MusicLibraryViewModel.ArtistItemViewModel artist)
        {
            try
            {
                HttpClient lastFmClient = new HttpClient();
                var response =
                    await
                        lastFmClient.GetStringAsync(
                            "http://ws.audioscrobbler.com/2.0/?method=artist.getsimilar&limit=8&api_key=" +
                            App.ApiKeyLastFm + "&artist=" + artist.Name);

                var xml = XDocument.Parse(response);
                var similarArtists = from results in xml.Descendants("artist")
                                     select new MusicLibraryViewModel.ArtistItemViewModel
                                     {
                                         Name = results.Element("name").Value.ToString(),
                                         Picture = results.Elements("image").ElementAt(3).Value,
                                     };
                if (similarArtists != null && similarArtists.Any())
                {
                    artist.OnlineRelatedArtists = similarArtists.ToList();
                    artist.IsOnlineRelatedArtistsLoaded = true;
                }
            }
            catch
            {

            }
        }

        public static async Task GetArtistBiography(MusicLibraryViewModel.ArtistItemViewModel artist)
        {
            string biography = string.Empty;
            try
            {
                var lastFmClient = new HttpClient();
                // Get users language/region
                // If their region is not support by LastFM, it won't return an artist biography.
                var region = new Windows.Globalization.GeographicRegion();
                string url =
                    string.Format(
 "http://ws.audioscrobbler.com/2.0/?method=artist.getinfo&artist={1}&api_key={0}&format=json&lang={2}",
                        App.ApiKeyLastFm, artist.Name, region.Code.ToLower());
                var reponse = await lastFmClient.GetStringAsync(url);
                {
                    var resourceLoader = new ResourceLoader();
                    var artistInfo = JsonConvert.DeserializeObject<ArtistInformation>(reponse);
                    if (artistInfo == null) return;
                    if (artistInfo.Artist == null) return;
                    var bioSummary = artistInfo.Artist.Bio.Summary;
                    if (bioSummary != null)
                    {
                        // Deleting the html tags
                        biography = Regex.Replace(bioSummary, "<.*?>", string.Empty);
                        // Remove leading new lines.
                        biography = biography.TrimStart('\r', '\n');
                        // Remove leading and ending white spaces.
                        biography = biography.Trim();
                        // TODO: Replace string "remove" with something better. It may not work on all artists and in all languages.
                        biography = !string.IsNullOrEmpty(biography) ? biography.Remove(biography.Length - "Read more about  on Last.fm".Length - artist.Name.Length - 6)
                            : resourceLoader.GetString("NoBiographyFound");
                    }
                    else
                    {
                        biography = resourceLoader.GetString("NoBiographyFound");
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Failed to get artist biography from LastFM. Returning nothing.");
            }
            artist.Biography = System.Net.WebUtility.HtmlDecode(biography);
        }
    }
}
// This code will be used later when XBOX Music will support all these queries (still no date of release though)

//{
//    foreach (var album in xBoxArtistItem.Albums.Items)
//    {
//        artist.OnlinePopularAlbumItems.Add(new VLC_WINRT.ViewModels.MainPage.MusicLibraryViewModel.OnlineAlbumItem()
//        {
//            Artist = xBoxArtistItem.Name,
//            Name = album.Name,
//            Picture = album.ImageUrlWithOptions(new ImageSettings(200, 200, ImageMode.Scale, "")),
//        });
//    }
//    foreach (var artists in xBoxArtistItem.RelatedArtists.Items)
//    {
//        var onlinePopularAlbums = artists.Albums.Items.Select(albums => new VLC_WINRT.ViewModels.MainPage.MusicLibraryViewModel.OnlineAlbumItem
//        {
//            Artist = artists.Name,
//            Name = albums.Name,
//            Picture = albums.ImageUrlWithOptions(new ImageSettings(280, 156, ImageMode.Scale, "")),
//        }).ToList();

//        var artistPic = artists.ImageUrl;
//        artist.OnlineRelatedArtists.Add(new VLC_WINRT.ViewModels.MainPage.MusicLibraryViewModel.ArtistItemViewModel
//        {
//            Name = artists.Name,
//            OnlinePopularAlbumItems = onlinePopularAlbums,
//            Picture = artistPic,
//        });
//    }

//}
