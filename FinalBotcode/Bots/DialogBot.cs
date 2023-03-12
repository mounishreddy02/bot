// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.18.1

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FinalBotcode.Bots
{
    public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly ILogger Logger;
        protected readonly BotState UserState;

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            var activity = turnContext.Activity;

            if (string.IsNullOrWhiteSpace(activity.Text))
            {
                activity.Text = JsonConvert.SerializeObject(activity.Value);
            }

            

            //// Check if the user sent a message
            //if  (turnContext.Activity.Type == ActivityTypes.Message)
            //{
            //    // Check if the user requested a file download
            //    string messageText = turnContext.Activity.Text.ToLowerInvariant();
            //    if (messageText.Contains("download"))
            //    {
            //        // Get the file URL
            //        string fileUrl = "https://sbi.co.in/documents/77530/25386736/100323-Junior+Associates-mains-2022-RESULT-15+FORMAT.pdf";

            //        // Create the attachment
            //        Attachment attachment = new Attachment
            //        {
            //            ContentUrl = fileUrl,
            //            ContentType = "application/pdf",
            //            Name = "sample.pdf"
            //        };

            //        // Send the attachment to the user
            //        var message = MessageFactory.Text("Here is the file you requested:");
            //        message.Attachments = new List<Attachment> { attachment };
            //        await turnContext.SendActivityAsync(message);

            //        // End the conversation
            //        await turnContext.SendActivityAsync(MessageFactory.Text("Thank you for using our service. Goodbye!"));
            //        // End the conversation
            //        var endOfConversation = Activity.CreateEndOfConversationActivity();
            //        await turnContext.SendActivityAsync(endOfConversation);



            //    }
            //}

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeCardAttachment = CreateAdaptiveCardAttachment();
                    var welcomeMessage = MessageFactory.Attachment(welcomeCardAttachment, "Hello there! I'm the ChatTicket booking chatbot, how may I assist you today with your booking needs?");
                    await turnContext.SendActivityAsync(welcomeMessage, cancellationToken);
                }
            }
        }

        private Attachment CreateAdaptiveCardAttachment()
        {
            var cardJsonPath = Path.Combine(".", "Cards", "welcomeCard.json");
            var cardJson = File.ReadAllText(cardJsonPath);
            var adaptiveCard = AdaptiveCard.FromJson(cardJson).Card;
            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = adaptiveCard
            };
        }


        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}
