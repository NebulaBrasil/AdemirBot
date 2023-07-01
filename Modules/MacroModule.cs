﻿using Discord;
using Discord.Interactions;
using DiscordBot.Domain.Entities;
using DiscordBot.Modules.Modals;
using System.Text.RegularExpressions;

namespace DiscordBot.Modules
{
    public class MacroModule : InteractionModuleBase
    {        private readonly Context db;

        public MacroModule(Context context)
        {
            db = context;
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("macro", "Adiciona uma macro de prefixo '%'")]
        public async Task Macro()
            => await Context.Interaction.RespondWithModalAsync<MacroModal>("macro");

        [ModalInteraction("macro")]
        public async Task MacroResponse(MacroModal modal)
        {
            var macro = await db.macros.Count(a => a.GuildId == Context.Guild.Id && a.Nome == modal.Nome);

            if (macro > 0)
            {
                await RespondAsync($"Já existe uma macro com o nome {modal.Nome} no server.", ephemeral: true);
            }

            await db.macros.AddAsync(new Macro
            {
                MacroId = Guid.NewGuid(),
                GuildId = Context.Guild.Id,
                Nome = modal.Nome,
                Mensagem = modal.Mensagem
            });

            await RespondAsync($"Lembre-se que para acionar a macro você deve digitar %{modal.Nome}", ephemeral: true);
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("excluir-macro", "Excluir a macro especificada")]
        public async Task ExcluirMacro([Summary(description: "Nome da macro")] string nome)
        {
            await DeferAsync(ephemeral: true);
            var macro = await db.macros.DeleteAsync(a => a.GuildId == Context.Guild.Id && a.Nome == nome);

            if (macro == null)
                await ModifyOriginalResponseAsync(a => a.Content = "Essa macro não existe.");
            else
                await ModifyOriginalResponseAsync(a => a.Content = "Macro excluída.");
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("editar-macro", "Editar a macro")]
        public async Task EditMacro([Summary(description: "Nome da macro")] string nome) => 
            await Context.Interaction.RespondWithModalAsync<EditMacroModal>($"edit-macro:{nome}", modifyModal: (m) =>
            {
                m.Title = $"Editar Macro {nome}";                
            });

        [ModalInteraction(@"edit-macro:*", TreatAsRegex = true)]
        public async Task MacroEditResponse(EditMacroModal modal)
        {
            string id = ((IModalInteraction)Context.Interaction).Data.CustomId;
            var nome = Regex.Match(id, @"edit-macro:(\w+)").Groups[1].Value;
            var macro = await db.macros.Count(a => a.GuildId == Context.Guild.Id && a.Nome == nome);

            if (macro == 0)
            {
                await RespondAsync($"Não existe uma macro com o nome {nome} no server.", ephemeral: true);
            }

            await db.macros.UpsertAsync(new Macro
            {
                MacroId = Guid.NewGuid(),
                GuildId = Context.Guild.Id,
                Nome = nome,
                Mensagem = modal.Mensagem
            });

            await RespondAsync($"Lembre-se que para acionar a macro você deve digitar %{nome}", ephemeral: true);
        }
    }
}
