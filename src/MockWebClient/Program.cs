// See https://aka.ms/new-console-template for more information
using System.Security.Cryptography;
using MockWebClient;

var users = RandomNumberGenerator.GetInt32(1, 100);

BrowseRepository.Default.DoBrowse(users);