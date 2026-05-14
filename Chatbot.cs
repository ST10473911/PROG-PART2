using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace CybersecurityChatbotGUI
{
    public class Chatbot
    {
        // Memory storage
        private string userName;
        private string userInterest;
        private string lastTopic;
        private Dictionary<string, string> userMemory = new Dictionary<string, string>();

        // Keyword responses
        private Dictionary<string, List<string>> keywordResponses;
        private Dictionary<string, string> followUpResponses;

        // Sentiment detection keywords
        private Dictionary<string, string> sentimentResponses;

        private Random random = new Random();
        private SpeechSynthesizer speechSynthesizer;

        // Track conversation history for follow-ups
        private Queue<string> conversationHistory = new Queue<string>();

        public Chatbot()
        {
            InitializeResponses();
            InitializeSentimentResponses();
            speechSynthesizer = new SpeechSynthesizer();
        }

        private void InitializeResponses()
        {
            keywordResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["password"] = new List<string>
                {
                    "🔐 Create strong passwords with at least 12 characters, including uppercase, lowercase, numbers, and symbols.",
                    "🔑 Never reuse passwords across different accounts. Use a password manager!",
                    "🛡️ Enable two-factor authentication whenever possible for an extra layer of security."
                },
                ["scan"] = new List<string>
                {
                    "📱 Regularly scan your devices for malware using trusted antivirus software.",
                    "🔍 Before downloading files, scan them with Windows Defender or your preferred antivirus.",
                    "✅ Run weekly security scans to detect and remove potential threats."
                },
                ["privacy"] = new List<string>
                {
                    "👁️ Review privacy settings on social media to control who sees your information.",
                    "🔒 Use a VPN when using public Wi-Fi to protect your personal data.",
                    "📧 Avoid sharing personal information like your ID number or address in emails."
                },
                ["phishing"] = new List<string>
                {
                    "🎣 Never click on suspicious links in emails or text messages.",
                    "⚠️ Check the sender's email address carefully - scammers use fake addresses.",
                    "🔗 Hover over links before clicking to see the actual URL destination.",
                    "📞 If an email seems urgent, contact the company directly using their official phone number."
                },
                ["malware"] = new List<string>
                {
                    "🦠 Keep your operating system and software updated to patch security vulnerabilities.",
                    "💾 Only download software from official websites or trusted app stores.",
                    "🚫 Don't click on pop-up ads claiming your computer is infected."
                },
                ["how are you"] = new List<string>
                {
                    "I'm doing great, thank you for asking! How can I help you with cybersecurity today?",
                    "I'm fully secure and ready to assist you! What would you like to learn about?",
                    "Feeling safe and sound! What cybersecurity topic interests you today?"
                },
                ["what's your purpose"] = new List<string>
                {
                    "My purpose is to educate South African citizens about staying safe online!",
                    "I'm here to help you learn about cybersecurity threats and how to avoid them.",
                    "Think of me as your personal cybersecurity assistant - ask me anything!"
                },
                ["what can i ask you about"] = new List<string>
                {
                    "You can ask me about: passwords, scanning for malware, privacy, phishing, or general cybersecurity tips!",
                    "Try asking about 'password safety', 'phishing tips', 'privacy', or 'how to scan for malware'.",
                    "I can help with password security, privacy protection, malware scanning, and spotting phishing attempts!"
                }
            };

            followUpResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tell me more"] = "Sure! Let me give you more details about {topic}.",
                ["another tip"] = "Here's another helpful tip about {topic}:",
                ["explain more"] = "I'd be happy to explain further about {topic}.",
                ["more"] = "Here's additional information about {topic}:"
            };
        }

        private void InitializeSentimentResponses()
        {
            sentimentResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["worried"] = "It's completely normal to feel worried about online security. Don't worry - I'm here to help you stay safe! Let me share some practical tips.",
                ["scared"] = "I understand your concern. Cybersecurity can feel overwhelming, but taking small steps makes a big difference. Here's what you can do:",
                ["frustrated"] = "I hear your frustration. Online security can be annoying sometimes, but these precautions protect you. Let me simplify this for you:",
                ["curious"] = "That's great that you're curious! Learning about cybersecurity is the first step to staying safe. Here's what you should know:",
                ["confused"] = "I understand this can be confusing. Let me explain it in simpler terms:",
                ["happy"] = "I'm glad you're in good spirits! Let me share something interesting about cybersecurity:"
            };
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
                // Silently fail - UI will still work
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
      
      🔐 CYBERSECURITY AWARENESS BOT 🔐
      Protecting South African citizens online";
        }

        public string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Please type a message so I can help you with cybersecurity!";
            }

            // Store in conversation history for follow-ups
            conversationHistory.Enqueue(input);
            if (conversationHistory.Count > 5) conversationHistory.Dequeue();

            // Check for sentiment first
            string sentiment = DetectSentiment(input);
            if (sentiment != null && sentimentResponses.ContainsKey(sentiment))
            {
                return sentimentResponses[sentiment] + " " + GetCybersecurityTip();
            }

            // Check for name extraction (first time user)
            if (string.IsNullOrEmpty(userName))
            {
                string extractedName = ExtractName(input);
                if (extractedName != null)
                {
                    userName = extractedName;
                    userMemory["Name"] = userName;
                    return $"Nice to meet you, {userName}! I'll remember that. What would you like to learn about cybersecurity today?";
                }
            }

            // Check for interest extraction
            string interest = ExtractInterest(input);
            if (interest != null)
            {
                userInterest = interest;
                userMemory["Interest"] = interest;
                return $"Great! I'll remember that you're interested in {interest}. It's a very important topic for staying safe online!";
            }

            // Check for follow-up requests
            string followUpResponse = HandleFollowUp(input);
            if (followUpResponse != null)
            {
                return followUpResponse;
            }

            // Check for keywords
            string keywordResponse = GetKeywordResponse(input);
            if (keywordResponse != null)
            {
                lastTopic = keywordResponse;
                return keywordResponse;
            }

            // Check if user is asking about stored memory
            if (input.Contains("my name") || input.Contains("who am i"))
            {
                if (!string.IsNullOrEmpty(userName))
                    return $"You told me your name is {userName}!";
                else
                    return "I don't know your name yet. What should I call you?";
            }

            if (input.Contains("my interest") || input.Contains("interested in"))
            {
                if (!string.IsNullOrEmpty(userInterest))
                    return $"You're interested in {userInterest}. Would you like to learn more about that?";
                else
                    return "You haven't told me what cybersecurity topic interests you yet!";
            }

            // Default response
            return "I'm not sure I understand. Can you try rephrasing? You can ask me about passwords, scanning for malware, privacy, or phishing.";
        }

        private string GetKeywordResponse(string input)
        {
            foreach (var keyword in keywordResponses.Keys)
            {
                if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    List<string> responses = keywordResponses[keyword];
                    string selectedResponse = responses[random.Next(responses.Count)];

                    // Store the topic for follow-ups
                    lastTopic = keyword;
                    userMemory["LastTopic"] = keyword;

                    return selectedResponse;
                }
            }
            return null;
        }

        private string HandleFollowUp(string input)
        {
            if (string.IsNullOrEmpty(lastTopic)) return null;

            foreach (var followUp in followUpResponses.Keys)
            {
                if (input.Contains(followUp, StringComparison.OrdinalIgnoreCase))
                {
                    if (keywordResponses.ContainsKey(lastTopic))
                    {
                        var responses = keywordResponses[lastTopic];
                        string newResponse = responses[random.Next(responses.Count)];
                        return followUpResponses[followUp].Replace("{topic}", lastTopic) + " " + newResponse;
                    }
                    else
                    {
                        return $"Let me give you more information about cybersecurity. {GetCybersecurityTip()}";
                    }
                }
            }
            return null;
        }

        private string DetectSentiment(string input)
        {
            if (Regex.IsMatch(input, @"worried|anxious|nervous|concerned", RegexOptions.IgnoreCase))
                return "worried";
            if (Regex.IsMatch(input, @"scared|terrified|fear|afraid", RegexOptions.IgnoreCase))
                return "scared";
            if (Regex.IsMatch(input, @"frustrated|annoyed|angry", RegexOptions.IgnoreCase))
                return "frustrated";
            if (Regex.IsMatch(input, @"curious|interested|want to learn", RegexOptions.IgnoreCase))
                return "curious";
            if (Regex.IsMatch(input, @"confused|don't understand|unclear", RegexOptions.IgnoreCase))
                return "confused";
            if (Regex.IsMatch(input, @"happy|great|awesome|excited", RegexOptions.IgnoreCase))
                return "happy";
            return null;
        }

        private string ExtractName(string input)
        {
            var nameMatch = Regex.Match(input, @"my name is (\w+)", RegexOptions.IgnoreCase);
            if (nameMatch.Success) return nameMatch.Groups[1].Value;

            nameMatch = Regex.Match(input, @"call me (\w+)", RegexOptions.IgnoreCase);
            if (nameMatch.Success) return nameMatch.Groups[1].Value;

            nameMatch = Regex.Match(input, @"i am (\w+)", RegexOptions.IgnoreCase);
            if (nameMatch.Success && !nameMatch.Groups[1].Value.Equals("worried", StringComparison.OrdinalIgnoreCase))
                return nameMatch.Groups[1].Value;

            return null;
        }

        private string ExtractInterest(string input)
        {
            string[] interests = { "password", "privacy", "phishing", "malware", "scanning" };
            foreach (string interest in interests)
            {
                if (Regex.IsMatch(input, $@"interested in {interest}|like {interest}|want to learn about {interest}", RegexOptions.IgnoreCase))
                {
                    return interest;
                }
            }
            return null;
        }

        private string GetCybersecurityTip()
        {
            string[] tips = {
                "Always use strong, unique passwords for each account.",
                "Enable two-factor authentication wherever possible.",
                "Never share your passwords with anyone.",
                "Keep your software and operating system updated.",
                "Be careful what you download and click on.",
                "Use antivirus software and keep it updated.",
                "Back up your important files regularly.",
                "Be cautious of unsolicited emails asking for personal information."
            };
            return tips[random.Next(tips.Length)];
        }

        public string GetUserName()
        {
            return userName ?? "Friend";
        }

        public string GetUserInterest()
        {
            return userInterest ?? "cybersecurity";
        }
    }
}
