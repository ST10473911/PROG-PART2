using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CybersecurityChatbotGUI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Chatbot chatbot;
        private ObservableCollection<string> chatMessages;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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

            // Play voice greeting
            Task.Run(() => chatbot.PlayVoiceGreeting());

            // Display ASCII art
            AsciiArtTextBlock.Text = chatbot.GetAsciiArt();

            // Welcome message
            AddBotMessage("Hello! Welcome to the Cybersecurity Awareness Bot.");
            AddBotMessage("I'm here to help you stay safe online.");
            AddBotMessage("What's your name?");
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendUserMessage();
        }

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendUserMessage();
            }
        }

        private void SendUserMessage()
        {
            string userInput = UserInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userInput)) return;

            AddUserMessage(userInput);
            UserInputTextBox.Clear();

            // Process the input and get response
            string response = chatbot.ProcessInput(userInput);
            AddBotMessage(response);

            // Update status
            StatusTextBlock.Text = $"Helping {chatbot.GetUserName()} learn about {chatbot.GetUserInterest()}";
        }

        private void AddUserMessage(string message)
        {
            chatMessages.Add($"🧑 You: {message}");
            ScrollToBottom();
        }

        private void AddBotMessage(string message)
        {
            chatMessages.Add($"🤖 Bot: {message}");
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (ChatHistoryListBox.Items.Count > 0)
            {
                ChatHistoryListBox.ScrollIntoView(ChatHistoryListBox.Items[ChatHistoryListBox.Items.Count - 1]);
            }
        }
    }
}
