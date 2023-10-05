// See https://aka.ms/new-console-template for more information
using System.Security.Cryptography;
using MockWebClient;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

var users = RandomNumberGenerator.GetInt32(1, 100);
new DriverManager().SetUpDriver(new ChromeConfig());
BrowseRepository.Default.DoBrowse(users);