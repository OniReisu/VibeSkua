using CommunityToolkit.Mvvm.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.Services;

/// <summary>
/// Keeps the display name / guild chosen in the CoreBots "Options" tab applied to the game client.
/// The values live in options\CBO_Storage(user).txt, outside the Scripts folder, so script updates (or a script
/// resetting the name when it stops, as CoreBots does) can't change them for longer than one tick.
/// Does nothing unless "Use Custom Identity" is ticked, and never fights Streamer Mode.
/// </summary>
internal static class IdentityKeeper
{
    private const string UseKey = "UseCustomIdentity";
    private const string NameKey = "CustomIdentityName";
    private const string GuildKey = "CustomIdentityGuild";

    private static CancellationTokenSource? _cts;

    public static void Start()
    {
        if (_cts is not null)
            return;
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    Apply();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Never let the keeper take the client down; try again next tick.
                }
            }
        }, token);
    }

    private static void Apply()
    {
        IScriptPlayer player = Ioc.Default.GetRequiredService<IScriptPlayer>();
        if (!player.LoggedIn || !player.Playing || !player.Loaded || string.IsNullOrEmpty(player.Username))
            return;

        IScriptOption options = Ioc.Default.GetRequiredService<IScriptOption>();
        if (options.StreamerMode)
            return;

        string path = Path.Combine(ClientFileSources.SkuaOptionsDIR, $"CBO_Storage({player.Username}).txt");
        if (!File.Exists(path))
            return;

        string? name = null, guild = null;
        bool enabled = false;
        foreach (string line in File.ReadAllLines(path))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0)
                continue;
            string key = line[..colon].Trim();
            string value = line[(colon + 1)..].Trim();
            if (key == UseKey)
                enabled = bool.TryParse(value, out bool b) && b;
            else if (key == NameKey)
                name = value;
            else if (key == GuildKey)
                guild = value;
        }

        if (!enabled)
            return;
        if (!string.IsNullOrWhiteSpace(name))
            options.CustomName = name;
        if (!string.IsNullOrWhiteSpace(guild))
            options.CustomGuild = guild;
    }
}
