using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class LegalizerModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        [Command("convert"), Alias("showdown")]
        [Summary("Tries to convert the Showdown Set to RegenTemplate format.")]
        [Priority(1)]
        public Task ConvertShowdown([Summary("Generation/Format")] byte gen, [Remainder][Summary("Showdown Set")] string content)
        {
            return Task.Run(() => Context.Channel.ReplyWithLegalizedSetAsync(content, gen));
        }

        [Command("convert"), Alias("showdown")]
        [Summary("Tries to convert the Showdown Set to RegenTemplate format.")]
        [Priority(0)]
        public Task ConvertShowdown([Remainder][Summary("Showdown Set")] string content)
        {
            return Task.Run(() => Context.Channel.ReplyWithLegalizedSetAsync<T>(content));
        }

        [Command("legalize"), Alias("alm")]
        [Summary("Tries to legalize the attached pkm data and output as RegenTemplate.")]
        public async Task LegalizeAsync()
        {
            foreach (var att in Context.Message.Attachments)
            {
                await Task.Run(() => Context.Channel.ReplyWithLegalizedSetAsync(att)).ConfigureAwait(false);
            }
        }
    }
}
