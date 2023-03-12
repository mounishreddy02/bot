// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Activity = Microsoft.Bot.Schema.Activity;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using static System.Net.WebRequestMethods;
using System.Text.RegularExpressions;

namespace FinalBotcode.Bots
{
    public class MainDialog : ComponentDialog
    {


        private readonly HttpClient _httpClient;
      

       

       
        private readonly UserState _userState;
        private const string AgePrompt = "agePrompt";
        private const string cinemaprompt = "cinemaprompt";
        private const string nameprompt = "nameprompt";
        private const string emailprompt = "emailprompt";
        public MainDialog(UserState userState, HttpClient httpClient)

        : base(nameof(MainDialog))
        {

            _httpClient = httpClient;
         
            _userState = userState;

            AddDialog(new TopLevelDialog());

            

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {

               
                ///
                GetcinemaAsync,
                GetNameAsync,
                GetAgeAsync,
                GetEmailAsync,
                GetTicketsAsync,
                ConfirmAsync,
                FinalStepAsyncdata,
                FinalStepAsyncdataclose



            }));

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));


            AddDialog(new NumberPrompt<int>("agePrompt", ValidateAge));

            // Add the name prompt
            AddDialog(new TextPrompt("nameprompt", ValidateName));

            AddDialog(new TextPrompt("emailprompt", ValidateEmail));

            AddDialog(new TextPrompt("cinemaprompt"));

            // Add the tickets prompt
            AddDialog(new NumberPrompt<int>("ticketsPrompt", ValidateTickets));

            // Add the confirm prompt
            AddDialog(new ConfirmPrompt("confirmPrompt"));

            // Add the inputs prompt
            AddDialog(new TextPrompt("inputsPrompt", ValidateInputs));



            InitialDialogId = nameof(WaterfallDialog);








        }

        

        private static Task<bool> ValidateName(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Define a regular expression pattern for matching numeric characters
            Regex regex = new Regex(@"\d");

            // Validate that the user's name does not contain any numeric characters
            return Task.FromResult(promptContext.Recognized.Succeeded && !regex.IsMatch(promptContext.Recognized.Value));
        }


        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity;
            if (activity.Type == ActivityTypes.Message && activity.Text.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                 await innerDc.Context.SendActivityAsync(MessageFactory.Text("Thank you for using our service. lets strt from beginning!"));
                await innerDc.CancelAllDialogsAsync(cancellationToken);
                return await innerDc.ReplaceDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
            }
           
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }


        private async Task<DialogTurnResult> AskQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Prompt the user to enter a question
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your question:") }, cancellationToken);
        }



      

       
        private static Task<bool> ValidateInputs(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Split the input into age, name, tickets, and confirmation
            var inputs = promptContext.Recognized.Value.Split(',');
            if (inputs.Length != 4)
            {
                return Task.FromResult(false);
            }

            // Validate that the age is an integer greater than 0
            if (!int.TryParse(inputs[0], out int age) || age <= 0)
            {
                return Task.FromResult(false);
            }

            // Validate that the name is not empty
            if (string.IsNullOrEmpty(inputs[1]))
            {
                return Task.FromResult(false);
            }

            // Validate that the tickets is an integer greater than 0
            if (!int.TryParse(inputs[2], out int tickets) || tickets <= 0)
            {
                return Task.FromResult(false);
            }

            // Validate that the confirmation is either "yes" or "no"
            if (!inputs[3].Equals("yes", StringComparison.InvariantCultureIgnoreCase) && !inputs[3].Equals("no", StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult(false);
            }

            // If all input is valid, return true
            return Task.FromResult(true);
        }



        private async Task<DialogTurnResult> FinalStepAsyncforssingleline(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the inputs the user provided
            var inputs = stepContext.Result.ToString();

            // Split the inputs into age, name, tickets, and confirmation
            var inputsArray = inputs.Split(',');
            var age = int.Parse(inputsArray[0]);
            var name = inputsArray[1];
            var tickets = int.Parse(inputsArray[2]);
            //var confirm = inputsArray[3].Equals("yes", StringComparison.InvariantCultureIgnoreCase);

            // Process the user's order
            //await ProcessOrderAsync(age, name, tickets, confirm, cancellationToken);
            await stepContext.Context.SendActivityAsync(
              MessageFactory.Text($"Your age is {age}, your name is {name}, and you want to purchase {tickets} tickets."),
              cancellationToken);
            // End the dialog
            return await stepContext.EndDialogAsync(cancellationToken);
        }




        private async Task<DialogTurnResult> GetInputsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Prompt the user for their age, name, tickets, and confirmation
            return await stepContext.PromptAsync(
                "inputsPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter your age, name, number of tickets, and confirmation (yes/no) separated by commas."),
                    RetryPrompt = MessageFactory.Text("Please enter valid input in the format 'age, name, tickets, confirmation'."),
                },
                cancellationToken);
        }



        private static Task<bool> ValidateEmail(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user's email is not null or empty and is in a valid format
            string emailPattern = @"^[a-zA-Z0-9+_.-]+@[a-zA-Z0-9.-]+\.[a-zA-Z0-9.-]+$";
            return Task.FromResult(promptContext.Recognized.Succeeded && !string.IsNullOrWhiteSpace(promptContext.Recognized.Value) && Regex.IsMatch(promptContext.Recognized.Value, emailPattern));
        }



        private static Task<bool> ValidateAge(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user's age is greater than 0
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 0);
        }

        private static Task<bool> ValidateTickets(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user is purchasing at least 1 ticket
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 0);
        }
        private async Task<DialogTurnResult> GetcinemaAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Please select one of the cinemas below from the carousel, and I'll help you book your tickets. Simply click on the cinema you're interested in to proceed with your booking.");
            // 
            //// Prompt the user for their age
            //return await stepContext.PromptAsync(
            //    "agePrompt",
            //    new PromptOptions
            //    {
            //        Prompt = MessageFactory.Text("What is your age?"),
            //        RetryPrompt = MessageFactory.Text("Please enter a valid age."),
            //    },
            //    cancellationToken);



            //            var heroCards = new List<HeroCard>
            //            {
            //                new HeroCard
            //{
            //    Title = "Under 18",
            //    Subtitle = "Select this option if you are under 18",
            //    Images = new List<CardImage> { new CardImage(url: "https://tse3.mm.bing.net/th?id=OIP.h0mGPK5el-oHlkBToCpMngAAAA&pid=Api&P=0") },
            //    Buttons = new List<CardAction>
            //    {
            //        new CardAction(ActionTypes.ImBack, "Under 18", value: "Under 18"),
            //    },
            //},

            //                new HeroCard
            //                {
            //                    Title = "18-30",
            //                    Subtitle = "Select this option if you are between 18 and 30",
            //                    Buttons = new List<CardAction>
            //                    {
            //                        new CardAction(ActionTypes.ImBack, "18-30", value: "18-30"),
            //                    },
            //                },
            //                new HeroCard
            //                {
            //                    Title = "31-45",
            //                    Subtitle = "Select this option if you are between 31 and 45",
            //                    Buttons = new List<CardAction>
            //                    {
            //                        new CardAction(ActionTypes.ImBack, "31-45", value: "31-45"),
            //                    },
            //                },
            //                new HeroCard
            //                {
            //                    Title = "46 and over",
            //                    Subtitle = "Select this option if you are 46 or older",
            //                    Buttons = new List<CardAction>
            //                    {
            //                        new CardAction(ActionTypes.ImBack, "46 and over", value: "46 and over"),
            //                    },
            //                },
            //            };

            //            var attachments = new List<Attachment>();
            //            foreach (var card in heroCards)
            //            {
            //                attachments.Add(card.ToAttachment());
            //            }

            //            var reply = MessageFactory.Attachment(attachments);
            //            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            //           // Prompt the user to select their age
            //            return await stepContext.PromptAsync(AgePrompt, new PromptOptions
            //            {
            //                Prompt = (Activity)reply,
            //                RetryPrompt = MessageFactory.Text("Please select an age option from the carousel."),
            //            }, cancellationToken);


            //var heroCards = new List<HeroCard>();

            //string[] ageRanges = { "Under 18", "18-30", "31-45", "46 and over" };
            //string[] ageValues = { "Under 18", "18-30", "31-45", "46 and over" };

            //for (int i = 0; i < ageRanges.Length; i++)
            //{
            //    heroCards.Add(new HeroCard
            //    {
            //        Title = ageRanges[i],
            //        Subtitle = $"Select this option if you are {ageRanges[i].ToLower()}",
            //        Buttons = new List<CardAction>
            //{
            //    new CardAction(ActionTypes.ImBack, ageRanges[i], value: ageValues[i]),
            //},
            //    });
            //}
            //var attachments = new List<Attachment>();
            //foreach (var card in heroCards)
            //{
            //    attachments.Add(card.ToAttachment());
            //}

            //var reply = MessageFactory.Attachment(attachments);
            //reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            //// Prompt the user to select their age
            //return await stepContext.PromptAsync(AgePrompt, new PromptOptions
            //{
            //    Prompt = (Activity)reply,
            //    RetryPrompt = MessageFactory.Text("Please select an age option from the carousel."),
            //}, cancellationToken);

            //           var heroCards = new List<HeroCard>();

            //string[] cinemaNames = { "VeerasimhaReddy", "Sridevi Shoban Babu", "Dasara", "Vinaro Bhagyamu Vishnu Katha" };
            //string[] cinemaValues = { "VeerasimhaReddy", "Sridevi Shoban Babu", "Dasara", "Vinaro Bhagyamu Vishnu Katha" };
            //  string[] cinemaimages = {
            //                                                                                    "https://www.filmibeat.com/img/190x100x237/popcorn/movie_posters/veerasimhareddy-20221022191218-20142.jpg",
            //                                                                                    "https://www.filmibeat.com/img/190x100x237/popcorn/movie_posters/sridevishobanbabu-20230215151127-21593.jpg",
            //                                                                                    "https://www.filmibeat.com/img/190x100x237/popcorn/movie_posters/dasara-20220320122651-20470.jpg",
            //                                                                                    "https://www.filmibeat.com/img/190x100x237/popcorn/movie_posters/vinarobhagyamuvishnukatha-20220412201416-20894.jpg" };


            //            for (int i = 0; i < cinemaNames.Length; i++)
            //{
            //    heroCards.Add(new HeroCard
            //    {
            //        Title = cinemaNames[i],
            //        Subtitle = $"Select this option to book tickets for {cinemaNames[i]}",
            //        Images = new List<CardImage> { new CardImage(url: cinemaimages[i]) },
            //        Buttons = new List<CardAction>
            //        {
            //            new CardAction(ActionTypes.ImBack, cinemaNames[i], value: cinemaValues[i]),
            //        },
            //    });
            //}

            //var attachments = new List<Attachment>();
            //foreach (var card in heroCards)
            //{
            //    attachments.Add(card.ToAttachment());
            //}

            //var reply = MessageFactory.Attachment(attachments);
            //reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            //// Prompt the user to select a cinema
            //return await stepContext.PromptAsync(cinemaprompt, new PromptOptions
            //{
            //    Prompt = (Activity)reply,
            //    RetryPrompt = MessageFactory.Text("Please select a cinema from the carousel."),
            //}, cancellationToken);

               //  var heroCards = new List<HeroCard>();

            //          string[] cinemaNames = { "VeerasimhaReddy", "Sridevi Shoban Babu", "Dasara", "Vinaro Bhagyamu Vishnu Katha" };
            //          string[] cinemaValues = { "VeerasimhaReddy", "Sridevi Shoban Babu", "Dasara", "Vinaro Bhagyamu Vishnu Katha" };
            ////          string[] cinemaimages = {
            ////                  "https://tse3.mm.bing.net/th?id=OIP.udW0MP_GDBuLwd8xClz7pAAAAA&pid=Api&P=0",
            ////"https://tse1.mm.bing.net/th?id=OIP.wGZZ2q24CfBuI0e_MfzXTgHaHa&pid=Api&P=0",
            ////  "https://tse2.mm.bing.net/th?id=OIP.d1XdAvOUCWUVDOW9xBdGoQHaLk&pid=Api&P=0",
            ////  "https://tse2.mm.bing.net/th?id=OIF.P77YgS3qXsVPe6cSBst%2bUw&pid=Api&P=0" };

            //          string[] cinemaimages = {
            //                  "https://tse3.mm.bing.net/th?id=OIP.udW0MP_GDBuLwd8xClz7pAAAAA&pid=Api&P=0",
            //"https://tse3.mm.bing.net/th?id=OIP.udW0MP_GDBuLwd8xClz7pAAAAA&pid=Api&P=0",
            //  "https://tse3.mm.bing.net/th?id=OIP.udW0MP_GDBuLwd8xClz7pAAAAA&pid=Api&P=0",
            //  "https://tse3.mm.bing.net/th?id=OIP.udW0MP_GDBuLwd8xClz7pAAAAA&pid=Api&P=0" };


            //          for (int i = 0; i < cinemaNames.Length; i++)
            //          {
            //              heroCards.Add(new HeroCard
            //              {
            //                  Title = cinemaNames[i],
            //                  Subtitle = $"Select this option if you are {cinemaNames[i].ToLower()}",
            //                  Images = new List<CardImage> { new CardImage(url: cinemaimages[i]) },
            //                  Buttons = new List<CardAction>
            //          {
            //              new CardAction(ActionTypes.ImBack, cinemaValues[i], value: cinemaValues[i]),
            //          },
            //              });
            //          }
            //          var attachments = new List<Attachment>();
            //          foreach (var card in heroCards)
            //          {
            //              attachments.Add(card.ToAttachment());
            //          }

            //          var reply = MessageFactory.Attachment(attachments);
            //          reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            //          // Prompt the user to select their age
            //          return await stepContext.PromptAsync(AgePrompt, new PromptOptions
            //          {
            //              Prompt = (Activity)reply,
            //              RetryPrompt = MessageFactory.Text("Please select an age option from the carousel."),
            //          }, cancellationToken);



            var heroCards = new List<HeroCard>
            {
                new HeroCard
                {
                    Title = "VeerasimhaReddy",
                    Subtitle = "వీరసింహారెడ్డి",
                    Images = new List<CardImage> { new CardImage(url: "https://tse3.mm.bing.net/th?id=OIP.h0mGPK5el-oHlkBToCpMngAAAA&pid=Api&P=0") },
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "VeerasimhaReddy", value: "VeerasimhaReddy"),
                    },
                },
                new HeroCard
                {
                    Title = "Sridevi Shoban Babu",
                    Subtitle = "Sridevi Shoban Babu",
                    Images = new List<CardImage> { new CardImage(url: "https://tse3.mm.bing.net/th?id=OIP.sxobMOOZ2ype5f7k0dDsiwHaDt&pid=Api&P=0") },
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "Sridevi Shoban Babu", value: "Sridevi Shoban Babu"),
                    },
                },
                new HeroCard
                {
                    Title = "Sridevi Shoban Babu",
                    Subtitle = "Sridevi Shoban Babu",
                    Images = new List<CardImage> { new CardImage(url: "https://tse3.mm.bing.net/th?id=OIP.h0mGPK5el-oHlkBToCpMngAAAA&pid=Api&P=0") },
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "Sridevi Shoban Babu", value: "Sridevi Shoban Babu"),
                    },
                },
                new HeroCard
                {
                    Title = "Sridevi Shoban Babu",
                    Subtitle = "Sridevi Shoban Babu",
                    Images = new List<CardImage> { new CardImage(url: "https://tse3.mm.bing.net/th?id=OIP.h0mGPK5el-oHlkBToCpMngAAAA&pid=Api&P=0") },
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "Sridevi Shoban Babu", value: "Sridevi Shoban Babu"),
                    },
                },
            };

            var attachments = new List<Attachment>();
            foreach (var card in heroCards)
            {
                attachments.Add(card.ToAttachment());
            }

            var reply = MessageFactory.Attachment(attachments);
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            // Prompt the user to select their age
            return await stepContext.PromptAsync(cinemaprompt, new PromptOptions
            {
                Prompt = (Activity)reply,
                RetryPrompt = MessageFactory.Text("Please select an age option from the carousel."),
            }, cancellationToken);





        }

        private async Task<DialogTurnResult> GetNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the age the user provided
            //var age = (int)stepContext.Result;
            //stepContext.Values["age"] = age;

            var moviename = stepContext.Result;
            stepContext.Values["moviename"] = moviename;

            // Prompt the user for their name
            return await stepContext.PromptAsync(
                "nameprompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Sure 😎, what is your name?"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid name."),
                },
                cancellationToken);


            
        }


        private async Task<DialogTurnResult> GetAgeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var name = (string)stepContext.Result;
            stepContext.Values["name"] = name;
            //// Prompt the user for their age
            return await stepContext.PromptAsync(
                "agePrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What is your age?"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid age."),
                },
                cancellationToken);

        }

        private async Task<DialogTurnResult> GetEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the name the user provided
            var age = (int)stepContext.Result;
            stepContext.Values["age"] = age;

            //// After the user enters their name, prompt them for their email
            return await stepContext.PromptAsync(
                "emailprompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What is your email address?"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid email address."),
                },
                cancellationToken);





        }

        private Attachment CreateAdaptiveCardAttachment()
        {
            var cardJsonPath = Path.Combine(".", "Cards", "welcomeCard.json");
            var cardJson = System.IO.File.ReadAllText(cardJsonPath);
            var adaptiveCard = AdaptiveCard.FromJson(cardJson).Card;
            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = adaptiveCard
            };
        }

        private async Task<DialogTurnResult> GetTicketsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            // Get the email the user provided in the previous step
            var email = (string)stepContext.Result;
            stepContext.Values["email"] = email;

           
            // Prompt the user for the number of tickets they want to purchase
            return await stepContext.PromptAsync(
                "ticketsPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("How many tickets do you want to purchase?"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid number of tickets."),
                },

                cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the number of tickets the user provided
            var tickets = (int)stepContext.Result;
            stepContext.Values["tickets"] = tickets;





            // Prompt the user to confirm their order
            return await stepContext.PromptAsync(
                "confirmPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Do you want to purchase {tickets} tickets?"),
                    RetryPrompt = MessageFactory.Text("Please confirm your order by entering 'yes' or 'no'."),
                },
                cancellationToken);
        }



        private async Task<DialogTurnResult> FinalStepAsyncdata(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the age, name, tickets, and confirmation values from the previous steps
            var moviename = stepContext.Values["moviename"];
            var name = (string)stepContext.Values["name"];
            var age = (int)stepContext.Values["age"];
            var tickets = (int)stepContext.Values["tickets"];
            var confirm = (bool)stepContext.Result;

            // Show the user the information they provided
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Your movie name is {moviename}.\nYour name is {name}.\nYour age is {age}.\nYou want to purchase {tickets} tickets.\nYour order is confirmed: {confirm}."),
                cancellationToken);

            // Process the user's order
            // await ProcessOrderAsync(age, name, tickets, confirm, cancellationToken);

            // Prompt the user to start again
            await stepContext.Context.SendActivityAsync("Would you like to book tickets again? If yes,you can simply say \"hi\".");

            // Continue to the next step
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsyncdataclose(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // End the dialog and send a final message to the user
            return await stepContext.EndDialogAsync("Thank you for your order. Goodbye!", cancellationToken);
        }



        //private async Task ProcessOrderAsync(int age, string name, int tickets, bool confirm, CancellationToken cancellationToken)
        //{
        //    if (confirm)
        //    {
        //        // Save the order to the database
        //        await SaveOrderAsync(age, name, tickets, cancellationToken);

        //        // Send a confirmation email to the user
        //        await SendConfirmationEmailAsync(age, name, tickets, cancellationToken);
        //    }
        //    else
        //    {
        //        // Send a message to the user indicating that their order was not confirmed
        //        await stepContext.Context.SendActivityAsync(
        //            MessageFactory.Text("Your order was not confirmed."),
        //            cancellationToken);
        //    }
        //}
































    }
}
