using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;

namespace CybersecurityChatbotGUI
{
    public class Chatbot
    {
        private string userName;
        private string userInterest;
        private string lastTopic;
        private Random random = new Random();
        private SpeechSynthesizer speechSynthesizer;
        private Dictionary<string, List<string>> keywordResponses;
        private Dictionary<string, string> followUpResponses;
        private Dictionary<string, string> sentimentResponses;

        public Chatbot()
        {
            InitializeResponses();
            InitializeSentimentResponses();
            speechSynthesizer = new SpeechSynthesizer();
        }

        private void InitializeResponses()
        {
            keywordResponses = new Dictionary<string, List<string>>();

            // Add responses with lowercase keys for easier matching
            keywordResponses.Add("password", new List<string>
            {
                "🔐 Create strong passwords with at least 12 characters including uppercase, lowercase, numbers, and symbols.",
                "🔑 Never reuse passwords across different accounts. Use a password manager!",
                "🛡️ Enable two-factor authentication whenever possible for extra security."
            });

            keywordResponses.Add("scan", new List<string>
            {
                "📱 Regularly scan your devices for malware using trusted antivirus software.",
                "🔍 Before downloading files, scan them with Windows Defender first.",
                "✅ Run weekly security scans to detect and remove potential threats."
            });

            keywordResponses.Add("privacy", new List<string>
            {
                "👁️ Review privacy settings on social media to control who sees your information.",
                "🔒 Use a VPN when using public Wi-Fi to protect your personal data.",
                "📧 Avoid sharing personal information like your ID number or address in emails."
            });

            keywordResponses.Add("phishing", new List<string>
            {
                "🎣 Never click on suspicious links in emails or text messages.",
                "⚠️ Check the sender's email address carefully - scammers use fake addresses.",
                "🔗 Hover over links before clicking to see the actual URL destination."
            });

            keywordResponses.Add("how are you", new List<string>
            {
                "I'm doing great! How can I help you with cybersecurity today?",
                "I'm fully secure and ready to assist you!",
                "Feeling safe and sound! What would you like to learn about?"
            });

            keywordResponses.Add("purpose", new List<string>
            {
                "My purpose is to educate South African citizens about staying safe online!",
                "I'm here to help you learn about cybersecurity threats and how to avoid them.",
                "Think of me as your personal cybersecurity assistant!"
            });

            keywordResponses.Add("ask about", new List<string>
            {
                "You can ask me about: passwords, scanning, privacy, phishing, or safe browsing!",
                "Try asking about 'password safety', 'phishing tips', or 'privacy'.",
                "I can help with password security, privacy protection, and spotting phishing attempts!"
            });

            keywordResponses.Add("help", new List<string>
            {
                "Available topics: password, scan, privacy, phishing.\nSay 'my name is [name]' so I remember you!\nSay 'I'm interested in [topic]' to share your interests.",
                "Type 'password', 'scan', 'privacy', or 'phishing' to learn.\nI remember your name and interests too!"
            });

            followUpResponses = new Dictionary<string, string>();
            followUpResponses.Add("tell me more", "Here's more information about {topic}:");
            followUpResponses.Add("another tip", "Another helpful tip about {topic}:");
            followUpResponses.Add("explain more", "Let me explain further about {topic}:");
        }

        private void InitializeSentimentResponses()
        {
            sentimentResponses = new Dictionary<string, string>();
            sentimentResponses.Add("worried", "It's completely normal to feel worried about online security. Don't worry - I'm here to help! Here's a tip:");
            sentimentResponses.Add("scared", "I understand your concern. Let me share something that will help you feel safer:");
            sentimentResponses.Add("frustrated", "I hear your frustration. Let me simplify this for you:");
            sentimentResponses.Add("curious", "That's great that you're curious! Here's what you should know:");
            sentimentResponses.Add("confused", "Let me explain this in simpler terms:");
        }

        public void PlayVoiceGreeting()
        {
            try
            {
                string fullPath = "greeting.wav";
                if (System.IO.File.Exists(fullPath))
                {
                    using (var player = new SoundPlayer(fullPath))
                    {
                        player.PlaySync();
                    }
                }
                else
                {
                    speechSynthesizer.Speak("Hello! Welcome to the Cybersecurity Awareness Bot. I'm here to help you stay safe online.");
                }
            }
            catch (Exception)
            {
                // Silently continue if audio fails
            }
        }

        public string GetAsciiArt()
        {
            return @"   ________  ___  ___  ___  ___  ________   
  |\   ____\|\  \|\  \|\  \|\  \|\   ____\  
  \ \  \___|\ \  \\\  \ \  \\\  \ \  \___|  
   \ \  \    \ \   __  \ \   __  \ \  \    
    \ \  \____\ \  \ \  \ \  \ \  \ \  \____
     \ \_______\ \__\ \__\ \__\ \__\ \_______\
      \|_______|\|__|\|__|\|__|\|__|\|_______|
      
              🔐 CYBERSECURITY BOT 🔐
           Protecting South African citizens";
        }

        public string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Please type a message so I can help you with cybersecurity!";
            }

            // Convert to lowercase for easier matching
            string lowerInput = input.ToLower();

            // Check for sentiment
            string sentiment = DetectSentiment(lowerInput);
            if (sentiment != null && sentimentResponses.ContainsKey(sentiment))
            {
                return sentimentResponses[sentiment] + " " + GetRandomCybersecurityTip();
            }

            // Extract and remember name
            if (string.IsNullOrEmpty(userName))
            {
                string extractedName = ExtractName(input);
                if (extractedName != null)
                {
                    userName = extractedName;
                    return $"Nice to meet you, {userName}! I'll remember that. What would you like to learn about cybersecurity today?";
                }
            }

            // Extract and remember interest
            string interest = ExtractInterest(lowerInput);
            if (interest != null)
            {
                userInterest = interest;
                return $"Great! I'll remember that you're interested in {interest}. It's very important for staying safe online!";
            }

            // Handle follow-up
            string followUpResponse = HandleFollowUp(lowerInput);
            if (followUpResponse != null)
            {
                return followUpResponse;
            }

            // Check for keywords
            string keywordResponse = GetKeywordResponse(lowerInput);
            if (keywordResponse != null)
            {
                lastTopic = keywordResponse;
                return keywordResponse;
            }

            // Recall memory
            if (lowerInput.Contains("my name") || lowerInput.Contains("who am i"))
            {
                return !string.IsNullOrEmpty(userName) ? $"You told me your name is {userName}!" : "I don't know your name yet. What should I call you?";
            }

            if (lowerInput.Contains("my interest") || lowerInput.Contains("interested in"))
            {
                return !string.IsNullOrEmpty(userInterest) ? $"You're interested in {userInterest}. Would you like to learn more?" : "You haven't told me what cybersecurity topic interests you yet.";
            }

            return "I'm not sure I understand. You can ask me about passwords, scanning, privacy, or phishing. Type 'help' to see options.";
        }

        private string GetKeywordResponse(string input)
        {
            foreach (var keyword in keywordResponses.Keys)
            {
                if (input.Contains(keyword))
                {
                    List<string> responses = keywordResponses[keyword];
                    return responses[random.Next(responses.Count)];
                }
            }
            return null;
        }

        private string HandleFollowUp(string input)
        {
            if (string.IsNullOrEmpty(lastTopic)) return null;

            foreach (var followUp in followUpResponses.Keys)
            {
                if (input.Contains(followUp))
                {
                    foreach (var keyword in keywordResponses.Keys)
                    {
                        if (lastTopic.Contains(keyword))
                        {
                            var responses = keywordResponses[keyword];
                            return followUpResponses[followUp].Replace("{topic}", keyword) + " " + responses[random.Next(responses.Count)];
                        }
                    }
                    return $"Let me give you more information. {GetRandomCybersecurityTip()}";
                }
            }
            return null;
        }

        private string DetectSentiment(string input)
        {
            if (Regex.IsMatch(input, @"worried|anxious|nervous|concerned")) return "worried";
            if (Regex.IsMatch(input, @"scared|terrified|fear|afraid")) return "scared";
            if (Regex.IsMatch(input, @"frustrated|annoyed|angry")) return "frustrated";
            if (Regex.IsMatch(input, @"curious|interested|want to learn")) return "curious";
            if (Regex.IsMatch(input, @"confused|don't understand")) return "confused";
            return null;
        }

        private string ExtractName(string input)
        {
            string lowerInput = input.ToLower();

            if (lowerInput.Contains("my name is"))
            {
                int index = lowerInput.IndexOf("my name is") + 10;
                string name = input.Substring(index).Trim().Split(' ')[0];
                return name;
            }
            if (lowerInput.Contains("call me"))
            {
                int index = lowerInput.IndexOf("call me") + 7;
                string name = input.Substring(index).Trim().Split(' ')[0];
                return name;
            }
            if (lowerInput.Contains("i am") && !lowerInput.Contains("i am worried") && !lowerInput.Contains("i am scared"))
            {
                int index = lowerInput.IndexOf("i am") + 4;
                string name = input.Substring(index).Trim().Split(' ')[0];
                return name;
            }
            if (lowerInput.Contains("i'm") && !lowerInput.Contains("i'm interested"))
            {
                int index = lowerInput.IndexOf("i'm") + 3;
                string name = input.Substring(index).Trim().Split(' ')[0];
                return name;
            }
            return null;
        }

        private string ExtractInterest(string input)
        {
            if (input.Contains("interested in password") || input.Contains("like password")) return "password";
            if (input.Contains("interested in privacy") || input.Contains("like privacy")) return "privacy";
            if (input.Contains("interested in phishing") || input.Contains("like phishing")) return "phishing";
            if (input.Contains("interested in scan") || input.Contains("like scanning")) return "scanning";
            return null;
        }

        private string GetRandomCybersecurityTip()
        {
            string[] tips = {
                "Always use strong, unique passwords for each account.",
                "Enable two-factor authentication wherever possible.",
                "Never share your passwords with anyone.",
                "Keep your software and operating system updated.",
                "Use antivirus software and keep it updated.",
                "Back up your important files regularly."
            };
            return tips[random.Next(tips.Length)];
        }

        public string GetUserName() => userName ?? "Friend";
        public string GetUserInterest() => userInterest ?? "cybersecurity";
    }
}
