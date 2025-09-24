﻿using System.Globalization;
using ChatTwo.Code;
using ChatTwo.Http.MessageProtocol;
using ChatTwo.Util;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace ChatTwo.Http;

public class Processing
{
    private readonly Plugin Plugin;

    public Processing(Plugin plugin)
    {
        Plugin = plugin;
    }

    internal (MessageTemplate[] Name, bool Locked) ReadChannelName(Chunk[] channelName)
    {
        var locked = Plugin.CurrentTab is not { Channel: null };
        return (channelName.Select(ProcessChunk).ToArray(), locked);
    }

    internal async Task<MessageResponse[]> ReadMessageList()
    {
        var tabMessages = await Plugin.CurrentTab.Messages.GetCopy();
        return tabMessages.TakeLast(Plugin.Config.WebinterfaceMaxLinesToSend).Select(ReadMessageContent).ToArray();
    }

    internal MessageResponse ReadMessageContent(Message message)
    {
        var response = new MessageResponse
        {
            Timestamp = message.Date.ToLocalTime().ToString("t", !Plugin.Config.Use24HourClock ? null : CultureInfo.CreateSpecificCulture("es-ES"))
        };

        var sender = message.Sender.Select(ProcessChunk);
        var content = message.Content.Select(ProcessChunk);
        response.Templates = sender.Concat(content).ToArray();

        return response;
    }

    internal async Task PrepareNewClient(SSEConnection sse)
    {
        // This takes long, so keep it outside the next frame
        var messages = await GetAllMessages();

        // Using the bulk message event to clear everything on the client side that may still exist
        await Plugin.Framework.RunOnTick(() =>
        {
            sse.OutboundQueue.Enqueue(new BulkMessagesEvent(messages));

            sse.OutboundQueue.Enqueue(new SwitchChannelEvent(GetCurrentChannel()));
            sse.OutboundQueue.Enqueue(new ChannelListEvent(GetValidChannels()));

            sse.OutboundQueue.Enqueue(new ChatTabSwitchedEvent(GetCurrentTab()));
            sse.OutboundQueue.Enqueue(new ChatTabListEvent(GetAllTabs()));
        });
    }

    private MessageTemplate ProcessChunk(Chunk chunk)
    {
        if (chunk is IconChunk { } icon)
        {
            var iconId = (uint)icon.Icon;
            return IconUtil.GfdFileView.TryGetEntry(iconId, out _) ? new MessageTemplate {Payload = "icon", Id = iconId}: MessageTemplate.Empty;
        }

        if (chunk is TextChunk { } text)
        {
            if (chunk.Link is EmotePayload emotePayload && Plugin.Config.ShowEmotes)
            {
                var image = EmoteCache.GetEmote(emotePayload.Code);

                if (image is { Failed: false })
                    return new MessageTemplate { Payload = "emote", Color = 0, Content = emotePayload.Code };
            }

            var color = text.Foreground;
            if (color == null && text.FallbackColour != null)
            {
                var type = text.FallbackColour.Value;
                color = Plugin.Config.ChatColours.TryGetValue(type, out var col) ? col : type.DefaultColor();
            }

            color ??= 0;

            var userContent = text.Content ?? string.Empty;
            if (Plugin.ChatLogWindow.ScreenshotMode)
            {
                if (chunk.Link is PlayerPayload playerPayload)
                    userContent = Plugin.ChatLogWindow.HidePlayerInString(userContent, playerPayload.PlayerName, playerPayload.World.RowId);
                else if (Plugin.ClientState.LocalPlayer is { } player)
                    userContent = Plugin.ChatLogWindow.HidePlayerInString(userContent, player.Name.TextValue, player.HomeWorld.RowId);
            }

            var isNotUrl = text.Link is not UriPayload;
            return new MessageTemplate { Payload = isNotUrl ? "text" : "url", Color = color.Value, Content = userContent };
        }

        return MessageTemplate.Empty;
    }

    private async Task<Messages> GetAllMessages()
    {
        var messages = await WebserverUtil.FrameworkWrapper(ReadMessageList);
        return new Messages(messages);
    }

    private SwitchChannel GetCurrentChannel()
    {
        var channel = ReadChannelName(Plugin.ChatLogWindow.PreviousChannel);
        return new SwitchChannel(channel);
    }

    private ChannelList GetValidChannels()
    {
        var channels = Plugin.ChatLogWindow.GetValidChannels();
        return new ChannelList(channels.ToDictionary(pair => pair.Key, pair => (uint)pair.Value));
    }

    private ChatTab GetCurrentTab()
    {
        return new ChatTab(Plugin.CurrentTab.Name, Plugin.LastTab);
    }

    private ChatTabList GetAllTabs()
    {
        var tabs = Plugin.Config.Tabs.Select((tab, idx) => new ChatTab(tab.Name, idx)).ToArray();
        return new ChatTabList(tabs);
    }
}
