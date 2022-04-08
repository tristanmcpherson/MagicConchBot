// See https://aka.ms/new-console-template for more information
using Xunit;
using MagicConchBot.Api;

Console.WriteLine("Hello, World!");

public class UnitTest
{
    [Fact]
    public void TestBandcampApi()
    {
        var testUrl = "https://foisey.bandcamp.com/track/layitdwn";

        var bandcampApi = new BandcampApi();
        bandcampApi.GetSongInfo(testUrl);
    }
}