using Discord;
using Discord.Commands;
using SysBot.Pokemon.Helpers;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

// src: https://github.com/foxbot/patek/blob/master/src/Patek/Modules/InfoModule.cs
// ISC License (ISC)
// Copyright 2017, Christopher F. <foxbot@protonmail.com>
public class InfoModule : ModuleBase<SocketCommandContext>
{
    private const string detail = "I am an open-source Discord bot powered by PKHeX.Core and other open-source software.";

    private const ulong DisallowedUserId = 195756980873199618;

    private const string repo = " https://github.com/link2026";

    [Command("info")]
    [Alias("about", "whoami", "owner")]
    public async Task InfoAsync()
    {
        if (Context.User.Id == DisallowedUserId)
        {
            await ReplyAsync("We don't let shady people use this command.").ConfigureAwait(false);
            return;
        }
        var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

        var builder = new EmbedBuilder
        {
            Color = new Color(114, 137, 218),
            Description = detail,
        };

        builder.AddField("Info",
            $"- [Source Code]({repo})\n- [Join Our Discord!](https://discord.com/invite/WYbjRtwnmJ)\n" +
            $"- {Format.Bold("Owner")}: {app.Owner} ({app.Owner.Id})\n" +
            $"- {Format.Bold("Library")}: Discord.Net ({DiscordConfig.Version})\n" +
            $"- {Format.Bold("Uptime")}: {GetUptime()}\n" +
            $"- {Format.Bold("Runtime")}: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture} " +
            $"({RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture})\n" +
            $"- {Format.Bold("Buildtime")}: {GetVersionInfo("SysBot.Base", false)}\n" +
            $"- {Format.Bold("Mergebot Version")}: {TradeBot.Version}\n" +
            $"- {Format.Bold("Core Version")}: {GetVersionInfo("PKHeX.Core")}\n" +
            $"- {Format.Bold("AutoLegality Version")}: {GetVersionInfo("PKHeX.Core.AutoMod")}\n"
        );

        builder.AddField("Stats",
            $"- {Format.Bold("Heap Size")}: {GetHeapSize()}MiB\n" +
            $"- {Format.Bold("Guilds")}: {Context.Client.Guilds.Count}\n" +
            $"- {Format.Bold("Channels")}: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
            $"- {Format.Bold("Users")}: {Context.Client.Guilds.Sum(g => g.MemberCount)}\n"
        );

        await ReplyAsync("Here's a bit about me!", embed: builder.Build()).ConfigureAwait(false);
    }

    private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);

    private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

    private static string GetVersionInfo(string assemblyName, bool inclVersion = true)
    {
        const string _default = "Unknown";
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assembly = Array.Find(assemblies, x => x.GetName().Name == assemblyName);

        var attribute = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute is null)
            return _default;

        var info = attribute.InformationalVersion;
        var split = info.Split('+');
        if (split.Length >= 2)
        {
            var version = split[0];
            var revision = split[1];
            if (DateTime.TryParseExact(revision, "yyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var buildTime))
                return (inclVersion ? $"{version} " : "") + $@"{buildTime:yy-MM-dd\.hh\:mm}";
            return inclVersion ? version : _default;
        }
        return _default;
    }
}
