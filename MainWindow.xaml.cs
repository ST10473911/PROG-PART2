using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CybersecurityChatbotGUI
{
    public partial class MainWindow : Window
    {
        private Chatbot chatbot;
        private ObservableCollection<string> chatMessages;

        public MainWindow()
        {
            InitializeComponent();
            InitializeChatbot();
        }

        private void InitializeChatbot()
        {
            chatbot = new Chatbot();
            chatMessages = new ObservableCollection<string>();
            ChatHistoryListBox.ItemsSource = chatMessages;

            Task.Run(() => chatbot.PlayVoiceGreeting());
            AsciiArtTextBlock.Text = chatbot.GetAsciiArt();

            AddBotMessage("🔐 Hello! Welcome to the Cybersecurity Awareness Bot.");
            AddBotMessage("🇿🇦 I'm here to help South African citizens stay safe online.");
            AddBotMessage("What's your name? (Example: 'My name is Thabo')");
            AddBotMessage("💡 Try these commands:");
            AddBotMessage("  • 'add task: Enable 2FA'");
            AddBotMessage("  • 'show tasks'");
            AddBotMessage("  • 'start quiz'");
            AddBotMessage("  • 'show log'");
            AddBotMessage("  • 'help'");
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) => SendUserMessage();
        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) SendUserMessage(); }

        private void SendUserMessage()
        {
            string userInput = UserInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userInput)) return;

            AddUserMessage(userInput);
            UserInputTextBox.Clear();

            string response = chatbot.ProcessInput(userInput);
            AddBotMessage(response);

            // Update status bar
            StatusTextBlock.Text = $"🛡️ Helping {chatbot.GetUserName()} learn about {chatbot.GetUserInterest()}";

            // Update quiz status if quiz is active
            if (chatbot.IsQuizActive())
            {
                QuizStatusTextBlock.Text = $"📝 Quiz in progress: Question {chatbot.GetQuizProgress() + 1}/{chatbot.GetTotalQuestions()}";
                ScoreTextBlock.Text = $"Score: {chatbot.GetQuizScore()} correct";
            }
            else
            {
                QuizStatusTextBlock.Text = "";
                ScoreTextBlock.Text = "";
            }
        }

        private void AddUserMessage(string message) => chatMessages.Add($"🧑 You: {message}");
        private void AddBotMessage(string message) => chatMessages.Add($"🤖 Bot: {message}");
    }
}
