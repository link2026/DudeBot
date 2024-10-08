using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class AutoLegalityExtensionsDiscord
{
    public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, ITrainerInfo sav, ShowdownSet set)
    {
        if (set.Species <= 0)
        {
            await channel.SendMessageAsync("Oops! I wasn't able to interpret your message! If you intended to convert something, please double check what you're pasting!").ConfigureAwait(false);
            return;
        }

        try
        {
            // Offload CPU-bound work to a background thread
            var result = await Task.Run(() =>
            {
                var template = AutoLegalityWrapper.GetTemplate(set);
                var pkm = sav.GetLegal(template, out var resultText);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                return (template, pkm, resultText, la, spec);
            }).ConfigureAwait(false);

            var (template, pkm, resultText, la, spec) = result;

            if (!la.Valid)
            {
                var reason = resultText switch
                {
                    "Timeout" => $"That {spec} set took too long to generate.",
                    "VersionMismatch" => "Request refused: PKHeX and Auto-Legality Mod version mismatch.",
                    _ => $"I wasn't able to create a {spec} from that set."
                };
                var imsg = $"Oops! {reason}";
                if (resultText == "Failed")
                    imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                await channel.SendMessageAsync(imsg).ConfigureAwait(false);
                return;
            }

            // Create RegenTemplate from the legalized PKM
            var regenTemplate = new RegenTemplate(pkm);
            var regenText = regenTemplate.Text;

            // Get form name using FormConverter
            var formNames = FormConverter.GetFormList(pkm.Species, GameInfo.Strings.Types, GameInfo.Strings.forms, new List<string>(), pkm.Context);
            var formName = pkm.Form > 0 && pkm.Form < formNames.Length ? formNames[pkm.Form] : "";

            // Create species and form string
            var speciesForm = !string.IsNullOrEmpty(formName) ? $"{spec}-{formName}" : spec;
            var speciesInfo = $"{speciesForm}\n";

            // Prepend species information to the RegenTemplate
            regenText = speciesInfo + regenText;

            // Create embed
            var embed = new EmbedBuilder()
                .WithTitle($"Legalized RegenTemplate for {speciesForm}")
                .WithDescription($"Result: {resultText}\nEncounter: {la.EncounterOriginal.Name}")
                .AddField("RegenTemplate", $"```{regenText}```")
                .WithColor(Color.Green)
                .WithFooter("Copy the RegenTemplate text between the ``` marks to use it.");

            await channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(AutoLegalityExtensionsDiscord));
            var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
            await channel.SendMessageAsync(msg).ConfigureAwait(false);
        }
    }

    public static Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, string content, byte gen)
    {
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var sav = AutoLegalityWrapper.GetTrainerInfo(gen);
        return channel.ReplyWithLegalizedSetAsync(sav, set);
    }

    public static Task ReplyWithLegalizedSetAsync<T>(this ISocketMessageChannel channel, string content) where T : PKM, new()
    {
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        return channel.ReplyWithLegalizedSetAsync(sav, set);
    }

    public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, IAttachment att)
    {
        var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
        if (!download.Success)
        {
            await channel.SendMessageAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        // Offload CPU-bound work to a background thread
        var legalizationResult = await Task.Run(() =>
        {
            var pkm = download.Data!;
            var la = new LegalityAnalysis(pkm);
            if (la.Valid)
            {
                return (pkm, true, null);
            }
            else
            {
                var legal = pkm.LegalizePokemon();
                var laLegal = new LegalityAnalysis(legal);
                if (laLegal.Valid)
                {
                    legal.RefreshChecksum();
                    return (legal, true, null);
                }
                else
                {
                    return (pkm, false, "Unable to legalize.");
                }
            }
        }).ConfigureAwait(false);

        var (finalPkm, success, errorMessage) = legalizationResult;

        var embed = new EmbedBuilder()
            .WithTitle($"Legalization Report for {download.SanitizedFileName}")
            .WithDescription($"{download.SanitizedFileName} analysis and legalization attempt.")
            .WithColor(success ? Color.Green : Color.Red);

        if (success)
        {
            var msg = $"Here's your legalized PKM for {download.SanitizedFileName}!\n{ReusableActions.GetFormattedShowdownText(finalPkm)}";
            embed.AddField("Status", "Successfully legalized.");
            embed.AddField("Details", msg);
            await channel.SendPKMAsync(finalPkm).ConfigureAwait(false);
        }
        else
        {
            embed.AddField("Status", errorMessage);
        }

        await channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
}
