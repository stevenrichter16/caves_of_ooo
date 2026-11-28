using Microsoft.JSInterop;
using CavesOfOoo.Web;
public static class Bridge
{
    private static readonly BrowserGame Game = new();
    [JSInvokable] public static string Catalog() => Game.Catalog();
    [JSInvokable] public static string NewGame(string build,int seed,string name) => Game.NewGame(build,seed,name);
    [JSInvokable] public static string Dispatch(string json) => Game.Dispatch(json);
    [JSInvokable] public static byte[] Checkpoint() => Game.Save();
    [JSInvokable] public static string Restore(byte[] bytes) => Game.Load(bytes);
}
